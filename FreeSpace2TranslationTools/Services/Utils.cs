using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    public static class Utils
    {
        // REGEX HELP
        // (?=) : Positive lookahead. Matches a group after the main expression without including it in the result

        private static readonly Regex regexXstr = new("XSTR\\s*\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        // don't select entries in comment...
        private static readonly Regex regexNoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);

        public static Regex RegexNoAltNames { get => regexNoAltNames; }
        public static Regex RegexXstr { get => regexXstr;}
    }
}
