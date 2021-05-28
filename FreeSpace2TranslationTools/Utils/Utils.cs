using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Localization = FreeSpace2TranslationTools.Properties.Resources;

namespace FreeSpace2TranslationTools.Utils
{
    public static class Utils
    {
        public static Regex RegexNoAltNames = new(@"(\$Name:\s*(.*?)\r\n(?:\+nocreate\r\n)?)(((?!\$Alt Name).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex RegexAlternateTypes = new(@"#Alternate Types:.*?#end\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);

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

        private static void CreateFileWithPath(string filePath, string content)
        {
            string destDirectoryPath = Path.GetDirectoryName(filePath);

            // create the potential subfolders in the destination
            Directory.CreateDirectory(destDirectoryPath);

            File.WriteAllText(filePath, content);
        }
    }
}
