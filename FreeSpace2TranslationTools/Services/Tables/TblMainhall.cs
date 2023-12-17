using FreeSpace2TranslationTools.Services.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal class TblMainhall : Tables
	{
		public List<EMainhall> Mainhalls { get; set; } = new();

        public TblMainhall(List<GameFile> files, string tableName, string modularTableSuffix) : base(files, tableName, modularTableSuffix)
		{
			ExtractInternationalizationContent();
		}

		protected override void ExtractInternationalizationContent()
		{
			foreach (string table in AllTables)
			{
				int numberResolutions = 2;
				Match numResolutions = Regexp.NumResolutions.Match(table);

				if (numResolutions.Success)
				{
					numberResolutions = int.Parse(numResolutions.Groups[1].Value);
				}

				IEnumerable<Match> entries = Regexp.MainhallEntries.Matches(table);
				EMainhall mainhall = new();

				foreach (Match entry in entries)
				{
					Match name = Regexp.MainhallNames.Match(entry.Value);

					if (name.Success)
					{
						mainhall = new()
						{
							Name = XstrManager.GetValueWithoutXstr(name.Value),
							NumResolutions = numberResolutions
						};

						Mainhalls.Add(mainhall);
						Entries.Add(mainhall);
					}

					if (mainhall.MainhallResolutions.Count < numberResolutions)
					{
						EMainhallResolution mainhallResolution = new()
						{
							Name = mainhall.Name
						};

						IEnumerable<Match> doorDescriptions = Regexp.DoorDescriptions.Matches(entry.Value);

						foreach (Match doorDescription in doorDescriptions)
						{
							mainhallResolution.DoorDescriptions.Add(XstrManager.GetValueWithoutXstr(doorDescription.Value));
						}

						mainhall.MainhallResolutions.Add(mainhallResolution);
					}
				}
			}
		}

		public IEnumerable<GameFile> CreateInternationalizedTables()
		{
			List<GameFile> files = new();

			if (Mainhalls.Any(m => m.MainhallResolutions.Any(mr => mr.DoorDescriptions.Count > 0)))
			{
				Mainhalls = Mainhalls.OrderBy(m => m.NumResolutions).ToList();
				IEnumerable<int> numResolutions = Mainhalls.DistinctBy(m => m.NumResolutions).Select(m => m.NumResolutions);

				foreach (int numResolution in numResolutions)
				{
					string fileName = I18nFile.Replace(Constants.MAINHALL_MODULAR_TABLE_SUFFIX, "_" + numResolution + "res" + Constants.MAINHALL_MODULAR_TABLE_SUFFIX);
					files.Add(new GameFile(fileName, GetInternationalizedContent(numResolution)));
				}
			}

			return files;
		}

		protected string GetInternationalizedContent(int numResolutions)
		{
			IEnumerable<EMainhall> mainhalls = Mainhalls.Where(m => m.NumResolutions == numResolutions && m.MainhallResolutions.Any(mr => mr.DoorDescriptions.Count > 0));
			StringBuilder content = new($"$Num Resolutions: {numResolutions + Environment.NewLine}");

			foreach (EMainhall mainhall in mainhalls)
			{
				bool firstResolution = true;

				foreach (EMainhallResolution mainhallResolution in mainhall.MainhallResolutions)
				{
					content.Append($"{Environment.NewLine}$Main Hall{Environment.NewLine}");

					if (firstResolution)
					{
						content.Append($"+Name: {mainhall.Name + Environment.NewLine}+nocreate{Environment.NewLine}");
						firstResolution = false;
					}

					content.Append(Environment.NewLine);

					foreach(string doorDescription in mainhallResolution.DoorDescriptions)
					{
						content.Append($"+Door description: XSTR(\"{doorDescription}\", -1){Environment.NewLine}");
					}
				}
			}

			content.Append($"{Environment.NewLine}#End");

			return content.ToString();
		}

		// not used for mainhalls
		protected override string GetInternationalizedContent()
		{
			throw new NotImplementedException();
		}
	}
}
