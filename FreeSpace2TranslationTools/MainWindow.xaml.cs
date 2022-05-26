using FreeSpace2TranslationTools.Exceptions;
using FreeSpace2TranslationTools.Services;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Localization = FreeSpace2TranslationTools.Properties.Resources;

namespace FreeSpace2TranslationTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // \s => whitespace ; *? => select the shortest matching value
        Regex regexXstr = new("XSTR\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        Regex regexXstrInTstrings = new("(\\d+), (\".*?\")", RegexOptions.Singleline | RegexOptions.Compiled);
        readonly static string newLine = Environment.NewLine;

        public MainWindow()
        {
            InitializeComponent();
            // Default cache is 15
            Regex.CacheSize = 100;
        }

        private void btnTranslationSource_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.SourceFile, false, tbTranslationSource);
        }

        private void btnTranslationDestination_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.DestinationFile, false, tbTranslationDestination);
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            ToggleInputGrid();

            try
            {
                string translationSource = tbTranslationSource.Text;
                string translationDestination = tbTranslationDestination.Text;

                CheckFileIsValid(translationSource, Localization.SourceFile);
                CheckFileIsValid(translationDestination, Localization.DestinationFile);

                string sourceContent = File.ReadAllText(translationSource);
                string destinationContent = File.ReadAllText(translationDestination);

                MatchCollection matches = regexXstrInTstrings.Matches(destinationContent);

                foreach (Match match in matches)
                {
                    Regex regexWithID = new(string.Format("\\n{0}, (\".*?\")", match.Groups[1].Value), RegexOptions.Singleline);

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
            ChooseLocation(Localization.OldOriginalFile, false, tbOldOriginal);
        }

        private void btnOldTranslated_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.OldTranslatedFile, false, tbOldTranslated);
        }

        private void btnNewOriginal_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.NewOriginalFile, false, tbNewOriginal);
        }

        private void btnNewTranslated_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.NewTranslatedFile, false, tbNewTranslated);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += UpdateTranslation;
            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerAsync();
        }

        private static void ChooseLocation(string title, bool isFolderPicker, TextBox textBox)
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

        private static bool IsOnlyDigit(string text)
        {
            return Regex.IsMatch(text, "[^0-9.-]+");
        }

        /// <summary>
        /// Check if directory is valid, and show a MessageBox if not
        /// </summary>
        /// <param name="directoryLocation"></param>
        /// <param name="directoryLabel"></param>
        /// <returns></returns>
        private static void CheckDirectoryIsValid(string directoryLocation, string directoryLabel)
        {
            if (!Directory.Exists(directoryLocation))
            {
                throw new UserFriendlyException($"{Localization.InvalidDirectory}{directoryLabel}");
            }
        }

        private static void CheckFileIsValid(string FileLocation, string fileLabel)
        {
            if (!File.Exists(FileLocation))
            {
                throw new UserFriendlyException($"{Localization.InvalidFile}{fileLabel}");
            }
        }

        private static void ProcessComplete()
        {
            MessageBox.Show(Localization.ProcessComplete);
        }

        private static void ProcessComplete(TimeSpan time)
        {
            MessageBox.Show(Localization.ProcessComplete + newLine + newLine + Localization.ExecutionTime + time.Seconds.ToString() + " " + Localization.Seconds);
        }

        private static void ManageException(Exception ex)
        {
            if (ex.GetType().Name == "UserFriendlyException")
            {
                MessageBox.Show(ex.Message);
            }
            else
            {
                MessageBox.Show($"{Localization.TechnicalError}{newLine}{newLine}{ex.Message}{newLine}{ex.StackTrace}{newLine}{ex.InnerException}");
            }
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
                Stopwatch watch = Stopwatch.StartNew();

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

                CheckFileIsValid(oldOriginalFile, Localization.OldOriginalFile);
                CheckFileIsValid(newOriginalFile, Localization.NewOriginalFile);
                CheckFileIsValid(oldTranslatedFile, Localization.OldTranslatedFile);
                CheckFileIsValid(newTranslatedFile, Localization.NewTranslatedFile);

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

                MatchCollection matchesInOldOriginal = regexXstrInTstrings.Matches(oldOriginalContent);
                List<Xstr> oldOriginalXstrList = new();

                foreach (Match match in matchesInOldOriginal)
                {
                    Xstr xstr = new(int.Parse(match.Groups[1].Value), match.Groups[2].Value, match.Value);

                    oldOriginalXstrList.Add(xstr);
                }

                MatchCollection matchesInOldTranslated = regexXstrInTstrings.Matches(oldTranslatedContent);
                List<Xstr> oldTranslatedXstrList = new();

                foreach (Match match in matchesInOldTranslated)
                {
                    Xstr xstr = new(int.Parse(match.Groups[1].Value), match.Groups[2].Value, match.Value);

                    oldTranslatedXstrList.Add(xstr);
                }

                foreach (Match match in matchesInNewOriginal)
                {
                    currentProgress++;
                    (sender as BackgroundWorker).ReportProgress(currentProgress);

                    Xstr xstrOldOriginal = oldOriginalXstrList.FirstOrDefault(x => x.Text == match.Groups[2].Value);

                    if (xstrOldOriginal != null)
                    {
                        Xstr xstrOldTranslated = oldTranslatedXstrList.FirstOrDefault(x => x.Id == xstrOldOriginal.Id);

                        if (xstrOldTranslated != null)
                        {
                            newTranslatedContent = newTranslatedContent.Replace(match.Groups[2].Value.Insert(1, marker), xstrOldTranslated.Text);

                            oldTranslatedXstrList.Remove(xstrOldTranslated);
                        }

                        oldOriginalXstrList.Remove(xstrOldOriginal);
                    }
                }

                File.WriteAllText(newTranslatedFile, newTranslatedContent);

                watch.Stop();
                ProcessComplete(watch.Elapsed);
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
            ChooseLocation(Localization.ModFolder, true, tbModFolderInsertion);
        }

        private void btnOriginalTstrings_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.OriginalTstringsFile, false, tbOriginalTstrings);
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
            ChooseLocation(Localization.DestinationFolder, true, tbDestinationFolderInsert);
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

                CheckFileIsValid(originalTstrings, Localization.OriginalTstringsFile);
                CheckDirectoryIsValid(modFolder, Localization.ModFolder);
                CheckDirectoryIsValid(destinationFolder, Localization.DestinationFolder);

                string originalTstringsContent = File.ReadAllText(originalTstrings);

                MatchCollection allXstrInTstrings = regexXstrInTstrings.Matches(originalTstringsContent);
                List<Xstr> xstrFromTstringsList = new();

                foreach (Match match in allXstrInTstrings)
                {
                    if (int.TryParse(match.Groups[1].Value, out int id))
                    {
                        xstrFromTstringsList.Add(new Xstr(id, match.Groups[2].Value, match.Groups[0].Value));
                    }
                }

                List<string> filesList = GetFilesWithXstrFromFolder(modFolder);
                List<Xstr> xstrToBeAddedList = new();
                int nextID = SetNextID(startingId, xstrFromTstringsList);

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
                    FileInfo fileInfo = new(sourceFile);
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
                                    fileContent = Utils.ReplaceContentWithNewXstr(fileContent, new Xstr(matchingXstr[0].Id, match.Groups[1].Value, fileInfo, match.Groups[0].Value));
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
                                    Xstr newXstr = new(result.Id, match.Groups[1].Value, fileInfo, match.Groups[0].Value);
                                    fileContent = Utils.ReplaceContentWithNewXstr(fileContent, result);
                                }
                                else
                                {
                                    Xstr newXstr = new(nextID, match.Groups[1].Value, fileInfo, match.Groups[0].Value);
                                    fileContent = Utils.ReplaceContentWithNewXstr(fileContent, newXstr);
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

        private static string GenerateTstringsModified(List<Xstr> xstrToBeAddedList)
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
                        throw new Exception();
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

        private static List<string> GetFilesWithXstrFromFolder(string folderPath)
        {
            List<string> result = new();

            // First we look for tables, then we look for missions, to try to follow the translation conventions... and to avoid token problems in tables
            string[] tablesExtensions = new[] { ".tbl", ".tbm" };
            string[] missionsExtensions = new[] { ".fc2", ".fs2" };

            result.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => tablesExtensions.Contains(Path.GetExtension(f))).ToList());

            result.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => missionsExtensions.Contains(Path.GetExtension(f))).ToList());

            if (result.Count == 0)
            {
                throw new UserFriendlyException(Localization.NoValidFileInFolder);
            }

            return result;
        }

        private IEnumerable<Match> GetAllXstrFromFile(FileInfo fileInfo, string fileContent)
        {
            MatchCollection resultsFromFile = regexXstr.Matches(fileContent);
            IEnumerable<Match> combinedResults = resultsFromFile.OfType<Match>().Where(m => m.Success);

            // there is an additional specific format in fs2 files
            if (fileInfo.Extension == ".fs2")
            {
                MatchCollection modifyResults = Regex.Matches(fileContent, "\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);

                combinedResults = resultsFromFile.OfType<Match>().Concat(modifyResults.OfType<Match>()).Where(m => m.Success);
            }

            return combinedResults;
        }

        private static void CreateFileWithNewContent(string sourceFile, string modFolder, string destinationFolder, string content)
        {
            // take care to keep the potential subfolders...
            string filePath = sourceFile.Replace(modFolder, destinationFolder);

            CreateFileWithPath(filePath, content);
        }

        private static void CreateFileWithPath(string filePath, string content)
        {
            string destDirectoryPath = Path.GetDirectoryName(filePath);

            // create the potential subfolders in the destination
            Directory.CreateDirectory(destDirectoryPath);

            File.WriteAllText(filePath, content);
        }
        private static int SetNextID(string tbContent, List<Xstr> xstrList)
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
            BackgroundWorker worker = new();
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
                Stopwatch watch = Stopwatch.StartNew();

                int currentProgress = 0;
                (sender as BackgroundWorker).ReportProgress(currentProgress);

                string modFolder = string.Empty;
                string destinationFolder = string.Empty;
                string startingID = string.Empty;
                bool duplicatesMustBeManaged = false;

                Dispatcher.Invoke(() =>
                {
                    modFolder = tbModFolderXSTR.Text;
                    destinationFolder = tbDestinationFolderXSTR.Text;
                    duplicatesMustBeManaged = cbManageDuplicates.IsChecked ?? false;
                    startingID = tbStartingID.Text;
                });

                CheckDirectoryIsValid(modFolder, Localization.ModFolder);
                CheckDirectoryIsValid(destinationFolder, Localization.DestinationFolder);

                List<GameFile> files = Utils.GetFilesWithXstrFromFolder(modFolder);

                XstrManager xstrManager = new(this, sender, modFolder, destinationFolder, files);
                xstrManager.LaunchXstrProcess();
                SetProgressToMax(sender);

                TstringsManager tstringsManager = new(this, sender, modFolder, destinationFolder, duplicatesMustBeManaged, files, startingID);
                tstringsManager.ProcessTstrings();
                SetProgressToMax(sender);

                watch.Stop();
                ProcessComplete(watch.Elapsed);
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

        private void btnModFolderXSTR_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.ModFolder, true, tbModFolderXSTR);
        }

        private void btnDestinationFolderXSTR_Click(object sender, RoutedEventArgs e)
        {
            ChooseLocation(Localization.DestinationFolder, true, tbDestinationFolderXSTR);
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

        public void SetMaxProgress(int maxProgress)
        {
            // Required to avoid thread access errors...
            Dispatcher.Invoke(() =>
            {
                pbGlobalProgress.Maximum = maxProgress;
            });
        }

        public void InitializeProgress(object sender)
        {
            (sender as BackgroundWorker).ReportProgress(0);
        }

        public void IncreaseProgress(object sender, int progress)
        {
            (sender as BackgroundWorker).ReportProgress(progress);
        }

        public void SetProgressToMax(object sender)
        {
            // Required to avoid thread access errors...
            Dispatcher.Invoke(() =>
            {
                (sender as BackgroundWorker).ReportProgress(Convert.ToInt32(pbGlobalProgress.Maximum));
            });
        }
    }
}
