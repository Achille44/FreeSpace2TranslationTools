using FreeSpace2TranslationTools.Services.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal class TblCutscenes
	{
		public List<ECutscene> Cutscenes { get; set; }
		public List<string> AllTables { get; set; }

		public TblCutscenes() 
		{
			Cutscenes = new List<ECutscene>();
			AllTables = new List<string>();
		}

		public void ExtractInternationalizationContent()
		{
			foreach (string table in AllTables)
			{
				IEnumerable<Match> entries = Regexp.CutsceneEntries.Matches(table);

				foreach (Match entry in entries)
				{
					Match fileName = Regexp.FileNames.Match(entry.Value);

					if (!Cutscenes.Exists(c => c.FileName == fileName.Value))
					{
						Match name = Regexp.Names.Match(entry.Value);
						Match description = Regexp.CutsceneDescriptions.Match(entry.Value);

						Cutscenes.Add(new ECutscene()
						{
							FileName = fileName.Value,
							Name = name.Value.Trim(),
							Description = description.Value
						});
					}
				}
			}
		}

		public string GetContent()
		{
			StringBuilder content = new();
			content.Append($"#Cutscenes{Environment.NewLine}");

			foreach (ECutscene cutscene in Cutscenes)
			{
				content.Append($"{Environment.NewLine}$Filename: {cutscene.FileName}{Environment.NewLine}+nocreate{Environment.NewLine}");
				content.Append($"$Name: XSTR(\"{cutscene.Name}\", -1){Environment.NewLine}");
				content.Append($"$Description: XSTR({Environment.NewLine}\"{cutscene.Description}\", -1){Environment.NewLine}$end_multi_text{Environment.NewLine}");
			}

			content.Append($"{Environment.NewLine}#End");

			return content.ToString();
		}
	}
}
