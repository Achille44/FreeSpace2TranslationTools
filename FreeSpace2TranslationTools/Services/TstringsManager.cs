using FreeSpace2TranslationTools.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeSpace2TranslationTools.Services
{
	public class TstringsManager
	{
		private MainWindow Parent { get; set; }
		private object Sender { get; set; }
		private string ModFolder { get; set; }
		private string DestinationFolder { get; set; }
		private IReadOnlyCollection<GameFile> Files { get; set; }
		private List<IXstr> Lines { get; set; }
		private List<IXstr> Duplicates { get; set; }
		private bool ManageDuplicates { get; set; }
		private string StartingID { get; set; }
		private bool ExtractToSeparateFiles { get; }
		private int CurrentProgress { get; set; }

		public TstringsManager(MainWindow parent, object sender, string modFolder, string destinationFolder, bool manageDuplicates, List<GameFile> files, string startingID, bool extractToSeparateFiles)
		{
			Parent = parent;
			Sender = sender;
			ModFolder = modFolder;
			DestinationFolder = destinationFolder;
			CurrentProgress = 0;
			ManageDuplicates = manageDuplicates;
			StartingID = startingID;
			ExtractToSeparateFiles = extractToSeparateFiles;
			Lines = new();
			Duplicates = new();
			Files = files;

			MainWindow.InitializeProgress(Sender);
			Parent.SetMaxProgress(Files.Count);
		}

		#region public methods

		public void ProcessTstrings()
		{
			MainWindow.InitializeProgress(Sender);

			FetchXstr();

			#region Manage duplicates
			if (ManageDuplicates && Duplicates.Count > 0)
			{
				CreateFileForDuplicates();

				CreateModFilesWithNewIds();
			}
			#endregion

			foreach (GameFile file in Files)
			{
				file.CreateFileWithNewContent(ModFolder, DestinationFolder);
			}

			CreateTstringsFile();
		}
		#endregion

		/// <summary>
		/// look for xstr in each file
		/// </summary>
		private void FetchXstr()
		{
			IEnumerable<GameFile> tstringsFiles = Files.Where(x => x.Name.EndsWith(Constants.TSTRINGS_MODULAR_TABLE_SUFFIX) || x.Name.EndsWith(Constants.TSTRINGS_TABLE));

			foreach (GameFile file in tstringsFiles)
			{
				try
				{
					IEnumerable<IXstr> allXstr = file.GetAllXstr();

					foreach (IXstr xstr in allXstr)
					{
						// if id not existing, add a new line
						if (xstr.Id >= 0 && !Lines.Any(x => x.Id == xstr.Id))
						{
							Lines.Add(xstr);
						}
					}
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}

			List<GameFile> compatibleFiles = Files.Where(x => 
				!x.Name.EndsWith(Constants.STRINGS_MODULAR_TABLE_SUFFIX) 
				&& !x.Name.EndsWith(Constants.TSTRINGS_MODULAR_TABLE_SUFFIX) 
				&& !x.Name.EndsWith(Constants.STRINGS_TABLE)).ToList();

			if (ExtractToSeparateFiles)
			{
				compatibleFiles = compatibleFiles.Where(x => 
					(
						!x.Name.EndsWith(Constants.SHIP_MODULAR_TABLE_SUFFIX)
						&& !x.Name.EndsWith(Constants.WEAPON_MODULAR_TABLE_SUFFIX)
						&& !x.Name.EndsWith(Constants.CUTSCENE_MODULAR_TABLE_SUFFIX)
						&& !x.Name.EndsWith(Constants.RANK_MODULAR_TABLE_SUFFIX)
					)
					|| x.Name.Contains(Constants.I18N_FILE_PREFIX)).ToList();
			}

			List<GameFile> orderedFiles = new();

			// First we look for tables, then modular tables (so that original tbl have less chance to see their ID changed in case of duplicates),
			// then we look for missions, to try to follow the translation conventions... and to avoid token problems in tables
			string[] tablesExtensions = new[] { Constants.TABLE_EXTENSION };
			string[] modularTablesExtensions = new[] { Constants.MODULAR_TABLE_EXTENSION, Constants.SOURCE_CODE_EXTENSION };
			string[] missionsExtensions = new[] { Constants.CAMPAIGN_EXTENSION, Constants.MISSION_EXTENSION };
			string[] fictionExtensions = new[] { Constants.FICTION_EXTENSION };

			orderedFiles.AddRange(compatibleFiles.Where(f => tablesExtensions.Contains(Path.GetExtension(f.Name))).ToList());
			orderedFiles.AddRange(compatibleFiles.Where(f => modularTablesExtensions.Contains(Path.GetExtension(f.Name))).ToList());
			orderedFiles.AddRange(compatibleFiles.Where(f => missionsExtensions.Contains(Path.GetExtension(f.Name))).ToList());
			orderedFiles.AddRange(compatibleFiles.Where(f => fictionExtensions.Contains(Path.GetExtension(f.Name))).ToList());

			foreach (GameFile file in orderedFiles)
			{
				try
				{
					IEnumerable<IXstr> allXstr = file.GetAllXstr();

					foreach (IXstr xstr in allXstr)
					{
						// remove replaceable xstr so that the new version can be added
						Lines.Remove(Lines.FirstOrDefault(x => x.Id == xstr.Id && x.Replaceable));

						// if id not existing, add a new line
						if (xstr.Id >= 0 && !Lines.Any(x => x.Id == xstr.Id))
						{
							Lines.Add(xstr);
						}
						// if id already existing but value is different, then put it in another list that will be treated separately
						else if (ManageDuplicates && (xstr.Id < 0 || Lines.First(x => x.Id == xstr.Id).Text != xstr.Text || xstr.UniqueId))
						{
							Duplicates.Add(xstr);
						}
					}
				}
				catch (Exception ex)
				{
					throw new FileException(ex, file.Name);
				}
			}

			int maxProgress = Lines.Count + (ManageDuplicates ? Files.Count + Duplicates.Count : 0);
			Parent.SetMaxProgress(maxProgress);
		}

		private void CreateFileForDuplicates()
		{
			// new ID = max ID + 1 to avoid duplicates
			int newId = SetNextID();

			string currentFile = "";
			StringBuilder i18nContent = new();
			i18nContent.Append($"#Default{Environment.NewLine}");

			foreach (IXstr duplicate in Duplicates)
			{
				IXstr originalXstr = Lines.FirstOrDefault(x => x.Text == duplicate.Text && !x.UniqueId);

				// if duplicated text exists in another xstr in the original file, then copy its ID
				if (originalXstr != null && !duplicate.UniqueId)
				{
					duplicate.Id = originalXstr.Id;
					duplicate.Treated = true;
				}
				// if there is another duplicate with the same text, we can reuse the same ID to avoid new duplicates in the new file
				else if (i18nContent.ToString().Contains(duplicate.Text) && !duplicate.UniqueId)
				{
					IXstr result = Duplicates.FirstOrDefault(x => x.Treated && x.Text == duplicate.Text);

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
						i18nContent.Append($"{Environment.NewLine}; {duplicate.FileName + Environment.NewLine}");
					}

					i18nContent.Append($"{Environment.NewLine + duplicate.Id}, {duplicate.Text + duplicate.Comments + Environment.NewLine}");
					duplicate.Treated = true;
				}

				MainWindow.IncreaseProgress(Sender, CurrentProgress++);
			}

			i18nContent.Append($"{Environment.NewLine}#End");

			FileManager.CreateFileWithPath(Path.Combine(DestinationFolder, $"tables/{Constants.I18N_FILE_PREFIX + Constants.TSTRINGS_MODULAR_TABLE_SUFFIX}"), i18nContent.ToString());
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
			IReadOnlyCollection<string> filesToModify = Duplicates.Select(x => x.FilePath).Distinct().ToList();

			foreach (string sourceFile in filesToModify)
			{
				GameFile gameFile = Files.FirstOrDefault(file => file.Name == sourceFile);

				string fileName = Path.GetFileName(sourceFile);
				string newContent = gameFile.Content;

				foreach (IXstr xstr in Duplicates.Where(x => x.FileName == fileName))
				{
					newContent = xstr.ReplaceContentWithNewXstrId(newContent);
				}

				gameFile.SaveContent(newContent);

				MainWindow.IncreaseProgress(Sender, CurrentProgress++);
			}
		}

		/// <summary>
		/// Creates the tstrings.tbl file with original IDs
		/// </summary>
		private void CreateTstringsFile()
		{
			if (Lines.Count > 0)
			{
				string iterationFile = string.Empty;
				StringBuilder content = new();
				content.Append($"#Default{Environment.NewLine}");

				foreach (IXstr line in Lines.OrderBy(x => x.Id))
				{
					// add the name of the file in comment, except for original tstrings tables
					if (iterationFile != line.FileName && line.GetType().Name != nameof(XstrTstrings))
					{
						iterationFile = line.FileName;
						content.Append($"{Environment.NewLine}; {line.FileName + Environment.NewLine}");
					}

					content.Append($"{Environment.NewLine + line.Id}, {line.Text + line.Comments + Environment.NewLine}");

					MainWindow.IncreaseProgress(Sender, CurrentProgress++);
				}

				content.Append($"{Environment.NewLine}#End");
				FileManager.CreateFileWithPath(Path.Combine(DestinationFolder, $"tables/{Constants.TSTRINGS_TABLE}"), content.ToString());
			}
		}
	}
}
