using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public static class Regexp
    {
        // REGEX HELP
        // (?=) : Positive lookahead. Matches a group after the main expression without including it in the result
        // -----------------------------------------------------------------------------------------------------------

        private static readonly Regex _Xstr = new("XSTR\\s*\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        // don't select entries in comment...
        private static readonly Regex _NoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _AlternateTypes = new(@"#Alternate Types:.*?#end\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
        // ex: $Label: Alpha 1 ==> $Label: XSTR("Alpha 1", -1)
        private static readonly Regex _Labels = new(@"(.*\$label:\s*)((?!XSTR).*)\r\n", RegexOptions.Compiled);
        private static readonly Regex _CallSigns = new(@"(.*\$Callsign:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);
        // ex: $Name: Psamtik   ==>     $Name: Psamtik
        //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
        //                      ==>     $Class......
        private static readonly Regex _ShipNames = new(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _ShowSubtitle = new(@"show-subtitle\s+.*?\r\n[ \t]+\)", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _ParametersInSexp = new(@"(\s*)(.*?)(\s*\r\n)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _JumpNodeNames = new(@"(.*\$Jump Node Name:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);
        private static readonly Regex _Objects = new(@"#Objects.*#Wings", RegexOptions.Singleline | RegexOptions.Compiled);
        // ((?!\$Name).)* => all characters not containing \$Name
        private static readonly Regex _AltShips = new(@"\$Name:\s*(((?!\$Name).)*?)\s*(\r\n|;)((?!\$Name).)*?\$Alt:\s*(.*?)\s*\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        // for unknown reason in this case \r is captured, so we have to uncapture it.
        // in some cases Alt can have an empty value... 
        private static readonly Regex _AltTypes =new(@"\$Alt:\s*(((?!\$Alt).)+)(?=\r)", RegexOptions.Compiled);
        private static readonly Regex _Alt = new(@"\$Alt:.*?\r\n", RegexOptions.Singleline);
        private static readonly Regex _SpecialSenders = new("\"(#.*?)\"", RegexOptions.Compiled);
        private static readonly Regex _MessagesSection = new(@"#Messages.*#Reinforcements", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _Messages = new(@"\$Name:\s*(.*?)(?=;|\r)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _SubtitleTexts = new("(show-subtitle-text\\s*\r\n\\s*\")(.*?)\"", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _HudTexts = new("(\\( (?:hud-set-text|hud-set-directive).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _SendMessages = new(@"\( (send-random-message|send-message).*?\r?\n[ \t]*\)\r?\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex AlternateTypes { get => _AlternateTypes; }
        public static Regex NoAltNames { get => _NoAltNames; }
        public static Regex Xstr { get => _Xstr; }
        public static Regex Labels { get => _Labels; }
        public static Regex CallSigns { get => _CallSigns; }
        public static Regex ShipNames { get => _ShipNames; }
        public static Regex ShowSubtitle { get => _ShowSubtitle; }
        public static Regex ParametersInSexp { get => _ParametersInSexp; }
        public static Regex JumpNodeNames { get => _JumpNodeNames; }
        public static Regex Objects { get => _Objects; }
        public static Regex AltShips { get => _AltShips; }
        public static Regex AltTypes { get => _AltTypes; }
        public static Regex Alt { get => _Alt; }
        public static Regex SpecialSenders { get => _SpecialSenders; }
        public static Regex MessagesSection { get => _MessagesSection; }
        public static Regex Messages { get => _Messages; }
        public static Regex SubtitleTexts { get => _SubtitleTexts; }
        public static Regex HudTexts { get => _HudTexts; }
        public static Regex SendMessages { get => _SendMessages; }
    }
}
