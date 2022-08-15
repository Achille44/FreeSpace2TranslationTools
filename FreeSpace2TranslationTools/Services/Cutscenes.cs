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
            return Regexp.HardcodedNames.Replace(OriginalContent, new MatchEvaluator(GenerateInternationalizedCutscenes));
        }

        private string GenerateInternationalizedCutscenes(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
