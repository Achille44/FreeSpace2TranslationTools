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
		private int CurrentProgress { get; set; }
		private List<Weapon> Weapons { get; set; }
		private List<EWeapon> EWeapons { get; set; }
		private List<Ship> Ships { get; set; }

		public XstrManager(MainWindow parent, object sender, List<GameFile> files, bool extractToSeparateFiles)
		{
			Parent = parent;
			Sender = sender;
			CurrentProgress = 0;
			Weapons = new List<Weapon>();
			EWeapons = new List<EWeapon>();
			Ships = new List<Ship>();
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
				// the tbl file must be treated last in this case, as here we go from highest priority to lowest.
				List<GameFile> files = Files.Where(f => f.Name.EndsWith(Constants.CUTSCENE_MODULAR_TABLE_SUFFIX) && !f.Name.Contains(Constants.I18N_FILE_PREFIX)).ToList();
				files.AddRange(Files.Where(f => f.Name.EndsWith("cutscenes.tbl")).ToList());

				if (files.Count > 0)
				{
					TblCutscenes tblCutscenes = new();
					string i18n = files[0].Name.Replace(Path.GetFileName(files[0].Name), Constants.I18N_FILE_PREFIX + Constants.CUTSCENE_MODULAR_TABLE_SUFFIX);

					foreach (GameFile file in files)
					{
						tblCutscenes.AllTables.Add(file.Content);
					}

					tblCutscenes.ExtractInternationalizationContent();

					if (tblCutscenes.Cutscenes.Count > 0)
					{
						Files.Add(new GameFile(i18n, tblCutscenes.GetContent()));
					}
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

		private void ProcessRankFile()
		{
			if (ExtractToSeparateFiles)
			{
				// the tbl file must be treated last in this case, as here we go from highest priority to lowest.
				List<GameFile> files = Files.Where(f => f.Name.EndsWith(Constants.RANK_MODULAR_TABLE_SUFFIX) && !f.Name.Contains(Constants.I18N_FILE_PREFIX)).ToList();
				files.AddRange(Files.Where(f => f.Name.EndsWith(Constants.RANK_TABLE)).ToList());

				if (files.Count > 0)
				{
					TblRank tblRank = new();
					string i18n = files[0].Name.Replace(Path.GetFileName(files[0].Name), Constants.I18N_FILE_PREFIX + Constants.RANK_MODULAR_TABLE_SUFFIX);

					foreach (GameFile file in files)
					{
						tblRank.AllTables.Add(file.Content);
					}

					tblRank.ExtractInternationalizationContent();

					if (tblRank.Ranks.Count > 0)
					{
						Files.Add(new GameFile(i18n, tblRank.GetContent()));
					}
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
				// the tbl file must be treated last in this case, as here we go from highest priority to lowest.
				List<GameFile> shipFiles = Files.Where(f => f.Name.EndsWith(Constants.SHIP_MODULAR_TABLE_SUFFIX) && !f.Name.Contains(Constants.I18N_FILE_PREFIX)).ToList();
				shipFiles.AddRange(Files.Where(f => f.Name.EndsWith("ships.tbl")).ToList());

				if (shipFiles.Count > 0)
				{
					TblShips tblShips = new(EWeapons);
					string i18n = shipFiles[0].Name.Replace(Path.GetFileName(shipFiles[0].Name), Constants.I18N_FILE_PREFIX + Constants.SHIP_MODULAR_TABLE_SUFFIX);

					foreach (GameFile file in shipFiles)
					{
						tblShips.AllTables.Add(file.Content);
					}

					tblShips.ExtractInternationalizationContent();

					if (tblShips.Ships.Count > 0)
					{
						Files.Add(new GameFile(i18n, tblShips.GetContent()));
					}
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
				// the tbl file must be treated last in this case, as here we go from highest priority to lowest.
				List<GameFile> weaponFiles = Files.Where(f => f.Name.EndsWith(Constants.WEAPON_MODULAR_TABLE_SUFFIX) && !f.Name.Contains(Constants.I18N_FILE_PREFIX)).ToList();
				weaponFiles.AddRange(Files.Where(f => f.Name.EndsWith("weapons.tbl")));

				if (weaponFiles.Count > 0)
				{
					TblWeapons tblWeapons = new();
					string i18n = weaponFiles[0].Name.Replace(Path.GetFileName(weaponFiles[0].Name), Constants.I18N_FILE_PREFIX + Constants.WEAPON_MODULAR_TABLE_SUFFIX);

					foreach (GameFile file in weaponFiles)
					{
						tblWeapons.AllTables.Add(file.Content);
					}

					EWeapons = tblWeapons.ExtractInternationalizationContent();

					if (tblWeapons.Primaries.Count > 0 || tblWeapons.Secondaries.Count > 0)
					{
						Files.Add(new GameFile(i18n, tblWeapons.GetContent()));
					}
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
