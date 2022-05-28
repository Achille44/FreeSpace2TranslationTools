using System;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Credits : IFile
    {
        private readonly string OriginalContent;

        public Credits(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regex.Replace(OriginalContent, @"(^)((?!(XSTR|\$|#End|#end)).+?)\r\n", new MatchEvaluator(GenerateCredits), RegexOptions.Multiline | RegexOptions.Compiled);
        }

        private string GenerateCredits(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
