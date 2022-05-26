using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class TstringsManager
    {
        public MainWindow Parent { get; set; }
        public object Sender { get; set; }
        public string ModFolder { get; set; }
        public string DestinationFolder { get; set; }
        public List<GameFile> FilesList { get; set; }
        public List<Xstr> Lines { get; set; }
        public List<Xstr> Duplicates { get; set; }
        public bool ManageDuplicates { get; set; }
        public string StartingID { get; set; }
        public int CurrentProgress { get; set; }

        public TstringsManager(MainWindow parent, object sender, string modFolder, string destinationFolder, bool manageDuplicates, List<GameFile> filesList, string startingID = "")
        {
            Parent = parent;
            Sender = sender;
            ModFolder = modFolder;
            DestinationFolder = destinationFolder;
            CurrentProgress = 0;
            ManageDuplicates = manageDuplicates;
            StartingID = startingID;
            Lines = new();
            Duplicates = new();

            FilesList = filesList;

            Parent.InitializeProgress(Sender);
            Parent.SetMaxProgress(FilesList.Count);
        }

        #region public methods

        public void ProcessTstrings()
        {
            Parent.InitializeProgress(Sender);

            FetchXstr();

            #region Manage duplicates
            if (ManageDuplicates && Duplicates.Count > 0)
            {
                CreateFileForDuplicates();

                CreateModFilesWithNewIds();
            }
            #endregion

            foreach (GameFile file in FilesList)
            {
                if (file.Modified)
                {
                    Utils.CreateFileWithNewContent(file.Name, ModFolder, DestinationFolder, file.Content);
                }
            }

            CreateTstringsFile();
        }
        #endregion

        /// <summary>
        /// look for xstr in each file
        /// </summary>
        private void FetchXstr()
        {
            List<GameFile> compatibleFiles = FilesList.Where(x => !x.Name.Contains("-lcl.tbm") && !x.Name.Contains("-tlc.tbm") && !x.Name.Contains("strings.tbl")).ToList();

            foreach (GameFile file in compatibleFiles)
            {
                FileInfo fileInfo = new(file.Name);

                IEnumerable<Match> combinedResults = GetAllXstrFromFile(fileInfo, file.Content);

                foreach (Match match in combinedResults)
                {
                    //match.Groups[0] => entire line
                    //match.Groups[1] => text
                    //match.Groups[2] => id

                    if (int.TryParse(match.Groups[2].Value, out int id))
                    {
                        string text = match.Groups[1].Value;

                        // if id not existing, add a new line
                        if (id >= 0 && !Lines.Any(x => x.Id == id))
                        {
                            Lines.Add(new Xstr(id, text, fileInfo));
                        }
                        // if id already existing but value is different, then put it in another list that will be treated separately
                        else if (ManageDuplicates && (id < 0 || Lines.First(x => x.Id == id).Text != text))
                        {
                            Duplicates.Add(new Xstr(id, text, fileInfo, match.Value));
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }

            int maxProgress = Lines.Count + (ManageDuplicates ? FilesList.Count + Duplicates.Count : 0);
            Parent.SetMaxProgress(maxProgress);
        }

        private void CreateFileForDuplicates()
        {
            // new ID = max ID + 1 to avoid duplicates
            int newId = SetNextID();

            string currentFile = string.Empty;
            string tstringsModifiedContent = $"#Default{Environment.NewLine}";

            foreach (Xstr duplicate in Duplicates)
            {
                Xstr originalXstr = Lines.FirstOrDefault(x => x.Text == duplicate.Text);

                // if duplicated text exists in another xstr in the original file, then copy its ID
                if (originalXstr != null)
                {
                    duplicate.Id = originalXstr.Id;
                    duplicate.Treated = true;
                }
                // if there is another duplicate with the same text, we can reuse the same ID to avoid new duplicates in the new file
                else if (tstringsModifiedContent.Contains(duplicate.Text))
                {
                    Xstr result = Duplicates.FirstOrDefault(x => x.Treated && x.Text == duplicate.Text);

                    if (result != null)
                    {
                        duplicate.Id = result.Id;
                        duplicate.Treated = true;
                    }
                    else
                    {
                        throw new Exception();
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
                        tstringsModifiedContent += $"{Environment.NewLine}; {duplicate.FileName}{Environment.NewLine}";
                    }

                    tstringsModifiedContent += $"{Environment.NewLine}{duplicate.Id}, {duplicate.Text}{Environment.NewLine}";
                    duplicate.Treated = true;
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }

            tstringsModifiedContent += $"{Environment.NewLine}#End";

            Utils.CreateFileWithPath(Path.Combine(DestinationFolder, "tables/tstringsModified-tlc.tbm"), tstringsModifiedContent);
        }

        private int SetNextID()
        {
            int nextId = 0;

            if (StartingID != string.Empty && int.TryParse(StartingID, out int startingID))
            {
                nextId = startingID;
            }
            else if (Lines.Count > 0)
            {
                nextId = Lines.Max(x => x.Id) + 1;
            }

            return nextId;
        }

        /// <summary>
        /// Creates table and mission files with new IDs
        /// </summary>
        private void CreateModFilesWithNewIds()
        {
            Duplicates = Duplicates.OrderBy(x => x.FileName).ToList();
            List<string> filesToModify = Duplicates.Select(x => x.FilePath).Distinct().ToList();

            foreach (string sourceFile in filesToModify)
            {
                GameFile gameFile = FilesList.FirstOrDefault(file => file.Name == sourceFile);

                string fileName = Path.GetFileName(sourceFile);
                string newContent = gameFile.Content;

                foreach (Xstr lineToModify in Duplicates.Where(x => x.FileName == fileName))
                {
                    newContent = Utils.ReplaceContentWithNewXstr(newContent, lineToModify);
                }

                gameFile.SaveContent(newContent);


                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        /// <summary>
        /// Creates the tstrings.tbl file with original IDs
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        private void CreateTstringsFile()
        {
            if (Lines.Count > 0)
            {
                string iterationFile = string.Empty;
                string content = $"#Default{Environment.NewLine}";

                foreach (Xstr line in Lines.OrderBy(x => x.Id))
                {
                    // add the name of the file in comment
                    if (iterationFile != line.FileName)
                    {
                        iterationFile = line.FileName;
                        content += $"{Environment.NewLine}; {line.FileName}{Environment.NewLine}";
                    }

                    content += $"{Environment.NewLine}{line.Id}, {line.Text}{Environment.NewLine}";

                    Parent.IncreaseProgress(Sender, CurrentProgress++);
                }

                content += $"{Environment.NewLine}#End";
                Utils.CreateFileWithPath(Path.Combine(DestinationFolder, "tables/tstrings.tbl"), content);
            }
        }

        private static IEnumerable<Match> GetAllXstrFromFile(FileInfo fileInfo, string fileContent)
        {
            MatchCollection resultsFromFile = Utils.RegexXstr.Matches(fileContent);
            IEnumerable<Match> combinedResults = resultsFromFile.OfType<Match>().Where(m => m.Success);

            // there is an additional specific format in fs2 files
            if (fileInfo.Extension == ".fs2")
            {
                MatchCollection modifyResults = Regex.Matches(fileContent, "\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);

                combinedResults = combinedResults.Concat(modifyResults.OfType<Match>()).Where(match => match.Success);
            }
            else if (fileInfo.FullName.Contains(Constants.FICTION_FOLDER) && fileInfo.Extension == Constants.FICTION_EXTENSION)
            {
                MatchCollection showIconLines = Regex.Matches(fileContent, "SHOWICON.+?text=(\".+?\").+?xstrid=(-?\\d+)");

                MatchCollection msgXstrLines = Regex.Matches(fileContent, "(?<=MSGXSTR.+)(\".+?\") (-?\\d+)");

                combinedResults = combinedResults.Concat(showIconLines.OfType<Match>()).Where(match => match.Success)
                    .Concat(msgXstrLines.OfType<Match>()).Where(match => match.Success);
            }

            return combinedResults;
        }
    }
}
