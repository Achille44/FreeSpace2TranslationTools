using FreeSpace2TranslationTools.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeSpace_tstrings_generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // \s => whitespace ; *? => select the shortest matching value
        Regex regexXstr = new Regex("XSTR\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);
        Regex regexXstrInTstrings = new Regex("(\\d+), (\".*?\")", RegexOptions.Singleline);
        Regex regexModifyXstr = new Regex("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Singleline);
        Regex regexNotADigit = new Regex("[^0-9.-]+");
        // (?=...) => look ahead, select only before that part
        Regex regexEntries = new Regex(@"\$Name:\s*.*?(?=\$Name|#end|#End)", RegexOptions.Singleline);
        Regex regexName = new Regex(@"\$Name:\s*(.*)\r");
        readonly string newLine = Environment.NewLine;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnModFolder_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Mod folder", true, ref tbModFolder);
        }

        private void btnDestinationFolder_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Destination folder", true, ref tbDestinationFolder);
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += GenerateTstrings;
            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerAsync();
        }

        private void GenerateTstrings(object sender, DoWorkEventArgs e)
        {
            ToggleInputGrid();

            try
            {
                int currentProgress = 0;
                (sender as BackgroundWorker).ReportProgress(currentProgress);

                string modFolder = string.Empty;
                string destinationFolder = string.Empty;
                string startingID = string.Empty;
                bool manageDuplicates = false;

                Dispatcher.Invoke(() =>
                {
                    modFolder = tbModFolder.Text;
                    destinationFolder = tbDestinationFolder.Text;
                    manageDuplicates = cbManageDuplicates.IsChecked ?? false;
                    startingID = tbStartingID.Text;
                });

                if (!string.IsNullOrEmpty(modFolder) && !string.IsNullOrEmpty(destinationFolder))
                {
                    List<string> filesList = GetFilesWithXstrFromFolder(modFolder);
                    List<Xstr> lines = new List<Xstr>();
                    List<Xstr> duplicates = new List<Xstr>();

                    #region looking for xstr in each file
                    foreach (string file in filesList)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        string fileContent = File.ReadAllText(file);

                        IEnumerable<Match> combinedResults = GetAllXstrFromFile(fileInfo, fileContent);

                        foreach (Match match in combinedResults)
                        {
                            //match.Groups[0] => entire line
                            //match.Groups[1] => text
                            //match.Groups[2] => id

                            if (int.TryParse(match.Groups[2].Value, out int id))
                            {
                                string text = match.Groups[1].Value;

                                // if id not existing, add a new line
                                if (id > 0 && !lines.Any(x => x.Id == id))
                                {
                                    lines.Add(new Xstr(id, text, fileInfo));
                                }
                                // if id already existing but value is different, then put it in another list that will be treated separately
                                else if (manageDuplicates && (id <= 0 || lines.First(x => x.Id == id).Text != text))
                                {
                                    duplicates.Add(new Xstr(id, text, fileInfo, match.Value));
                                }
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                    }
                    #endregion

                    // Required to avoid thread access errors...
                    Dispatcher.Invoke(() =>
                    {
                        pbGlobalProgress.Maximum = lines.Count + (manageDuplicates ? filesList.Count + duplicates.Count : 0);
                    });

                    #region Manage duplicates
                    if (manageDuplicates && duplicates.Count > 0)
                    {
                        #region write duplicates into a separate file with new IDs
                        // new ID = max ID + 1 to avoid duplicates
                        int newId = SetNextID(startingID, ref lines);

                        string currentFile = string.Empty;
                        string tstringsModifiedContent = $"#Default{newLine}";

                        foreach (Xstr duplicate in duplicates)
                        {
                            Xstr originalXstr = lines.FirstOrDefault(x => x.Text == duplicate.Text);

                            // if duplicated text exists in another xstr in the original file, then copy its ID
                            if (originalXstr != null)
                            {
                                duplicate.Id = originalXstr.Id;
                                duplicate.Treated = true;
                            }
                            // if there is another duplicate with the same text, we can reuse the same ID to avoid new duplicates in the new file
                            else if (tstringsModifiedContent.Contains(duplicate.Text))
                            {
                                Xstr result = duplicates.FirstOrDefault(x => x.Treated && x.Text == duplicate.Text);

                                if (result != null)
                                {
                                    duplicate.Id = result.Id;
                                    duplicate.Treated = true;
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                duplicate.Id = newId;
                                newId++;

                                // add the name of the file in comment
                                if (currentFile != duplicate.FileName)
                                {
                                    currentFile = duplicate.FileName;
                                    tstringsModifiedContent += $"{newLine}; {duplicate.FileName}{newLine}";
                                }

                                tstringsModifiedContent += $"{newLine}{duplicate.Id}, {duplicate.Text}{newLine}";
                                duplicate.Treated = true;
                            }
                            currentProgress++;
                            (sender as BackgroundWorker).ReportProgress(currentProgress);
                        }

                        tstringsModifiedContent += $"{newLine}#End";
                        CreateFileWithPath(Path.Combine(destinationFolder, "tables/tstringsModified-tlc.tbm"), tstringsModifiedContent);
                        #endregion

                        #region create new version of tables and missions files with new IDs
                        duplicates = duplicates.OrderBy(x => x.FileName).ToList();

                        List<string> filesToModify = duplicates.Select(x => x.FilePath).Distinct().ToList();

                        foreach (string sourceFile in filesToModify)
                        {
                            string fileName = Path.GetFileName(sourceFile);

                            List<Xstr> linesForThisFile = duplicates.Where(x => x.FileName == fileName).ToList();

                            string fileContent = File.ReadAllText(sourceFile);

                            foreach (Xstr lineToModify in linesForThisFile)
                            {
                                ReplaceContentWithNewXstr(ref fileContent, lineToModify);
                            }

                            CreateFileWithNewContent(sourceFile, modFolder, destinationFolder, fileContent);

                            currentProgress++;
                            (sender as BackgroundWorker).ReportProgress(currentProgress);
                        }
                        #endregion
                    }
                    #endregion

                    #region Creation of tstrings.tbl
                    string iterationFile = string.Empty;
                    string content = $"#Default{newLine}";

                    foreach (Xstr line in lines)
                    {
                        // add the name of the file in comment
                        if (iterationFile != line.FileName)
                        {
                            iterationFile = line.FileName;
                            content += $"{newLine}; {line.FileName}{newLine}";
                        }

                        content += $"{newLine}{line.Id}, {line.Text}{newLine}";

                        currentProgress++;
                        (sender as BackgroundWorker).ReportProgress(currentProgress);
                    }

                    content += $"{newLine}#End";
                    CreateFileWithPath(Path.Combine(destinationFolder, "tables/tstrings.tbl"), content);
                    #endregion

                    Dispatcher.Invoke(() =>
                    {
                        (sender as BackgroundWorker).ReportProgress(Convert.ToInt32(pbGlobalProgress.Maximum));
                    });

                    ProcessComplete();
                }
            }
            catch (Exception ex)
            {
                ManageException(ex);
            }
            finally
            {
                ToggleInputGrid();
            }
        }

        private void btnTranslationSource_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Translation source", false, ref tbTranslationSource);
        }

        private void btnTranslationDestination_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Translation destination", false, ref tbTranslationDestination);
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            ToggleInputGrid();

            try
            {
                string translationSource = tbTranslationSource.Text;
                string translationDestination = tbTranslationDestination.Text;

                string sourceContent = File.ReadAllText(translationSource);
                string destinationContent = File.ReadAllText(translationDestination);

                MatchCollection matches = regexXstrInTstrings.Matches(destinationContent);

                foreach (Match match in matches)
                {
                    Regex regexWithID = new Regex(string.Format("\\n{0}, (\".*?\")", match.Groups[1].Value), RegexOptions.Singleline);

                    Match matchInSource = regexWithID.Match(sourceContent);

                    if (matchInSource.Success)
                    {
                        string newText = matchInSource.Groups[1].Value;

                        destinationContent = destinationContent.Replace(match.Groups[2].Value, newText);
                    }
                }

                File.WriteAllText(translationDestination, destinationContent);

                ProcessComplete();
            }
            catch (Exception ex)
            {
                ManageException(ex);
            }
            finally
            {
                ToggleInputGrid();
            }
        }

        private void btnOldOriginal_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Old original file", false, ref tbOldOriginal);
        }

        private void btnOldTranslated_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Old translated file", false, ref tbOldTranslated);
        }

        private void btnNewOriginal_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("New original file", false, ref tbNewOriginal);
        }

        private void btnNewTranslated_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("New file to translate", false, ref tbNewTranslated);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += UpdateTranslation;
            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerAsync();
        }

        private void ChooseLocation(string title, bool isFolderPicker, ref TextBox textBox)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = title;
            dlg.IsFolderPicker = isFolderPicker;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox.Text = dlg.FileName;
            }
        }

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsOnlyDigit(e.Text);
        }

        private void OnlyDigits_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (IsOnlyDigit(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool IsOnlyDigit(string text)
        {
            return regexNotADigit.IsMatch(text);
        }

        private void ProcessComplete()
        {
            MessageBox.Show("Process Complete!");
        }

        private void ManageException(Exception ex)
        {
            MessageBox.Show($"{ex.Message}{newLine}{ex.StackTrace}{newLine}{ex.InnerException}");
        }

        private void cbManageDuplicates_Checked(object sender, RoutedEventArgs e)
        {
            tbStartingID.IsEnabled = true;
        }

        private void cbManageDuplicates_Unchecked(object sender, RoutedEventArgs e)
        {
            tbStartingID.IsEnabled = false;
        }

        private void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbGlobalProgress.Value = e.ProgressPercentage;
        }

        private void UpdateTranslation(object sender, DoWorkEventArgs e)
        {
            ToggleInputGrid();

            try
            {
                int currentProgress = 0;
                (sender as BackgroundWorker).ReportProgress(currentProgress);

                string oldOriginalFile = string.Empty;
                string newOriginalFile = string.Empty;
                string oldTranslatedFile = string.Empty;
                string newTranslatedFile = string.Empty;
                string marker = string.Empty;

                Dispatcher.Invoke(() =>
                {
                    oldOriginalFile = tbOldOriginal.Text;
                    newOriginalFile = tbNewOriginal.Text;
                    oldTranslatedFile = tbOldTranslated.Text;
                    newTranslatedFile = tbNewTranslated.Text;
                    marker = tbMarker.Text;
                });

                //Encoding encoding = Utils.GetEncoding(oldTranslatedFile);

                string oldOriginalContent = File.ReadAllText(oldOriginalFile);
                string newOriginalContent = File.ReadAllText(newOriginalFile);
                string oldTranslatedContent = File.ReadAllText(oldTranslatedFile);
                string newTranslatedContent = File.ReadAllText(newTranslatedFile);

                MatchCollection matchesInNewOriginal = regexXstrInTstrings.Matches(newOriginalContent);

                // Required to avoid thread access errors...
                Dispatcher.Invoke(() =>
                {
                    pbGlobalProgress.Maximum = matchesInNewOriginal.Count;
                });

                foreach (Match match in matchesInNewOriginal)
                {
                    currentProgress++;
                    (sender as BackgroundWorker).ReportProgress(currentProgress);

                    if (int.TryParse(match.Groups[1].Value, out int id))
                    {
                        Regex regexOldOriginal = new Regex(string.Format("\\n(\\d+), ({0})", Regex.Escape(match.Groups[2].Value)), RegexOptions.Singleline);

                        Match matchInOldOriginal = regexOldOriginal.Match(oldOriginalContent);

                        if (matchInOldOriginal.Success)
                        {
                            Regex regexOldTranslated = new Regex(string.Format("\\n{0}, (\".*?\")", Regex.Escape(matchInOldOriginal.Groups[1].Value)), RegexOptions.Singleline);

                            Match matchInOldTranslated = regexOldTranslated.Match(oldTranslatedContent);

                            if (matchInOldTranslated.Success)
                            {
                                newTranslatedContent = newTranslatedContent.Replace(match.Groups[2].Value.Insert(1, marker), matchInOldTranslated.Groups[1].Value);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                File.WriteAllText(newTranslatedFile, newTranslatedContent);

                ProcessComplete();
            }
            catch (Exception ex)
            {
                ManageException(ex);
            }
            finally
            {
                ToggleInputGrid();
            }
        }

        /// <summary>
        /// Copy the path of the file in case of a drag and drop over a textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                TextBox textBox = sender as TextBox;
                textBox.Text = Path.GetFullPath(files[0]);
            }
        }

        private void textBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = DragDropEffects.Move;
        }

        private void btnModFolderInsertion_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Mod folder", true, ref tbModFolderInsertion);
        }

        private void btnOriginalTstrings_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Original tstrings", false, ref tbOriginalTstrings);
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += IncludeExistingTranslation;
            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerAsync();
        }

        private void btnDestinationFolderInsert_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Destination folder", true, ref tbDestinationFolderInsert);
        }

        private void IncludeExistingTranslation(object sender, DoWorkEventArgs e)
        {
            ToggleInputGrid();

            try
            {
                int currentProgress = 0;
                (sender as BackgroundWorker).ReportProgress(currentProgress);

                string modFolder = string.Empty;
                string destinationFolder = string.Empty;
                string originalTstrings = string.Empty;
                string startingId = string.Empty;
                bool manageNewIds = false;

                Dispatcher.Invoke(() =>
                {
                    modFolder = tbModFolderInsertion.Text;
                    destinationFolder = tbDestinationFolderInsert.Text;
                    originalTstrings = tbOriginalTstrings.Text;
                    startingId = tbStartingIDInsert.Text;
                    manageNewIds = cbManageNewIds.IsChecked ?? false;
                });

                string originalTstringsContent = File.ReadAllText(originalTstrings);

                MatchCollection allXstrInTstrings = regexXstrInTstrings.Matches(originalTstringsContent);
                List<Xstr> xstrFromTstringsList = new List<Xstr>();

                foreach (Match match in allXstrInTstrings)
                {
                    if (int.TryParse(match.Groups[1].Value, out int id))
                    {
                        xstrFromTstringsList.Add(new Xstr(id, match.Groups[2].Value, match.Groups[0].Value));
                    }
                }

                List<string> filesList = GetFilesWithXstrFromFolder(modFolder);
                List<Xstr> xstrToBeAddedList = new List<Xstr>();
                int nextID = SetNextID(startingId, ref xstrFromTstringsList);

                // Required to avoid thread access errors...
                Dispatcher.Invoke(() =>
                {
                    pbGlobalProgress.Maximum = filesList.Count + (manageNewIds ? 1 : 0);
                });

                foreach (string sourceFile in filesList)
                {
                    currentProgress++;
                    (sender as BackgroundWorker).ReportProgress(currentProgress);

                    bool fileModificationRequired = false;
                    FileInfo fileInfo = new FileInfo(sourceFile);
                    string fileContent = File.ReadAllText(sourceFile);
                    IEnumerable<Match> xstrMatches = GetAllXstrFromFile(fileInfo, fileContent);

                    foreach (Match match in xstrMatches)
                    {
                        List<Xstr> matchingXstr = xstrFromTstringsList.Where(x => x.Text == match.Groups[1].Value).ToList();

                        if (matchingXstr.Count > 0)
                        {
                            if (int.TryParse(match.Groups[2].Value, out int id))
                            {
                                // in this case, the text already exists, but the ID is not the same
                                if (!matchingXstr.Exists(x => x.Id == id))
                                {
                                    fileModificationRequired = true;
                                    ReplaceContentWithNewXstr(ref fileContent, new Xstr(matchingXstr[0].Id, match.Groups[1].Value, fileInfo, match.Groups[0].Value));
                                }
                            }
                        }
                        // here the text does not exist in the original tstrings, so we have to create new lines in a different file
                        else if (manageNewIds)
                        {
                            if (int.TryParse(match.Groups[2].Value, out int id))
                            {
                                fileModificationRequired = true;

                                Xstr result = xstrToBeAddedList.FirstOrDefault(x => x.Text == match.Groups[1].Value);

                                if (result != null)
                                {
                                    Xstr newXstr = new Xstr(result.Id, match.Groups[1].Value, fileInfo, match.Groups[0].Value);
                                    ReplaceContentWithNewXstr(ref fileContent, result);
                                }
                                else
                                {
                                    Xstr newXstr = new Xstr(nextID, match.Groups[1].Value, fileInfo, match.Groups[0].Value);
                                    ReplaceContentWithNewXstr(ref fileContent, newXstr);
                                    xstrToBeAddedList.Add(newXstr);
                                    nextID++;
                                }
                            }
                        }
                    }

                    if (fileModificationRequired)
                    {
                        CreateFileWithNewContent(sourceFile, modFolder, destinationFolder, fileContent);
                    }
                }

                if (manageNewIds && xstrToBeAddedList.Count > 0)
                {
                    currentProgress++;
                    (sender as BackgroundWorker).ReportProgress(currentProgress);

                    string tstringsModifiedContent = GenerateTstringsModified(xstrToBeAddedList);

                    CreateFileWithPath(Path.Combine(destinationFolder, "tables/tstringsModified-tlc.tbm"), tstringsModifiedContent);
                }

                ProcessComplete();
            }
            catch (Exception ex)
            {
                ManageException(ex);
            }
            finally
            {
                ToggleInputGrid();
            }
        }

        private string GenerateTstringsModified(List<Xstr> xstrToBeAddedList)
        {
            string currentFile = string.Empty;
            string tstringsModifiedContent = $"#Default{newLine}";

            foreach (Xstr xstr in xstrToBeAddedList)
            {
                // if there is another duplicate with the same text, we can reuse the same ID to avoid new duplicates in the new file
                if (tstringsModifiedContent.Contains(xstr.Text))
                {
                    Xstr result = xstrToBeAddedList.FirstOrDefault(x => x.Treated && x.Text == xstr.Text);

                    if (result != null)
                    {
                        xstr.Id = result.Id;
                        xstr.Treated = true;
                    }
                    else
                    {

                    }
                }
                else
                {
                    // add the name of the file in comment
                    if (currentFile != xstr.FileName)
                    {
                        currentFile = xstr.FileName;
                        tstringsModifiedContent += $"{newLine}; {xstr.FileName}{newLine}";
                    }

                    tstringsModifiedContent += $"{newLine}{xstr.Id}, {xstr.Text}{newLine}";
                    xstr.Treated = true;
                }
            }

            tstringsModifiedContent += $"{newLine}#end";

            return tstringsModifiedContent;
        }

        private List<string> GetFilesWithXstrFromFolder(string folderPath)
        {
            List<string> result = new List<string>();

            // First we look for tables, then we look for missions, to try to follow the translation conventions... and to avoid token problems in tables
            string[] tablesExtensions = new[] { ".tbl", ".tbm" };
            string[] missionsExtensions = new[] { ".fc2", ".fs2" };

            result.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => tablesExtensions.Contains(Path.GetExtension(f))).ToList());

            result.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => missionsExtensions.Contains(Path.GetExtension(f))).ToList());

            return result;
        }

        private IEnumerable<Match> GetAllXstrFromFile(FileInfo fileInfo, string fileContent)
        {
            MatchCollection resultsFromFile = regexXstr.Matches(fileContent);
            IEnumerable<Match> combinedResults = resultsFromFile.OfType<Match>().Where(m => m.Success);

            // there is an additional specific format in fs2 files
            if (fileInfo.Extension == ".fs2")
            {
                Regex regexModify = new Regex("\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);
                MatchCollection modifyResults = regexModify.Matches(fileContent);

                combinedResults = resultsFromFile.OfType<Match>().Concat(modifyResults.OfType<Match>()).Where(m => m.Success);
            }

            return combinedResults;
        }

        private void CreateFileWithNewContent(string sourceFile, string modFolder, string destinationFolder, string content)
        {
            // take care to keep the potential subfolders...
            string filePath = sourceFile.Replace(modFolder, destinationFolder);

            CreateFileWithPath(filePath, content);
        }

        private void CreateFileWithPath(string filePath, string content)
        {
            string destDirectoryPath = Path.GetDirectoryName(filePath);

            // create the potential subfolders in the destination
            Directory.CreateDirectory(destDirectoryPath);

            File.WriteAllText(filePath, content);
        }

        private void ReplaceContentWithNewXstr(ref string content, Xstr lineToModify)
        {
            string newLine = string.Empty;

            if (lineToModify.FullLine.Contains("modify-variable-xstr"))
            {
                newLine = regexModifyXstr.Replace(lineToModify.FullLine,
                    m => $"{m.Groups[1].Value}{lineToModify.Id}{m.Groups[3].Value}");
            }
            else
            {
                newLine = regexXstr.Replace(lineToModify.FullLine,
                    m => $"XSTR({m.Groups[1].Value}, {lineToModify.Id})");
            }

            content = content.Replace(lineToModify.FullLine, newLine);
        }

        private int SetNextID(string tbContent, ref List<Xstr> xstrList)
        {
            int nextId = 0;

            if (tbContent != string.Empty && int.TryParse(tbContent, out int startingID))
            {
                nextId = startingID;
            }
            else if (xstrList.Count > 0)
            {
                nextId = xstrList.Max(x => x.Id) + 1;
            }

            return nextId;
        }

        private void cbManageNewIds_Checked(object sender, RoutedEventArgs e)
        {
            tbStartingIDInsert.IsEnabled = true;
        }

        private void cbManageNewIds_Unchecked(object sender, RoutedEventArgs e)
        {
            tbStartingIDInsert.IsEnabled = false;
        }

        private void btnCreateXstr_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += CreateXstr;
            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerAsync();
        }

        private void CreateXstr(object sender, DoWorkEventArgs e)
        {
            ToggleInputGrid();

            try
            {
                int currentProgress = 0;
                (sender as BackgroundWorker).ReportProgress(currentProgress);

                string modFolder = string.Empty;
                string destinationFolder = string.Empty;

                Dispatcher.Invoke(() =>
                {
                    modFolder = tbModFolderXSTR.Text;
                    destinationFolder = tbDestinationFolderXSTR.Text;
                });

                if (!string.IsNullOrEmpty(modFolder) && !string.IsNullOrEmpty(destinationFolder))
                {
                    List<string> filesList = GetFilesWithXstrFromFolder(modFolder);

                    // Required to avoid thread access errors...
                    Dispatcher.Invoke(() =>
                    {
                        pbGlobalProgress.Maximum = filesList.Count;
                    });

                    ProcessMainHallFiles(filesList, modFolder, destinationFolder, ref currentProgress, ref sender);

                    ProcessMedalsFile(filesList, modFolder, destinationFolder, ref currentProgress, ref sender);

                    ProcessShipFiles(filesList, destinationFolder, ref currentProgress, ref sender);

                    ProcessWeaponFiles(filesList, destinationFolder, ref currentProgress, ref sender);

                    ProcessMissionFiles(filesList, modFolder, destinationFolder, ref currentProgress, ref sender);

                    Dispatcher.Invoke(() =>
                    {
                        (sender as BackgroundWorker).ReportProgress(Convert.ToInt32(pbGlobalProgress.Maximum));
                    });

                    ProcessComplete();
                }
            }
            catch (Exception ex)
            {
                ManageException(ex);
            }
            finally
            {
                ToggleInputGrid();
            }
        }

        /// <summary>
        /// Adds alt names with XSTR variables to medals
        /// </summary>
        /// <param name="filesList"></param>
        /// <param name="modFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        private void ProcessMedalsFile(List<string> filesList, string modFolder, string destinationFolder, ref int currentProgress, ref object sender)
        {
            string medalsFile = filesList.First(x => x.Contains("medals.tbl"));
            string sourceContent = File.ReadAllText(medalsFile);

            Regex regexMedalNames = new Regex(@"(\$Name:\s*(.*?)\r\n)(\$Bitmap)", RegexOptions.Multiline);

            string newContent = regexMedalNames.Replace(sourceContent, new MatchEvaluator(GenerateMedals));

            if (sourceContent != newContent)
            {
                CreateFileWithNewContent(medalsFile, modFolder, destinationFolder, newContent);
            }

            currentProgress++;
            (sender as BackgroundWorker).ReportProgress(currentProgress);
        }


        /// <summary>
        /// Adds XSTR variables to door descriptions of main halls
        /// </summary>
        /// <param name="filesList"></param>
        /// <param name="modFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        private void ProcessMainHallFiles(List<string> filesList, string modFolder, string destinationFolder, ref int currentProgress, ref object sender)
        {
            #region Main hall => door descriptions
            List<string> mainHallFiles = filesList.Where(x => x.Contains("-hall.tbm") || x.Contains("mainhall.tbl")).ToList();

            // all door descriptions without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
            Regex regexDoorDescription = new Regex(@"\+Door description:\s*(((?!XSTR).)*)\r\n", RegexOptions.Multiline);

            foreach (string file in mainHallFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = regexDoorDescription.Replace(sourceContent, new MatchEvaluator(GenerateDoorDescriptions));

                if (sourceContent != newContent)
                {
                    CreateFileWithNewContent(file, modFolder, destinationFolder, newContent);
                }

                currentProgress++;
                (sender as BackgroundWorker).ReportProgress(currentProgress);
            }
            #endregion
        }

        private void ProcessShipFiles(List<string> filesList, string destinationFolder, ref int currentProgress, ref object sender)
        {
            List<string> shipFiles = filesList.Where(x => x.Contains("-shp.tbm") || x.Contains("ships.tbl")).ToList();
            List<string> shipNames = new List<string>();

            foreach (string file in shipFiles)
            {
                string sourceContent = File.ReadAllText(file);
                // Match all ship entries
                MatchCollection shipEntries = regexEntries.Matches(sourceContent);

                shipNames.AddRange(GetEntryNames(shipEntries));

                currentProgress++;
                (sender as BackgroundWorker).ReportProgress(currentProgress);
            }

            // create new file containing ship alt names
            if (shipNames.Count > 0)
            {
                string newShipFileContent = GenerateFileContent("#Ship Classes", shipNames);
                CreateFileWithPath(Path.Combine(destinationFolder, "tables/new-shp.tbm"), newShipFileContent);
            }
        }

        private void ProcessWeaponFiles(List<string> filesList, string destinationFolder, ref int currentProgress, ref object sender)
        {
            List<string> weaponFiles = filesList.Where(x => x.Contains("-wep.tbm") || x.Contains("weapons.tbl")).ToList();
            List<string> primaryNames = new List<string>();
            List<string> secondaryNames = new List<string>();

            Regex regexPrimary = new Regex("#Primary Weapons.*?(#end|#End)", RegexOptions.Singleline);
            Regex regexSecondary = new Regex("#Secondary Weapons.*?(#end|#End)", RegexOptions.Singleline);

            foreach (string file in weaponFiles)
            {
                string content = File.ReadAllText(file);
                Match primarySection = regexPrimary.Match(content);
                Match secondarySection = regexSecondary.Match(content);

                if (primarySection.Success)
                {
                    MatchCollection primaries = regexEntries.Matches(primarySection.Value);

                    primaryNames.AddRange(GetEntryNames(primaries));
                }

                if (secondarySection.Success)
                {
                    MatchCollection secondaries = regexEntries.Matches(secondarySection.Value);

                    secondaryNames.AddRange(GetEntryNames(secondaries));
                }

                currentProgress++;
                (sender as BackgroundWorker).ReportProgress(currentProgress);
            }

            // create new file containing weapons alt names
            if (primaryNames.Count > 0 || secondaryNames.Count > 0)
            {
                string newWeaponsFileContent = string.Empty;

                if (primaryNames.Count > 0)
                {
                    newWeaponsFileContent += GenerateFileContent("#Primary Weapons", primaryNames);
                    // add a new line in case of a following secondary section
                    newWeaponsFileContent += $"{newLine}{newLine}";
                }

                if (secondaryNames.Count > 0)
                {
                    newWeaponsFileContent += GenerateFileContent("#Secondary Weapons", secondaryNames);
                }

                CreateFileWithPath(Path.Combine(destinationFolder, "tables/new-wep.tbm"), newWeaponsFileContent);
            }
        }

        private void ProcessMissionFiles(List<string> filesList, string modFolder, string destinationFolder, ref int currentProgress, ref object sender)
        {
            List<string> missionFiles = filesList.Where(x => x.Contains(".fs2")).ToList();

            // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
            // ex: $Label: Alpha 1 ==> $Label: XSTR ("Alpha 1", -1)
            Regex regexLabels = new Regex(@"\$label:\s*(((?!XSTR).)*)\r", RegexOptions.Multiline);

            // ex: $Name: Psamtik   ==>     $Name: Psamtik
            //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
            //                      ==>     $Class......
            Regex regexShipNames = new Regex(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline);

            foreach (string file in missionFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = regexLabels.Replace(sourceContent, new MatchEvaluator(GenerateLabels));

                newContent = regexShipNames.Replace(newContent, new MatchEvaluator(GenerateShipNames));

                newContent = ConvertShowSubtitleToShowSubtitleText(newContent);

                newContent = ExtractShowSubtitleTextContentToMessages(newContent);

                newContent = ConvertAltToVariables(newContent);

                if (sourceContent != newContent)
                {
                    CreateFileWithNewContent(file, modFolder, destinationFolder, newContent);
                }

                currentProgress++;
                (sender as BackgroundWorker).ReportProgress(currentProgress);
            }
        }

        /// <summary>
        /// Extract hardcoded strings from show-subtitle-text and put them into new messages so they can be translated
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string ExtractShowSubtitleTextContentToMessages(string content)
        {
            // ex: ( show-subtitle-text     ==>     ( show-subtitle-text
            //        "Europa, 2386"        ==>        "AutoGeneratedMessage1"
            //                              ==>----------------------------------
            //                              ==>     #Messages
            //                              ==>     
            //                              ==>     $Name: AutoGeneratedMessage1
            //                              ==>     $Team: -1
            //                              ==>     $MessageNew: XSTR("Europa, 2386", -1)
            //                              ==>     $end_multi_text
            //                              ==>     
            //                              ==>     #Reinforcements
            Regex regexShowSubtitleText = new Regex("(show-subtitle-text\\s*\r\n\\s*\")(.*?)\"", RegexOptions.Multiline);
            Regex regexMessagesSection = new Regex(@"#Messages.*#Reinforcements", RegexOptions.Singleline);
            Regex regexAllMessages = new Regex(@"\$Name:\s*(.*?)(?=;|\r)", RegexOptions.Multiline);

            #region get all existing messages in the mission
            string messagesSection = regexMessagesSection.Match(content).Value;
            MatchCollection messages = regexAllMessages.Matches(messagesSection);
            List<string> allMessages = new List<string>();

            string autoGeneratedMessage = "AutoGeneratedMessage";
            int subtitleMessagesCount = 0;

            foreach (Match match in messages)
            {
                allMessages.Add(match.Groups[1].Value);

                // Check for existing AutoGeneratedMessage to increment the count in order to avoid duplications
                if (match.Groups[1].Value.Contains(autoGeneratedMessage))
                {
                    int iteration = int.Parse(match.Groups[1].Value.Substring(autoGeneratedMessage.Length));

                    if (iteration >= subtitleMessagesCount)
                    {
                        subtitleMessagesCount = iteration++;
                    }
                }
            }
            #endregion

            MatchCollection subtitleTextResults = regexShowSubtitleText.Matches(content);
            string newMessages = string.Empty;

            foreach (Match match in subtitleTextResults)
            {
                if (!allMessages.Contains(match.Groups[2].Value))
                {
                    subtitleMessagesCount++;

                    content = content.Replace(match.Value, match.Groups[1].Value + autoGeneratedMessage + subtitleMessagesCount + "\"");

                    newMessages += $"$Name: {autoGeneratedMessage}{subtitleMessagesCount}{newLine}" +
                        $"$Team: -1{newLine}" +
                        $"$MessageNew:  XSTR(\"{match.Groups[2].Value}\", -1){newLine}" +
                        $"$end_multi_text{newLine}{newLine}";

                    allMessages.Add(match.Groups[2].Value);
                }
            }

            if (newMessages != string.Empty)
            {
                content = content.Replace("#Reinforcements", newMessages + "#Reinforcements");
            }

            return content;
        }

        /// <summary>
        /// Convert ship alt names to sexp variables so that they can be translated via modify-variable-xstr sexp
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string ConvertAltToVariables(string content)
        {
            Regex regexAlternateTypes = new Regex(@"#Alternate Types:.*?#end\r\n\r\n", RegexOptions.Singleline);

            if (regexAlternateTypes.IsMatch(content))
            {
                #region alt from '#Alternate Types' section
                string alternateTypes = regexAlternateTypes.Match(content).Value;
                // for unknown reason in this case \r case is captured, so we have to uncapture it
                // in some cases and Alt can have an empty value... 
                Regex regexAlt = new Regex(@"\$Alt:\s*(((?!\$Alt).)+)(?=\r)");
                MatchCollection altTypes = regexAlt.Matches(alternateTypes);
                #endregion

                #region alt from '#Objects' section
                Regex regexObjects = new Regex(@"#Objects.*#Wings", RegexOptions.Singleline);
                string objects = regexObjects.Match(content).Value;
                // ((?!\$Name).)* => all characters not containing \$Name
                //Regex regexAltShips = new Regex(@"\$Name:\s*(.*?)\s*(\r\n|;)((?!\$Name).)*?\$Alt(.*?)\s*\r\n", RegexOptions.Singleline);
                Regex regexAltShips = new Regex(@"\$Name:\s*(((?!\$Name).)*?)\s*(\r\n|;)((?!\$Name).)*?\$Alt:\s*(.*?)\s*\r\n", RegexOptions.Singleline);
                MatchCollection altShips = regexAltShips.Matches(objects);
                #endregion

                List<Alt> altList = new List<Alt>();

                foreach (Match match in altTypes)
                {
                    Alt alt = new Alt(match.Groups[1].Value);

                    foreach (Match altShip in altShips)
                    {
                        if (altShip.Groups[5].Value == alt.DefaultValue)
                        {
                            alt.AddShip(altShip.Groups[1].Value);
                        }
                    }

                    // some alt are not used for unknown reasons, so we dont keep them
                    if (alt.Ships.Count > 0)
                    {
                        altList.Add(alt);
                    }
                }

                // Remove the 'Alternate Types' section
                content = content.Replace(alternateTypes, string.Empty);
                // Remove all '$Alt' from '#Objects' section
                Regex regexAltLines = new Regex(@"\$Alt:\s*.*?\r\n", RegexOptions.Singleline);
                content = regexAltLines.Replace(content, string.Empty);

                content = AddVariablesToSexpVariablesSection(content, altList);

                content = AddEventToManageAltNames(content, altList);
            }

            return content;
        }

        private string AddVariablesToSexpVariablesSection(string content, List<Alt> altList)
        {
            // Create '#Sexp_variables' section if not exists
            if (!content.Contains("#Sexp_variables"))
            {
                Regex regexBeforeSexp = new Regex(@"(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline);

                string beforeSexp = regexBeforeSexp.Match(content).Value;

                string newSection = $"#Sexp_variables{newLine}{newLine}$Variables:{newLine}({newLine}){newLine}{newLine}{beforeSexp}";

                content = content.Replace(beforeSexp, newSection);
            }

            Regex regexSexpVariablesSection = new Regex(@"#Sexp_variables.*?(#Fiction Viewer|#Command Briefing)", RegexOptions.Singleline);
            string sexpVariablesSection = regexSexpVariablesSection.Match(content).Value;
            Regex regexVariableId = new Regex(@"\t\t(\d+)\t\t", RegexOptions.Multiline);
            MatchCollection variableIds = regexVariableId.Matches(sexpVariablesSection);

            int variableId = 0;

            // set the next variable id
            foreach (Match match in variableIds)
            {
                int currentId = int.Parse(match.Groups[1].Value);

                if (currentId >= variableId)
                {
                    variableId++;
                }
            }

            string newSexpVariablesSection = string.Empty;

            // here we add a new variable for each alt
            foreach (Alt alt in altList)
            {
                alt.VariableName = "autoGenVar" + variableId;
                newSexpVariablesSection += $"\t\t{variableId}\t\t\"{alt.VariableName}\"\t\t\"{alt.DefaultValue}\"\t\t\"string\"{newLine}";
                variableId++;
            }

            Regex regexEndOfVariables = new Regex(@"\)\r\n\r\n(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline);
            string endOfVariables = regexEndOfVariables.Match(content).Value;

            return content.Replace(endOfVariables, newSexpVariablesSection + endOfVariables);
        }

        private string AddEventToManageAltNames(string content, List<Alt> altList)
        {
            // very unorthodox way to add the event but it allows me to manage the case when this event already exists in the original file
            string eventForAltNamesTitle = "Auto generated event: alt names";
            string eventEnd = $"){newLine}"
                + $"+Name: {eventForAltNamesTitle}{newLine}"
                + $"+Repeat Count: 1{newLine}"
                + $"+Interval: 1{newLine}{newLine}";

            if (!content.Contains(eventForAltNamesTitle))
            {
                Regex regexEvents = new Regex(@"#Events.*?\r\n\r\n", RegexOptions.Singleline);
                string events = regexEvents.Match(content).Value;

                string eventBeginning = $"$Formula: ( when {newLine}"
                    + $"   ( true ) {newLine}";

                content = content.Replace(events, events + eventBeginning + eventEnd);
            }

            string newSexp = string.Empty;

            foreach (Alt alt in altList)
            {
                newSexp += alt.ModifyVariableXstr() + alt.ShipChangeAltName();
            }

            return content.Replace(eventEnd, newSexp + eventEnd);
        }

        /// <summary>
        /// Convert sexp show-subtitle to show-subtitle-text so they can be translated
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string ConvertShowSubtitleToShowSubtitleText(string content)
        {
            Regex regexShowSubtitle = new Regex(@"show-subtitle\s+.*?\)", RegexOptions.Singleline);
            Regex regexFullLines = new Regex(@"(\s*)(.*?)(\s*\r\n)", RegexOptions.Multiline);
            MatchCollection subtitleResults = regexShowSubtitle.Matches(content);

            foreach (Match match in subtitleResults)
            {
                MatchCollection parameters = regexFullLines.Matches(match.Value);

                Sexp sexp = new Sexp("show-subtitle-text", parameters[1].Groups[1].Value, parameters[1].Groups[3].Value);

                // text to display
                sexp.AddParameter(parameters[3].Groups[2].Value);
                // X position, from 0 to 100%
                sexp.AddParameter(ConvertXPositionFromAbsoluteToRelative(parameters[1].Groups[2].Value));
                // Y position, from 0 to 100%
                sexp.AddParameter(ConvertYPositionFromAbsoluteToRelative(parameters[2].Groups[2].Value));
                // Center horizontally?
                if (parameters.Count < 8)
                {
                    sexp.AddParameter("( false )");
                }
                else
                {
                    sexp.AddParameter(parameters[7].Groups[2].Value);
                }
                // Center vertically?
                if (parameters.Count < 9)
                {
                    sexp.AddParameter("( false )");
                }
                else
                {
                    sexp.AddParameter(parameters[8].Groups[2].Value);
                }
                // Time (in milliseconds) to be displayed
                sexp.AddParameter(parameters[4].Groups[2].Value);
                // Fade time (in milliseconds) (optional)
                if (parameters.Count > 6)
                {
                    sexp.AddParameter(parameters[6].Groups[2].Value);
                }
                // Paragraph width, from 1 to 100% (optional; 0 uses default 200 pixels)
                if (parameters.Count > 9)
                {
                    sexp.AddParameter(parameters[9].Groups[2].Value);
                }
                // Text red component (0-255) (optional)
                if (parameters.Count > 10)
                {
                    sexp.AddParameter(parameters[10].Groups[2].Value);
                }
                // Text green component(0 - 255) (optional)
                if (parameters.Count > 11)
                {
                    sexp.AddParameter(parameters[11].Groups[2].Value);
                }
                // Text blue component(0 - 255) (optional)
                if (parameters.Count > 12)
                {
                    sexp.AddParameter(parameters[12].Groups[2].Value);
                }

                sexp.CloseFormula();

                content = content.Replace(match.Value, sexp.Formula);
            }

            return content;
        }

        private string ConvertXPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 900;
            double output = 88;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString();
        }

        private string ConvertYPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 500;
            double output = 65;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString();
        }

        /// <summary>
        /// This function generates content of a weapons/ships alt name list
        /// </summary>
        /// <param name="sectionTitle"></param>
        /// <param name="entryNames"></param>
        /// <returns></returns>
        private string GenerateFileContent(string sectionTitle, List<string> entryNames)
        {
            string content = $"{sectionTitle}{newLine}";

            foreach (string entry in entryNames)
            {
                content += $"{newLine}$Name: {entry}" +
                $"{newLine}+nocreate" +
                // remove @ and # alias from xstr
                $"{newLine}$Alt Name: XSTR(\"{entry.Split("#")[0].Trim('@')}\", -1){newLine}";
            }

            content += $"{newLine}#End";
            return content;
        }

        private List<string> GetEntryNames(MatchCollection allEntries)
        {
            List<string> nameList = new List<string>();

            foreach (Match match in allEntries)
            {
                if (!match.Value.Contains("$Alt Name:"))
                {
                    Match name = regexName.Match(match.Value);

                    if (name.Success)
                    {
                        // remove possible comments and @ from the name
                        nameList.Add(name.Groups[1].Value.Trim().Split(';', 2, StringSplitOptions.RemoveEmptyEntries)[0]);
                    }
                }
            }

            return nameList;
        }

        /// <summary>
        /// Replaces an hardcoded line with an XSTR variable
        /// </summary>
        /// <param name="marker">Marker identifying the line</param>
        /// <param name="match">Groups[1] must be the XSTR value (comments will be removed)</param>
        /// <returns></returns>
        private string ReplaceHardcodedValueWithXstr(string marker, Match match)
        {
            string[] values = match.Groups[1].Value.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
            string result = $"{marker}: XSTR(\"{values[0]}\", -1){newLine}";

            if (values.Count() > 1)
            {
                result += $" ;{values[1]}";
            }

            return result;
        }

        /// <summary>
        /// Adds a new line including an XSTR variable
        /// </summary>
        /// <param name="newMarker">Name of the new marker identifying the XSTR variable</param>
        /// <param name="match">Groups[1]: first original line (including \r\n), Groups[2]: hardcoded value to be translated, Groups[3]: line after the hardcoded value</param>
        /// <returns></returns>
        private string AddXstrLineToHardcodedValue(string newMarker, Match match)
        {
            string valueWithoutComment = match.Groups[2].Value.Split(';', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            string valueWithoutAlias = valueWithoutComment.Split('#', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            return $"{match.Groups[1].Value}{newMarker}: XSTR(\"{valueWithoutAlias.Trim()}\", -1){newLine}{match.Groups[3].Value}";
        }

        private string GenerateDoorDescriptions(Match match)
        {
            return ReplaceHardcodedValueWithXstr("+Door description", match);
        }

        private string GenerateLabels(Match match)
        {
            return ReplaceHardcodedValueWithXstr("$label", match);
        }

        private string GenerateShipNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Display Name", match);
        }

        private string GenerateMedals(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Name", match);
        }

        private void btnModFolderXSTR_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Mod folder", true, ref tbModFolderXSTR);
        }

        private void btnDestinationFolderXSTR_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Destination folder", true, ref tbDestinationFolderXSTR);
        }

        /// <summary>
        /// Enable/Disable all inputs on the screen
        /// </summary>
        private void ToggleInputGrid()
        {
            Dispatcher.Invoke(() =>
            {
                mainWindow.IsEnabled = !mainWindow.IsEnabled;
            });
        }
    }
}
