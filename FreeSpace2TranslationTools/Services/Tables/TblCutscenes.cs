using FreeSpace2TranslationTools.Services.Entries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal class TblCutscenes : Tables
	{
		public List<ECutscene> Cutscenes { get; set; } = [];

		public TblCutscenes(List<GameFile> files, string tableName, string modularTableSuffix) : base(files, tableName, modularTableSuffix)
		{
			ExtractInternationalizationContent();
		}

		protected override void ExtractInternationalizationContent()
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

						ECutscene cutscene = new()
						{
							FileName = fileName.Value,
							Name = XstrManager.GetValueWithoutXstr(name.Value),
							Description = description.Value
						};

						Cutscenes.Add(cutscene);
						Entries.Add(cutscene);
					}
				}
			}
		}

		protected override string GetInternationalizedContent()
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
