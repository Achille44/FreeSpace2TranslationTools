using FreeSpace2TranslationTools.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        string modFolder = string.Empty;
        string destinationFolder = string.Empty;
        string translationSource = string.Empty;
        string translationDestination = string.Empty;
        string oldOriginalFile = string.Empty;
        string oldTranslatedFile = string.Empty;
        string newOriginalFile = string.Empty;
        string newTranslatedFile = string.Empty;
        Regex regexXstrInTstrings = new Regex("(\\d+), (\".*?\")", RegexOptions.Singleline);
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
            modFolder = tbModFolder.Text;
            destinationFolder = tbDestinationFolder.Text;
            bool manageDuplicates = cbManageDuplicates.IsChecked ?? false;

            Regex regexXstr = new Regex("XSTR\\((\".*?\"), (-?\\d+)\\)", RegexOptions.Singleline);

            if (!string.IsNullOrEmpty(modFolder) && !string.IsNullOrEmpty(destinationFolder))
            {
                string[] extensions = new[] { ".tbl", ".tbm", ".fc2", ".fs2" };

                List<string> filesList = Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories)
                    .Where(f => extensions.Contains(Path.GetExtension(f))).ToList();

                List<Xstr> lines = new List<Xstr>();
                List<Xstr> duplicates = new List<Xstr>();

                foreach (string file in filesList)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string fileContent = File.ReadAllText(file);
                    MatchCollection resultsFromFile = regexXstr.Matches(fileContent);

                    IEnumerable<Match> combinedResults = resultsFromFile.OfType<Match>().Where(m => m.Success);

                    // there is an additional specific format in fs2 files
                    if (fileInfo.Extension == ".fs2")
                    {
                        Regex regexModify = new Regex("\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(\\d+)\\s*\\)", RegexOptions.Singleline);
                        MatchCollection modifyResults = regexModify.Matches(fileContent);

                        combinedResults = resultsFromFile.OfType<Match>().Concat(modifyResults.OfType<Match>()).Where(m => m.Success);

                        //resultsFromFile.Concat(modifyResults);
                    }

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

                if (manageDuplicates && duplicates.Count > 0)
                {
                    #region write duplicates into a separate file with new IDs
                    // new ID = max ID + 1 to avoid duplicates
                    int newId = 0;

                    if (tbStartingID.Text != string.Empty && int.TryParse(tbStartingID.Text, out int startingID))
                    {
                        newId = startingID;
                    }
                    else if (lines.Count > 0)
                    {
                        newId = lines.Max(x => x.Id) + 1;
                    }

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
                        // take care to keep the potential subfolders...
                        string destFile = sourceFile.Replace(modFolder, destinationFolder);
                        string destDirectoryPath = Path.GetDirectoryName(destFile);

                        // create the potential subfolders in the destination
                        Directory.CreateDirectory(destDirectoryPath);

                        string fileName = Path.GetFileName(destFile);

                        List<Xstr> linesForThisFile = duplicates.Where(x => x.FileName == fileName).ToList();

                        string content = string.Empty;

                        try
                        {
                            using StreamReader reader = new StreamReader(sourceFile);
                            content = reader.ReadToEnd();
                        }
                        catch (Exception ex)
                        {

                        }

                        Regex regexModify = new Regex("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(\\d+)\\s*\\)", RegexOptions.Singleline);

                        foreach (Xstr lineToModify in linesForThisFile)
                        {
                            string newLine = string.Empty;

                            if (lineToModify.FullLine.Contains("modify-variable-xstr"))
                            {
                                newLine = regexModify.Replace(lineToModify.FullLine,
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

                        try
                        {
                            using StreamWriter sw = new StreamWriter(destFile, false);
                            sw.Write(content);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    #endregion
                }

                try
                {
                    #region Creation of tstring.tbl
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
            translationSource = tbTranslationSource.Text;
            translationDestination = tbTranslationDestination.Text;

            string sourceContent = Utils.ReadFileContent(translationSource);
            string destinationContent = Utils.ReadFileContent(translationDestination);

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

            try
            {
                using StreamWriter sw = new StreamWriter(translationDestination, false);
                sw.Write(destinationContent);

                ProcessComplete();
            }
            catch (Exception ex)
            {

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

        private void tbStartingID_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsOnlyDigit(e.Text);
        }

        private void tbStartingID_Pasting(object sender, DataObjectPastingEventArgs e)
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

        private void UpdateTranslation (object sender, DoWorkEventArgs e)
        {
            oldOriginalFile = tbOldOriginal.Text;
            newOriginalFile = tbNewOriginal.Text;
            oldTranslatedFile = tbOldTranslated.Text;
            newTranslatedFile = tbNewTranslated.Text;

            string oldOriginalContent = Utils.ReadFileContent(oldOriginalFile);
            string newOriginalContent = Utils.ReadFileContent(newOriginalFile);
            string oldTranslatedContent = Utils.ReadFileContent(oldTranslatedFile);
            string newTranslatedCOntent = Utils.ReadFileContent(newTranslatedFile);

            MatchCollection matchesInNewOriginal = regexXstrInTstrings.Matches(newOriginalContent);

            // Required to avoid thread access errors...
            Dispatcher.Invoke(() =>
            {
                pbGlobalProgress.Maximum = matchesInNewOriginal.Count;
            });
            
            int currentProgress = 0;

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
                            newTranslatedCOntent = newTranslatedCOntent.Replace(match.Groups[2].Value.Insert(1, tbMarker.Text), matchInOldTranslated.Groups[1].Value);
                        }
                    }
                }
                else
                {
                    throw new Exception();
                }
            }

            try
            {
                using StreamWriter sw = new StreamWriter(newTranslatedFile, false);
                sw.Write(newTranslatedCOntent);

                ProcessComplete();
            }
            catch (Exception ex)
            {

            }
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
    }
}
