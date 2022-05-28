using System;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Campaign : IFile
    {
        private readonly string OriginalContent;

        public Campaign(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regex.Replace(OriginalContent, @"(.*?\$Name: )((?!XSTR).*)\r\n", new MatchEvaluator(GenerateCampaignNames), RegexOptions.Compiled);
        }

        private string GenerateCampaignNames(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
