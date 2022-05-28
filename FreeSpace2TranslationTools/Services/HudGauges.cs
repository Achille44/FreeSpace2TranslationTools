using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class HudGauges : IFile
    {
        private readonly string OriginalContent;

        public HudGauges(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regex.Replace(OriginalContent, @"(.*?Text:[ \t]*)((?!XSTR).*)\r?\n", new MatchEvaluator(GenerateHudGauges), RegexOptions.Compiled);
        }

        private string GenerateHudGauges(Match match)
        {
            // Always Show Text is a boolean, so don't treat this case
            if (match.Value.Contains("Always Show Text"))
            {
                return match.Value;
            }
            else
            {
                return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
            }
        }
    }
}
