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
        Regex regexXstr = new Regex("XSTR\\((\".*?\"), (-?\\d+)\\)", RegexOptions.Singleline);
        Regex regexXstrInTstrings = new Regex("(\\d+), (\".*?\")", RegexOptions.Singleline);
        Regex regexModifyXstr = new Regex("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(\\d+)\\s*\\)", RegexOptions.Singleline);
        Regex regexNotADigit = new Regex("[^0-9.-]+");

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
                    int newId =  SetNextID(tbStartingID.Text, ref lines);

                    try
                    {
                        using StreamWriter sw = File.CreateText(Path.Combine(destinationFolder, "tstringsModified-tlc.tbm"));
                        sw.WriteLine("#default");

                        string currentFile = string.Empty;
                        string content = string.Empty;

                        foreach (Xstr duplicate in duplicates)
                        {
                            // if there is another duplicate with the same text, we can reuse the same ID to avoid new duplicates in the new file
                            if (content.Contains(duplicate.Text))
                            {
                                duplicate.Id = duplicates.First(x => x.Treated && x.Text == duplicate.Text).Id;
                                duplicate.Treated = true;
                            }
                            else
                            {
                                duplicate.Id = newId;
                                newId++;

                                // add the name of the file in comment
                                if (currentFile != duplicate.FileName)
                                {
                                    currentFile = duplicate.FileName;
                                    content += Environment.NewLine + "; " + duplicate.FileName + Environment.NewLine;
                                }
                                content += Environment.NewLine + duplicate.Id + ", " + duplicate.Text + Environment.NewLine;
                                duplicate.Treated = true;
                            }
                        }

                        content += Environment.NewLine + "#end";

                        sw.Write(content);
                    }
                    catch (Exception ex)
                    {

                    }
                    #endregion

                    #region create new version of tables and missions files with new IDs
                    duplicates = duplicates.OrderBy(x => x.FileName).ToList();

                    List<string> filesToModify = duplicates.Select(x => x.FilePath).Distinct().ToList();

                    foreach (string sourceFile in filesToModify)
                    {
                        string fileName = Path.GetFileName(sourceFile);

                        List<Xstr> linesForThisFile = duplicates.Where(x => x.FileName == fileName).ToList();

                        string content = File.ReadAllText(sourceFile);

                        foreach (Xstr lineToModify in linesForThisFile)
                        {
                            ReplaceContentWithNewXstr(ref content, lineToModify);
                        }

                        CreateFileWithNewIDs(sourceFile, modFolder, destinationFolder, content);
                    }
                    #endregion
                }

                try
                {
                    #region Creation of tstrings.tbl
                    using StreamWriter sw = File.CreateText(Path.Combine(destinationFolder, "tstrings.tbl"));
                    sw.WriteLine("#default");

                    string currentFile = string.Empty;

                    foreach (Xstr line in lines)
                    {
                        // add the name of the file in comment
                        if (currentFile != line.FileName)
                        {
                            currentFile = line.FileName;
                            sw.WriteLine(string.Empty);
                            sw.WriteLine("; " + line.FileName);
                        }

                        sw.WriteLine(string.Empty);
                        sw.WriteLine(line.Id + ", " + line.Text);
                    }

                    sw.WriteLine(string.Empty);
                    sw.WriteLine("#end");
                    #endregion

                    ProcessComplete();
                }
                catch (Exception ex)
                {

                }
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

            int nextID = SetNextID(startingId, ref xstrFromTstringsList);

            List<string> filesList = GetFilesWithXstrFromFolder(modFolder);

            // Required to avoid thread access errors...
            Dispatcher.Invoke(() =>
            {
                pbGlobalProgress.Maximum = filesList.Count;
            });

            foreach (string sourceFile in filesList)
            {
                currentProgress++;
                (sender as BackgroundWorker).ReportProgress(currentProgress);

                bool fileModificationRequired = false;
                FileInfo fileInfo = new FileInfo(sourceFile);
                string fileContent = File.ReadAllText(sourceFile);
                IEnumerable<Match> xstrMatches = GetAllXstrFromFile(fileInfo, fileContent);
                List<Xstr> xstrToBeAddedList = new List<Xstr>();

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
                    // here the text does not exist in the original tstrings, so we have to create new lines in a different file !!! TODO !!!
                    else if (manageNewIds)
                    {
                        fileModificationRequired = true;
                        Xstr newXstr = new Xstr(nextID, match.Groups[1].Value, fileInfo, match.Groups[0].Value);
                        ReplaceContentWithNewXstr(ref fileContent, newXstr);
                        xstrToBeAddedList.Add(newXstr);
                        nextID++;
                    }
                }

                if (fileModificationRequired)
                {
                    CreateFileWithNewIDs(sourceFile, modFolder, destinationFolder, fileContent);
                }
            }

            ProcessComplete();
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

        private void CreateFileWithNewIDs(string sourceFile, string modFolder, string destinationFolder, string content)
        {
            // take care to keep the potential subfolders...
            string destFile = sourceFile.Replace(modFolder, destinationFolder);
            string destDirectoryPath = Path.GetDirectoryName(destFile);

            // create the potential subfolders in the destination
            Directory.CreateDirectory(destDirectoryPath);

            File.WriteAllText(destFile, content);
        }

        private void ReplaceContentWithNewXstr(ref string content, Xstr lineToModify)
        {
            string newLine = string.Empty;

            if (lineToModify.FullLine.Contains("modify-variable-xstr"))
            {
                newLine = regexModifyXstr.Replace(lineToModify.FullLine,
                    m => string.Format(
                        "{0}{1}",
                        m.Groups[1].Value,
                        lineToModify.Id));
            }
            else
            {
                newLine = regexXstr.Replace(lineToModify.FullLine,
                    m => string.Format(
                        "XSTR({0}, {1})",
                        m.Groups[1].Value,
                        lineToModify.Id));
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
    }
}
