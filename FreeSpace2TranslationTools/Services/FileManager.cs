using FreeSpace2TranslationTools.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Localization = FreeSpace2TranslationTools.Properties.Resources;

namespace FreeSpace2TranslationTools.Services
{
    internal static class FileManager
    {
        internal static List<GameFile> GetFilesWithXstrFromFolder(string folderPath)
        {
            List<string> files = [];
            List<GameFile> gameFiles = [];
			string[] sourceFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

			// First we look for tables, then modular tables (so that original tbl have less chance to see their ID changed in case of duplicates),
			// then we look for missions, to try to follow the translation conventions... and to avoid token problems in tables
			string[] tablesExtensions = [Constants.TABLE_EXTENSION];
            string[] modularTablesExtensions = [Constants.MODULAR_TABLE_EXTENSION, Constants.SOURCE_CODE_EXTENSION];
            string[] missionsExtensions = [Constants.CAMPAIGN_EXTENSION, Constants.MISSION_EXTENSION];
            string[] fictionExtensions = [Constants.FICTION_EXTENSION];

			files.AddRange([.. sourceFiles.Where(f => tablesExtensions.Contains(Path.GetExtension(f)))]);
            files.AddRange([.. sourceFiles.Where(f => modularTablesExtensions.Contains(Path.GetExtension(f)))]);
			files.AddRange([.. sourceFiles.Where(f => missionsExtensions.Contains(Path.GetExtension(f)))]);
            files.AddRange([.. sourceFiles.Where(f => fictionExtensions.Contains(Path.GetExtension(f)))]);

            if (files.Count == 0)
            {
                throw new UserFriendlyException(Localization.NoValidFileInFolder);
            }

            foreach (string file in files)
            {
                gameFiles.Add(new GameFile(file));
            }

            return gameFiles;
        }

        public static void CreateFileWithPath(string filePath, string content)
        {
            string destDirectoryPath = Path.GetDirectoryName(filePath);

            // create the potential subfolders in the destination
            Directory.CreateDirectory(destDirectoryPath);

            File.WriteAllText(filePath, content);
        }
    }
}
