using FreeSpace2TranslationTools.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Localization = FreeSpace2TranslationTools.Properties.Resources;

namespace FreeSpace2TranslationTools.Services
{
    public static class Utils
    {
        // REGEX HELP
        // (?=) : Positive lookahead. Matches a group after the main expression without including it in the result

        private static readonly Regex regexXstr = new("XSTR\\s*\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        // don't select entries in comment...
        private static readonly Regex regexNoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex regexAlternateTypes = new(@"#Alternate Types:.*?#end\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex regexModifyXstr = new("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex regexShowIcon = new(@"(SHOWICON.+xstrid=)(-?\d+)(.*$)", RegexOptions.Compiled);
        private static readonly Regex regexMsgXstr = new("(\".*?\" )(-?\\d+)", RegexOptions.Compiled);

        public static Regex RegexAlternateTypes { get => regexAlternateTypes; }
        public static Regex RegexNoAltNames { get => regexNoAltNames; }
        public static Regex RegexXstr { get => regexXstr;}

        public static List<GameFile> GetFilesWithXstrFromFolder(string folderPath)
        {
            List<string> files = new();
            List<GameFile> gameFiles = new();

            // First we look for tables, then modular tables (so that original tbl have less chance to see their ID changed in case of duplicates),
            // then we look for missions, to try to follow the translation conventions... and to avoid token problems in tables
            string[] tablesExtensions = new[] { ".tbl" };
            string[] modularTablesExtensions = new[] { ".tbm", ".cpp" };
            string[] missionsExtensions = new[] { ".fc2", ".fs2" };
            string[] fictionExtensions = new[] { Constants.FICTION_EXTENSION };

            files.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => tablesExtensions.Contains(Path.GetExtension(f))).ToList());

            files.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => modularTablesExtensions.Contains(Path.GetExtension(f))).ToList());

            files.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => missionsExtensions.Contains(Path.GetExtension(f))).ToList());

            files.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => fictionExtensions.Contains(Path.GetExtension(f))).ToList());

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

        public static void CreateFileWithNewContent(string sourceFile, string modFolder, string destinationFolder, string content)
        {
            // take care to keep the potential subfolders...
            string filePath = sourceFile.Replace(modFolder, destinationFolder);

            CreateFileWithPath(filePath, content);
        }

        public static void CreateFileWithPath(string filePath, string content)
        {
            string destDirectoryPath = Path.GetDirectoryName(filePath);

            // create the potential subfolders in the destination
            Directory.CreateDirectory(destDirectoryPath);

            File.WriteAllText(filePath, content);
        }

        public static string ReplaceContentWithNewXstr(string content, Xstr lineToModify)
        {
            string newLine = string.Empty;

            if (lineToModify.FullLine.Contains("modify-variable-xstr"))
            {
                newLine = regexModifyXstr.Replace(lineToModify.FullLine,
                    match => $"{match.Groups[1].Value}{lineToModify.Id}{match.Groups[3].Value}");
            }
            else if (lineToModify.FullLine.StartsWith("XSTR"))
            {
                newLine = RegexXstr.Replace(lineToModify.FullLine,
                    match => $"XSTR({match.Groups[1].Value}, {lineToModify.Id})");
            }
            else if (lineToModify.FullLine.StartsWith(VisualNovel.SHOWICON_MARKER))
            {
                newLine = regexShowIcon.Replace(lineToModify.FullLine,
                    match => $"{match.Groups[1].Value}{lineToModify.Id}{match.Groups[3].Value}");
            }
            else
            {
                newLine = regexMsgXstr.Replace(lineToModify.FullLine,
                    match => $"{match.Groups[1].Value}{lineToModify.Id}");
            }

            return content.Replace(lineToModify.FullLine, newLine);
        }

        /// <summary>
        /// Removes comments, alias and spaces from a name
        /// </summary>
        public static string SanitizeName(string rawName, bool fullSanatizing = false)
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
    }
}
