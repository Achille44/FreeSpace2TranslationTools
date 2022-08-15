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
            return Regexp.HardcodedDoorDescriptions.Replace(OriginalContent, new MatchEvaluator(GenerateInternationalizedDoorDescriptions));
        }

        private string GenerateInternationalizedDoorDescriptions(Match match)
        {
            return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
