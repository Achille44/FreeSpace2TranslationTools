using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FreeSpace2TranslationTools.Utils
{
    public static class Utils
    {
        public static string ReadFileContent(string fileLocation)
        {
            string fileContent = string.Empty;

            try
            {
                using StreamReader reader = new StreamReader(fileLocation);
                fileContent = reader.ReadToEnd();
            }
            catch (Exception ex)
            {

            }

            return fileContent;
        }
    }
}
