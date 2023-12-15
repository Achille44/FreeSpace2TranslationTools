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
        readonly static string newLine = Environment.NewLine;

        public MainWindow()
        {
            InitializeComponent();
            // Default cache is 15
            Regex.CacheSize = 100;
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
            return Regexp.OnlyDigits.IsMatch(text);
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

        private static void ManageFileException(FileException ex)
        {
            MessageBox.Show($"{Localization.TechnicalError}{newLine}{newLine}{Localization.ModFileInvolved}{ex.File}{newLine}{newLine}{ex.Message}{newLine}{ex.StackTrace}{newLine}{ex.InnerException}");
        }

        private static void ManageException(Exception ex)
        {
            if (ex.GetType().Name == nameof(UserFriendlyException))
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

                MatchCollection matchesInNewOriginal = Regexp.XstrInTstrings.Matches(newOriginalContent);
				List<IXstr> newOriginalXstrList = new();

				foreach (Match match in matchesInNewOriginal.AsEnumerable())
				{
					Xstr xstr = new(int.Parse(match.Groups[1].Value), match.Groups[2].Value, match.Value, match.Groups[3].Value);
					newOriginalXstrList.Add(xstr);
				}

				// We order the list so that entries with comments are treated before, and so they don't get replaced by similar entries without comments
                newOriginalXstrList = newOriginalXstrList.OrderByDescending(x => x.Comments).ToList();

				// Required to avoid thread access errors...
				Dispatcher.Invoke(() =>
                {
                    pbGlobalProgress.Maximum = matchesInNewOriginal.Count;
                });

                IEnumerable<Match> matchesInOldOriginal = Regexp.XstrInTstrings.Matches(oldOriginalContent);
                List<IXstr> oldOriginalXstrList = new();

                foreach (Match match in matchesInOldOriginal)
                {
                    Xstr xstr = new(int.Parse(match.Groups[1].Value), match.Groups[2].Value, match.Value, match.Groups[3].Value);
                    oldOriginalXstrList.Add(xstr);
                }

                IEnumerable<Match> matchesInOldTranslated = Regexp.XstrInTstrings.Matches(oldTranslatedContent);
                List<IXstr> oldTranslatedXstrList = new();

                foreach (Match match in matchesInOldTranslated)
                {
                    Xstr xstr = new(int.Parse(match.Groups[1].Value), match.Groups[2].Value, match.Value, match.Groups[3].Value);
                    oldTranslatedXstrList.Add(xstr);
                }

                foreach (IXstr xstrNewOriginal in newOriginalXstrList)
                {
                    currentProgress++;
                    (sender as BackgroundWorker).ReportProgress(currentProgress);
                    bool exactMatch = true;

                    // first we look for an entry with same text and same comments, then only same text by default
                    IXstr xstrOldOriginal = oldOriginalXstrList.FirstOrDefault(x => x.Text == xstrNewOriginal.Text && x.Comments == xstrNewOriginal.Comments);

                    if (xstrOldOriginal == null)
                    {
                        exactMatch = false;
						xstrOldOriginal = oldOriginalXstrList.FirstOrDefault(x => x.Text == xstrNewOriginal.Text);
					}

					if (xstrOldOriginal != null)
                    {
                        IXstr xstrOldTranslated = oldTranslatedXstrList.FirstOrDefault(x => x.Id == xstrOldOriginal.Id);

                        if (xstrOldTranslated != null)
                        {
                            if (exactMatch)
                            {
								newTranslatedContent = newTranslatedContent.Replace(xstrNewOriginal.Text.Insert(1, marker) + xstrNewOriginal.Comments, xstrOldTranslated.Text + xstrOldTranslated.Comments);
							}
                            else
                            {
								newTranslatedContent = newTranslatedContent.Replace(xstrNewOriginal.Text.Insert(1, marker), xstrOldTranslated.Text);
							}

							oldTranslatedXstrList.Remove(xstrOldTranslated);
                        }

                        oldOriginalXstrList.Remove(xstrOldOriginal);
                    }
                }

                File.WriteAllText(newTranslatedFile, newTranslatedContent);

                watch.Stop();
                ProcessComplete(watch.Elapsed);
            }
            catch (FileException ex)
            {
                ManageFileException(ex);
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
                bool extractToSeparateFiles = false;

				Dispatcher.Invoke(() =>
                {
                    modFolder = tbModFolderXSTR.Text;
                    destinationFolder = tbDestinationFolderXSTR.Text;
                    duplicatesMustBeManaged = cbManageDuplicates.IsChecked ?? false;
					extractToSeparateFiles = cbExtractToNewFiles.IsChecked ?? false;
                    startingID = tbStartingID.Text;
                });

                CheckDirectoryIsValid(modFolder, Localization.ModFolder);
                CheckDirectoryIsValid(destinationFolder, Localization.DestinationFolder);

                List<GameFile> files = FileManager.GetFilesWithXstrFromFolder(modFolder);

                XstrManager xstrManager = new(this, sender, files, extractToSeparateFiles);
                xstrManager.LaunchXstrProcess();
                SetProgressToMax(sender);

                TstringsManager tstringsManager = new(this, sender, modFolder, destinationFolder, duplicatesMustBeManaged, files, startingID, extractToSeparateFiles);
                tstringsManager.ProcessTstrings();
                SetProgressToMax(sender);

                watch.Stop();
                ProcessComplete(watch.Elapsed);
            }
            catch (FileException ex)
            {
                ManageFileException(ex);
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

        public static void InitializeProgress(object sender)
        {
            (sender as BackgroundWorker).ReportProgress(0);
        }

        public static void IncreaseProgress(object sender, int progress)
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
