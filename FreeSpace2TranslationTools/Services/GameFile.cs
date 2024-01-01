using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public GameFile(string name, string content)
		{
			Name = name;
			Content = content;
			Modified = true;
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
            IEnumerable<Match> resultsFromFile = Regexp.Xstr.Matches(Content);

            foreach (Match match in resultsFromFile)
            {
                IXstr xstr = new Xstr(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value, match.Groups[4].Value);

                if (match.Groups[4].Value.Contains(Constants.UNIQUE_ID))
                {
                    xstr.UniqueId = true;
				}

				result.Add(xstr);
            }

            switch (Type)
            {
                case FileType.Campaign:
                    break;
                case FileType.Fiction:
					IEnumerable<Match> showIconLines = Regexp.ShowIcon.Matches(Content);

					foreach (Match match in showIconLines)
					{
						IXstr xstr = new XstrShowIcon(int.Parse(match.Groups[3].Value), match.Groups[2].Value, fileInfo, match.Value);
						result.Add(xstr);
					}

					IEnumerable<Match> msgXstrLines = Regexp.MsgXstr.Matches(Content);

					foreach (Match match in msgXstrLines)
					{
						IXstr xstr = new XstrMsg(int.Parse(match.Groups[2].Value), match.Groups[1].Value, fileInfo, match.Value);
						result.Add(xstr);
					}
					break;
                case FileType.Mission:
					IEnumerable<Match> modifyResults = Regexp.ModifyVariableXstr.Matches(Content);

					foreach (Match match in modifyResults)
					{
						IXstr xstr = new XstrModifyVariable(int.Parse(match.Groups[3].Value), match.Groups[2].Value, fileInfo, match.Value);
						result.Add(xstr);
					}

					IEnumerable<Match> techIntelResults = Regexp.TechAddIntelXstr.Matches(Content);

					foreach (Match match in techIntelResults)
					{
                        foreach (Match intel in Regexp.XstrInSexp.Matches(match.Value).Cast<Match>())
                        {
							IXstr xstr = new XstrTechIntel(int.Parse(intel.Groups[3].Value), intel.Groups[2].Value, fileInfo, intel.Value);
							result.Add(xstr);
						}
					}
					break;
                case FileType.Table:
                    break;
                case FileType.Tstrings:
					IEnumerable<Match> tstrings = Regexp.XstrInTstrings.Matches(Content);

                    foreach(Match match in tstrings)
                    {
                        IXstr xstr = new XstrTstrings(int.Parse(match.Groups[1].Value), match.Groups[2].Value, fileInfo, match.Value);
						result.Add(xstr);
					}
					break;
                default:
                    break;
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
			else if (Name.EndsWith(Constants.TSTRINGS_TABLE) || Name.EndsWith(Constants.TSTRINGS_MODULAR_TABLE_SUFFIX))
			{
				Type = FileType.Tstrings;
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
