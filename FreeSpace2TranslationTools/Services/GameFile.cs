using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    public class GameFile
    {
        public string Name { get; private set; }
        public string Content { get; private set; }
        private bool Modified { get; set; }
        public FileType Type { get; private set; }

        public GameFile(string name)
        {
            Name = name;
            Content = File.ReadAllText(name);
            Modified = false;
            SetFileType();
        }

        public void SaveContent(string newContent)
        {
            if (newContent != Content)
            { 
                Content = newContent; 
                Modified = true; 
            }
        }

        public void CreateFileWithNewContent(string modFolder, string destinationFolder)
        {
            if (Modified)
            {
                // take care to keep the potential subfolders...
                string filePath = Name.Replace(modFolder, destinationFolder);

                FileManager.CreateFileWithPath(filePath, Content);
            }
        }

        internal IEnumerable<IXstr> GetAllXstr()
        {
            FileInfo fileInfo = new(Name);

            List<IXstr> result = new();
            MatchCollection resultsFromFile = Utils.RegexXstr.Matches(Content);

            foreach (Match match in resultsFromFile)
            {
                IXstr xstr = new Xstr(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value);
                result.Add(xstr);
            }

            if (Type == FileType.Mission)
            {
                MatchCollection modifyResults = Regex.Matches(Content, "\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);

                foreach (Match match in modifyResults)
                {
                    IXstr xstr = new XstrModifyVariable(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value);
                    result.Add(xstr);
                }

                MatchCollection techIntelResults = Regex.Matches(Content, "\\(\\s*tech-add-intel-xstr\\s*(\".*?\")\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);

                foreach (Match match in techIntelResults)
                {
                    IXstr xstr = new XstrTechIntel(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value);
                    result.Add(xstr);
                }
            }
            else if (Type == FileType.Fiction)
            {
                MatchCollection showIconLines = Regex.Matches(Content, "SHOWICON.+?text=(\".+?\").+?xstrid=(-?\\d+)");

                foreach (Match match in showIconLines)
                {
                    IXstr xstr = new XstrShowIcon(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value);
                    result.Add(xstr);
                }

                MatchCollection msgXstrLines = Regex.Matches(Content, "(?<=MSGXSTR.+)(\".+?\") (-?\\d+)");

                foreach (Match match in msgXstrLines)
                {
                    IXstr xstr = new XstrMsg(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value);
                    result.Add(xstr);
                }
            }

            return result;
        }

        private void SetFileType()
        {
            if (Name.EndsWith(Constants.MISSION_EXTENSION))
            {
                Type = FileType.Mission;
            }
            else if (Name.EndsWith(Constants.CAMPAIGN_EXTENSION))
            {
                Type = FileType.Campaign;
            }
            else if (Name.EndsWith(Constants.TABLE_EXTENSION) || Name.EndsWith(Constants.MODULAR_TABLE_EXTENSION))
            {
                Type = FileType.Table;
            }
            else if (Name.Contains(Constants.FICTION_FOLDER) && Name.EndsWith(Constants.FICTION_EXTENSION))
            {
                Type = FileType.Fiction;
            }
        }
    }
}
