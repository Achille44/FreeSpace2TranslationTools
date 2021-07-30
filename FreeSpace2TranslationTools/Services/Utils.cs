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
        public static Regex RegexXstr = new("XSTR\\s*\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        // don't select entries in comment...
        public static Regex RegexNoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        //public static Regex RegexNoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex RegexAlternateTypes = new(@"#Alternate Types:.*?#end\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex RegexModifyXstr = new("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Singleline | RegexOptions.Compiled);

        public static IEnumerable<Match> GetAllXstrFromFile(FileInfo fileInfo, string fileContent)
        {
            MatchCollection resultsFromFile = RegexXstr.Matches(fileContent);
            IEnumerable<Match> combinedResults = resultsFromFile.OfType<Match>().Where(m => m.Success);

            // there is an additional specific format in fs2 files
            if (fileInfo.Extension == ".fs2")
            {
                MatchCollection modifyResults = Regex.Matches(fileContent, "\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline);

                combinedResults = resultsFromFile.OfType<Match>().Concat(modifyResults.OfType<Match>()).Where(m => m.Success);
            }

            return combinedResults;
        }

        public static List<string> GetFilesWithXstrFromFolder(string folderPath)
        {
            List<string> result = new();

            // First we look for tables, then we look for missions, to try to follow the translation conventions... and to avoid token problems in tables
            string[] tablesExtensions = new[] { ".tbl", ".tbm" };
            string[] missionsExtensions = new[] { ".fc2", ".fs2" };

            result.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => tablesExtensions.Contains(Path.GetExtension(f))).ToList());

            result.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(f => missionsExtensions.Contains(Path.GetExtension(f))).ToList());

            if (result.Count == 0)
            {
                throw new UserFriendlyException(Localization.NoValidFileInFolder);
            }

            return result;
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
                newLine = RegexModifyXstr.Replace(lineToModify.FullLine,
                    m => $"{m.Groups[1].Value}{lineToModify.Id}{m.Groups[3].Value}");
            }
            else
            {
                newLine = RegexXstr.Replace(lineToModify.FullLine,
                    m => $"XSTR({m.Groups[1].Value}, {lineToModify.Id})");
            }

            return content.Replace(lineToModify.FullLine, newLine);
        }


        /// <summary>
        /// Removes comments, alias and spaces from a name
        /// </summary>
        /// <param name="rawName"></param>
        /// <returns></returns>
        public static string SanitizeName(string rawName)
        {
            return rawName.Split(';')[0].Trim();
            //return rawName.Split(';')[0].Split('#')[0].Trim().TrimStart('@');
        }
    }
}
