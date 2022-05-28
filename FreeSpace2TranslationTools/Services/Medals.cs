using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Medals : IFile
    {
        private readonly string OriginalContent;

        public Medals(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regex.Replace(OriginalContent, @"(\$Name:[ \t]*(.*?)\r\n)([^\r]*\$Bitmap)", new MatchEvaluator(XstrManager.GenerateAltNames), RegexOptions.Multiline | RegexOptions.Compiled);
        }
    }
}
