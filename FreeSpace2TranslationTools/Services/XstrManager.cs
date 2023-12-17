using FreeSpace2TranslationTools.Exceptions;
using FreeSpace2TranslationTools.Services.Entries;
using FreeSpace2TranslationTools.Services.Tables;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
	public class XstrManager
	{
		private MainWindow Parent { get; set; }
		private object Sender { get; set; }
		private List<GameFile> Files { get; set; }
		public bool ExtractToSeparateFiles { get; set; }
		private int CurrentProgress { get; set; } = 0;
		private List<Weapon> Weapons { get; set; } = new List<Weapon>();
		private List<EWeapon> EWeapons { get; set; } = new List<EWeapon>();
		private List<Ship> Ships { get; set; } = new List<Ship>();

		public XstrManager(MainWindow parent, object sender, List<GameFile> files, bool extractToSeparateFiles)
		{
			Parent = parent;
			Sender = sender;
			Files = files;
			ExtractToSeparateFiles = extractToSeparateFiles;

			MainWindow.InitializeProgress(Sender);
			Parent.SetMaxProgress(Files.Count);
		}

		public void LaunchXstrProcess()
		{
			ProcessCampaignFiles();
			ProcessCreditFiles();
			ProcessCutscenesFile();
			ProcessHudGaugeFiles();
			ProcessMainHallFiles();
			ProcessMedalsFiles();
			ProcessRankFile();
			// Weapons must be treated before ships because of the way ship turrets are treated!
			ProcessWeaponFiles();
			ProcessShipFiles();
			ProcessMissionFiles();
			ProcessVisualNovelFiles();
		}

		internal static string GenerateAltNames(Match match)
		{
			return AddXstrLineToHardcodedValue("$Alt Name", match);
		}

		internal static string InternationalizeHardcodedValue(Match match)
		{
			return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
		}

		internal static string ReplaceHardcodedValueWithXstr(string originalMatch, string beginningOfLine, string value)
		{
			// if this is a comment or if it's already XSTR, then don't touch it and return the original match
			if (beginningOfLine.Contains(';') || value.Contains("XSTR"))
			{
				return originalMatch;
			}
			else
			{
				string[] values = value.Trim().Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
				string sanatizedValue = values.Length == 0 ? "" : values[0].Replace("\"", "$quote").Trim();

				// in case no value, keep original
				if (sanatizedValue == "")
				{
					return originalMatch;
				}

				string result = $"{beginningOfLine}XSTR(\"{sanatizedValue}\", -1)";

				if (values.Length > 1)
				{
					result += $" ;{values[1]}";
				}

				if (originalMatch.EndsWith("\r\n"))
				{
					result += "\r\n";
				}
				else if (originalMatch.EndsWith("\n"))
				{
					result += "\n";
				}

				return result;
			}
		}

		private void ProcessCampaignFiles()
		{
			foreach (GameFile file in Files.Where(x => x.Type == FileType.Campaign))
			{
				try
				{
					ProcessFile(file, new Campaign(file.Content));
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}
		}

		private void ProcessCreditFiles()
		{
			foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-crd.tbm") || x.Name.EndsWith("credits.tbl")))
			{
				try
				{
					ProcessFile(file, new Credits(file.Content));
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}
		}

		private void ProcessCutscenesFile()
		{
			if (ExtractToSeparateFiles)
			{
				TblCutscenes tables = new(Files, Constants.CUTSCENES_TABLE, Constants.CUTSCENE_MODULAR_TABLE_SUFFIX);
				GameFile internationalizedTable = tables.CreateInternationalizedTable();

				if (internationalizedTable != null)
				{
					Files.Add(internationalizedTable);
				}
			}
			else
			{
				GameFile cutscenes = Files.FirstOrDefault(x => x.Name.EndsWith("cutscenes.tbl"));

				if (cutscenes != null)
				{
					try
					{
						ProcessFile(cutscenes, new Cutscenes(cutscenes.Content));
					}
					catch (Exception ex)
					{
						throw new FileException(ex, cutscenes.Name);
					}
				}
			}
		}

		private void ProcessHudGaugeFiles()
		{
			foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-hdg.tbm") || x.Name.EndsWith("hud_gauges.tbl")))
			{
				try
				{
					ProcessFile(file, new HudGauges(file.Content));
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}
		}

		private void ProcessMedalsFiles()
		{
			foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-mdl.tbm") || x.Name.EndsWith("medals.tbl")))
			{
				try
				{
					ProcessFile(file, new Medals(file.Content));
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}
		}

		private void ProcessMainHallFiles()
		{
			if (ExtractToSeparateFiles)
			{
				TblMainhall tables = new(Files, Constants.MAINHALL_TABLE, Constants.MAINHALL_MODULAR_TABLE_SUFFIX);
				IEnumerable<GameFile> internationalizedTables = tables.CreateInternationalizedTables();
				Files.AddRange(internationalizedTables);
			}
			else
			{
				foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-hall.tbm") || x.Name.EndsWith("mainhall.tbl")))
				{
					try
					{
						ProcessFile(file, new Mainhall(file.Content));
					}
					catch (Exception ex)
					{
						throw new FileException(ex, file.Name);
					}
				}
			}
		}

		private void ProcessRankFile()
		{
			if (ExtractToSeparateFiles)
			{
				TblRank tables = new(Files, Constants.RANK_TABLE, Constants.RANK_MODULAR_TABLE_SUFFIX);
				GameFile internationalizedTable = tables.CreateInternationalizedTable();

				if (internationalizedTable != null)
				{
					Files.Add(internationalizedTable);
				}
			}
			else
			{
				GameFile rankFile = Files.FirstOrDefault(x => x.Name.Contains(Constants.RANK_TABLE));

				if (rankFile != null)
				{
					try
					{
						ProcessFile(rankFile, new Rank(rankFile.Content));
					}
					catch (Exception ex)
					{
						throw new FileException(ex, rankFile.Name);
					}
				}
			}
		}

		private void ProcessShipFiles()
		{
			if (ExtractToSeparateFiles)
			{
				TblShips tables = new(Files, Constants.SHIPS_TABLE, Constants.SHIP_MODULAR_TABLE_SUFFIX, EWeapons);
				GameFile internationalizedTable = tables.CreateInternationalizedTable();

				if (internationalizedTable != null)
				{
					Files.Add(internationalizedTable);
				}
			}
			else
			{
				// Start with ships.tbl
				List<GameFile> shipFiles = Files.Where(f => f.Name.EndsWith("ships.tbl")).ToList();
				shipFiles.AddRange(Files.Where(f => f.Name.EndsWith(Constants.SHIP_MODULAR_TABLE_SUFFIX)).ToList());

				foreach (GameFile file in shipFiles)
				{
					try
					{
						ProcessShipFile(file, new ShipsFile(file.Content, Weapons));
					}
					catch (Exception ex)
					{
						throw new FileException(ex, file.Name);
					}
				}
			}
		}

		private void ProcessWeaponFiles()
		{
			if (ExtractToSeparateFiles)
			{
				TblWeapons tables = new(Files, Constants.WEAPONS_TABLE, Constants.WEAPON_MODULAR_TABLE_SUFFIX);
				EWeapons = tables.ExtractInternationalizationWeaponContent();
				GameFile internationalizedTable = tables.CreateInternationalizedTable();

				if (internationalizedTable != null)
				{
					Files.Add(internationalizedTable);
				}
			}
			else
			{
				foreach (GameFile file in Files.Where(x => x.Name.EndsWith(Constants.WEAPON_MODULAR_TABLE_SUFFIX) || x.Name.EndsWith("weapons.tbl")))
				{
					try
					{
						ProcessWeaponFile(file, new WeaponsFile(file.Content));
					}
					catch (Exception ex) { throw new FileException(ex, file.Name); }
				}
			}
		}

		private void ProcessMissionFiles()
		{
			foreach (GameFile file in Files.Where(x => x.Type == FileType.Mission).ToList())
			{
				try
				{
					ProcessFile(file, new Mission(file.Content));
				}
				catch (Exception ex) { throw new FileException(ex, file.Name); }
			}
		}

		private void ProcessVisualNovelFiles()
		{
			GameFile[] visualNovels = Files.Where(file => file.Type == FileType.Fiction).ToArray();

			foreach (GameFile file in visualNovels)
			{
				try
				{
					ProcessFile(file, new VisualNovel(file.Content));
				}
				catch (WrongFileFormatException)
				{
					continue;
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}
		}

		private void ProcessFile(GameFile gameFile, IFile file)
		{
			gameFile.SaveContent(file.GetInternationalizedContent());

			MainWindow.IncreaseProgress(Sender, CurrentProgress++);
		}

		private void ProcessWeaponFile(GameFile gameFile, IFile file)
		{
			gameFile.SaveContent(file.GetInternationalizedContent(Weapons));

			MainWindow.IncreaseProgress(Sender, CurrentProgress++);
		}

		private void ProcessShipFile(GameFile gameFile, IFile file)
		{
			gameFile.SaveContent(file.GetInternationalizedContent(Ships));

			MainWindow.IncreaseProgress(Sender, CurrentProgress++);
		}

		/// <summary>
		/// Adds a new line including an XSTR variable
		/// </summary>
		/// <param name="newMarker">Name of the new marker identifying the XSTR variable</param>
		/// <param name="match">Groups[1]: first original line (including \r\n), Groups[2]: hardcoded value to be translated, Groups[3]: line after the hardcoded value</param>
		/// <returns></returns>
		internal static string AddXstrLineToHardcodedValue(string newMarker, Match match)
		{
			// if marker already present, then don't touch anything
			if (match.Value.Contains(newMarker))
			{
				return match.Value;
			}
			else
			{
				string valueWithoutComment = match.Groups[2].Value.Split(';', 2, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
				string valueWithoutAlias = valueWithoutComment.Split('#', 2, StringSplitOptions.RemoveEmptyEntries)[0];
				return $"{match.Groups[1].Value}{newMarker}: XSTR(\"{valueWithoutAlias.Trim().TrimStart('@')}\", -1){Environment.NewLine}{match.Groups[3].Value}";
			}
		}

		/// <summary>
		/// Removes comments, alias and spaces from a name
		/// </summary>
		internal static string SanitizeName(string rawName, bool fullSanatizing = false)
		{
			if (fullSanatizing)
			{
				return rawName.Split(';')[0].Split('#')[0].Trim().TrimStart('@');
			}
			else
			{
				return rawName.Split(';')[0].Trim();
			}
		}

		internal static string GetValueWithoutXstr(string content)
		{
			content = content.Trim();

			if (content.Contains("XSTR"))
			{
				content = content.Split('\"')[1];
			}
			else
			{
				content = content.Replace("\"", "$quote");
			}

			return content.Split('#')[0].TrimStart('@');
		}
	}
}
