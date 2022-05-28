using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Mainhall : IFile
    {
        private readonly string OriginalContent;

        public Mainhall(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            // replace all door descriptions without XSTR variable
            return Regex.Replace(OriginalContent, @"(.*\+Door description:\s*)((?!XSTR).*)\r\n", new MatchEvaluator(GenerateDoorDescriptions), RegexOptions.Compiled);
        }

        private string GenerateDoorDescriptions(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
