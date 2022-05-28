using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Cutscenes : IFile
    {
        private readonly string OriginalContent;

        public Cutscenes(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regex.Replace(OriginalContent, @"(\$Name:[ \t]*)(.*?)\r\n", new MatchEvaluator(GenerateCutscenes), RegexOptions.Compiled);
        }

        private string GenerateCutscenes(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
