using FreeSpace2TranslationTools.Services.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal class TblRank : Tables
	{
		public TblRank(List<GameFile> files, string tableName, string modularTableSuffix) : base(files, tableName, modularTableSuffix)
		{
			ExtractInternationalizationContent();
		}

		protected override void ExtractInternationalizationContent()
		{
			foreach (string table in AllTables)
			{
				IEnumerable<Match> entries = Regexp.GenericEntries.Matches(table);

				foreach (Match entry in entries)
				{
					Match name = Regexp.Names.Match(entry.Value);
					Match altNameMatch = Regexp.AltNames.Match(entry.Value);
					Match rankTitleMatch = Regexp.RankTitles.Match(entry.Value);
					Match promotionTextMatch = Regexp.PromotionTexts.Match(entry.Value);

					string altName = name.Value.Trim();
					string title = name.Value.Trim();
					string promotionText = "";

					if (altNameMatch.Success)
					{
						altName = altNameMatch.Value;
					}

					if (rankTitleMatch.Success)
					{
						title = rankTitleMatch.Value;
					}

					if (promotionTextMatch.Success)
					{
						promotionText = promotionTextMatch.Value;
					}

					Entries.Add(new ERank()
					{
						Name = name.Value.Trim(),
						AltName = altName,
						Title = title,
						PromotionText = promotionText
					});
				}
			}
		}

		protected override string GetInternationalizedContent()
		{
			StringBuilder content = new();
			content.Append($"[RANK NAMES]{Environment.NewLine}");

			foreach (ERank rank in Entries)
			{
				content.Append($"{Environment.NewLine}$Name: {rank.Name}{Environment.NewLine}+nocreate{Environment.NewLine}");
				content.Append($"$Alt Name: XSTR(\"{rank.AltName}\", -1){Environment.NewLine}");
				content.Append($"$Title: XSTR({Environment.NewLine}\"{rank.Title}\", -1) ;{Constants.UNIQUE_ID} | used in $rtitle{Environment.NewLine}");
				content.Append($"$Promotion Text: XSTR({Environment.NewLine}\"{rank.PromotionText}\", -1){Environment.NewLine}$end_multi_text{Environment.NewLine}");
			}

			content.Append($"{Environment.NewLine}#End");

			return content.ToString();
		}
	}
}
