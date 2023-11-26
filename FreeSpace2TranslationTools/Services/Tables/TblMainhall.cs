using FreeSpace2TranslationTools.Services.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal class TblMainhall
	{
        public List<EMainhall> Mainhalls { get; set; }
		public List<string> AllTables { get; set; }

		public TblMainhall() 
		{
			Mainhalls = new List<EMainhall>();
			AllTables = new List<string>();
		}

		public void ExtractInternationalizationContent()
		{
			foreach (string table in AllTables)
			{
				IEnumerable<Match> entries = Regexp.MainhallEntries.Matches(table);

				foreach (Match entry in entries)
				{
					Match name = Regexp.MainhallNames.Match(entry.Value);

					if (!Mainhalls.Exists(c => c.Name == name.Value))
					{
						IEnumerable<Match> doorDescriptions = Regexp.DoorDescriptions.Matches(entry.Value);

						EMainhall mainhall = new()
						{
							Name = name.Value,
						};

						foreach (Match doorDescription in doorDescriptions)
						{
							mainhall.DoorDescriptions.Add(doorDescription.Value);
						}

						Mainhalls.Add(mainhall);
					}
				}
			}
		}

		public string GetContent()
		{
			StringBuilder content = new();
			content.Append($"$Main Hall{Environment.NewLine}");

			foreach (EMainhall mainhall in Mainhalls.Where(m => m.HasContent()))
			{
				content.Append($"{Environment.NewLine}+Name: {mainhall.Name}{Environment.NewLine}+nocreate{Environment.NewLine}");

                foreach (string doorDescription in mainhall.DoorDescriptions)
                {
					content.Append($"$Description: XSTR(\"{doorDescription}\", -1){Environment.NewLine}");
				}
			}

			content.Append($"{Environment.NewLine}#End");

			return content.ToString();
		}
	}
}
