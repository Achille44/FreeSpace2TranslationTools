using FreeSpace2TranslationTools.Services.Entries;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal abstract class Tables
	{
		protected List<string> AllTables { get; set; } = [];
		protected List<IEntry> Entries { get; set; } = [];
		protected string I18nFile { get; set; }

		public Tables(List<GameFile> files, string tableName, string modulartableSuffix)
		{
			// the tbl file must be treated last in this case, as here we go from highest priority to lowest.
			List<GameFile> tableFiles = [.. files.Where(f => f.Name.EndsWith(modulartableSuffix) && !f.Name.Contains(Constants.I18N_FILE_PREFIX))];
			tableFiles.AddRange([.. files.Where(f => f.Name.EndsWith(tableName))]);

			if (tableFiles.Count > 0)
			{
				I18nFile = tableFiles[0].Name.Replace(Path.GetFileName(tableFiles[0].Name), Constants.I18N_FILE_PREFIX + modulartableSuffix);

				foreach (GameFile file in tableFiles)
				{
					AllTables.Add(file.Content);
				}
			}
		}

		public virtual GameFile CreateInternationalizedTable()
		{
			if (Entries.Count == 0) return null;
			else
			{
				return new GameFile(I18nFile, GetInternationalizedContent());
			}
		}

		protected abstract void ExtractInternationalizationContent();
		protected abstract string GetInternationalizedContent();
	}
}
