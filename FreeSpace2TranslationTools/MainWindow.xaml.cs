using FreeSpace2TranslationTools.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        Regex regexModifyXstr = new Regex("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(\\d+)\\s*\\)", RegexOptions.Singleline);
        Regex regexNotADigit = new Regex("[^0-9.-]+");
        // (?=...) => look ahead, select only before that part
        Regex regexEntries = new Regex(@"\$Name:\s*.*?(?=\$Name|#End)", RegexOptions.Singleline);
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
            string modFolder = tbModFolder.Text;
            string destinationFolder = tbDestinationFolder.Text;
            bool manageDuplicates = cbManageDuplicates.IsChecked ?? false;

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

                if (manageDuplicates && duplicates.Count > 0)
                {
                    #region write duplicates into a separate file with new IDs
                    // new ID = max ID + 1 to avoid duplicates
                    int newId = SetNextID(tbStartingID.Text, ref lines);

                    string currentFile = string.Empty;
                    string tstringsModifiedContent = $"#Default{newLine}";

                    foreach (Xstr duplicate in duplicates)
                    {
                        // if there is another duplicate with the same text, we can reuse the same ID to avoid new duplicates in the new file
                        if (tstringsModifiedContent.Contains(duplicate.Text))
                        {
                            Xstr result = duplicates.FirstOrDefault(x => x.Treated && x.Text == duplicate.Text);

                            if (result != null)
                            {
                                duplicate.Id = result.Id;
                                //duplicate.Id = duplicates.First(x => x.Treated && x.Text == duplicate.Text).Id;
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
                    }
                    #endregion
                }

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
                }

                content += $"{newLine}#End";
                CreateFileWithPath(Path.Combine(destinationFolder, "tables/tstrings.tbl"), content);
                #endregion

                ProcessComplete();
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
            string translationSource = tbTranslationSource.Text;
            string translationDestination = tbTranslationDestination.Text;

            string sourceContent = File.ReadAllText(translationSource);
            string destinationContent = File.ReadAllText(translationDestination);

            //Regex regexXstr = new Regex("(\\d+), (\".*?\")", RegexOptions.Singleline);

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
                pbGlobalProgress.Maximum = filesList.Count + (manageNewIds ? 1:0);
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

            tstringsModifiedContent += $"{newLine}#End";

            return tstringsModifiedContent;
        }

        private List<string> GetFilesWithXstrFromFolder(string folderPath)
        {
            string[] extensions = new[] { ".tbl", ".tbm", ".fc2", ".fs2" };

            return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f))).ToList();
        }

        private IEnumerable<Match> GetAllXstrFromFile(FileInfo fileInfo, string fileContent)
        {
            MatchCollection resultsFromFile = regexXstr.Matches(fileContent);
            IEnumerable<Match> combinedResults = resultsFromFile.OfType<Match>().Where(m => m.Success);

            // there is an additional specific format in fs2 files
            if (fileInfo.Extension == ".fs2")
            {
                Regex regexModify = new Regex("\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(\\d+)\\s*\\)", RegexOptions.Singleline);
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
                    m => $"{m.Groups[1].Value}{lineToModify.Id}");
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
            string modFolder = tbModFolderXSTR.Text;
            string destinationFolder = tbDestinationFolderXSTR.Text;

            if (!string.IsNullOrEmpty(modFolder) && !string.IsNullOrEmpty(destinationFolder))
            {
                List<string> filesList = GetFilesWithXstrFromFolder(modFolder);

                #region Main hall => door descriptions
                List<string> mainHallFiles = filesList.Where(x => x.Contains("-hall.tbm") || x.Contains("Mainhall.tbl")).ToList();

                // all door descriptions without XSTR variable (everything after ':' is selected in group 1, so comments (;) should be taken away
                Regex regexDoorDescription = new Regex(@"\+Door description:\s*(((?!XSTR).)*)\r", RegexOptions.Multiline);

                foreach (string file in mainHallFiles)
                {
                    string sourceContent = File.ReadAllText(file);

                    string newContent = regexDoorDescription.Replace(sourceContent, new MatchEvaluator(GenerateXstrWithoutComments));

                    if (sourceContent != newContent)
                    {
                        CreateFileWithNewContent(file, modFolder, destinationFolder, newContent);
                    }
                }
                #endregion

                #region Ships alt names
                List<string> shipFiles = filesList.Where(x => x.Contains("-shp.tbm") || x.Contains("Ships.tbl")).ToList();
                List<string> shipNames = new List<string>();

                foreach (string file in shipFiles)
                {
                    string sourceContent = File.ReadAllText(file);
                    // Match all ship entries
                    MatchCollection shipEntries = regexEntries.Matches(sourceContent);

                    shipNames.AddRange(GetEntryNames(shipEntries));
                }

                // create new file containing ship alt names
                if (shipNames.Count > 0)
                {
                    string newShipFileContent = GenerateFileContent("#Ship Classes", shipNames);
                    CreateFileWithPath(Path.Combine(destinationFolder, "tables/new-shp.tbm"), newShipFileContent);
                }
                #endregion

                #region Weapons alt names
                List<string> weaponFiles = filesList.Where(x => x.Contains("-wep.tbm") || x.Contains("Weapons.tbl")).ToList();
                List<string> primaryNames = new List<string>();
                List<string> secondaryNames = new List<string>();

                Regex regexPrimary = new Regex("#Primary Weapons.*?#End", RegexOptions.Singleline);
                Regex regexSecondary = new Regex("#Secondary Weapons.*?#End", RegexOptions.Singleline);

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
                #endregion

                ProcessComplete();
            }
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
                $"{newLine}$Alt Name: XSTR(\"{entry.Trim('@')}\", -1){newLine}";
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

        private string GenerateXstrWithoutComments(Match match)
        {
            string[] values = match.Groups[1].Value.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
            string result = $"+Door description: XSTR(\"{values[0]}\", -1)";

            if (values.Count() > 1)
            {
                result += $" ;{values[1]}";
            }

            return result;
        }

        private void btnModFolderXSTR_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Mod folder", true, ref tbModFolderXSTR);
        }

        private void btnDestinationFolderXSTR_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation("Destination folder", true, ref tbDestinationFolderXSTR);
        }
    }
}
