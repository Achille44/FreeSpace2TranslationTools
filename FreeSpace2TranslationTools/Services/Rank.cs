using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Rank : IFile
    {
        private readonly string OriginalContent;

        public Rank(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regex.Replace(OriginalContent, @"(.*\$Name:\s*)(.*?)\r\n", new MatchEvaluator(GenerateRanks), RegexOptions.Compiled);
        }

        private string GenerateRanks(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
