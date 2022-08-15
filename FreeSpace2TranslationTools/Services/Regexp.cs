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
        public static Regex Xstr { get => _Xstr; }

        // don't select entries in comment...
        private static readonly Regex _NoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex NoAltNames { get => _NoAltNames; }

        private static readonly Regex _AlternateTypes = new(@"#Alternate Types:.*?#end\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex AlternateTypes { get => _AlternateTypes; }
        // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
        // ex: $Label: Alpha 1 ==> $Label: XSTR("Alpha 1", -1)
        private static readonly Regex _Labels = new(@"(.*\$label:\s*)((?!XSTR).*)\r\n", RegexOptions.Compiled);
        public static Regex Labels { get => _Labels; }

        private static readonly Regex _CallSigns = new(@"(.*\$Callsign:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);
        public static Regex CallSigns { get => _CallSigns; }

        // ex: $Name: Psamtik   ==>     $Name: Psamtik
        //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
        //                      ==>     $Class......
        private static readonly Regex _ShipNames = new(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex ShipNames { get => _ShipNames; }

        private static readonly Regex _ShowSubtitle = new(@"show-subtitle\s+.*?\r\n[ \t]+\)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex ShowSubtitle { get => _ShowSubtitle; }

        private static readonly Regex _ParametersInSexp = new(@"(\s*)(.*?)(\s*\r\n)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex ParametersInSexp { get => _ParametersInSexp; }

        private static readonly Regex _JumpNodeNames = new(@"(.*\$Jump Node Name:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);
        public static Regex JumpNodeNames { get => _JumpNodeNames; }

        private static readonly Regex _Objects = new(@"#Objects.*#Wings", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex Objects { get => _Objects; }

        // ((?!\$Name).)* => all characters not containing \$Name
        private static readonly Regex _AltShips = new(@"\$Name:\s*(((?!\$Name).)*?)\s*(\r\n|;)((?!\$Name).)*?\$Alt:\s*(.*?)\s*\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex AltShips { get => _AltShips; }

        // for unknown reason in this case \r is captured, so we have to uncapture it.
        // in some cases Alt can have an empty value... 
        private static readonly Regex _AltTypes =new(@"\$Alt:\s*(((?!\$Alt).)+)(?=\r)", RegexOptions.Compiled);
        public static Regex AltTypes { get => _AltTypes; }

        private static readonly Regex _Alt = new(@"\$Alt:.*?\r\n", RegexOptions.Singleline);
        public static Regex Alt { get => _Alt; }

        private static readonly Regex _SpecialSenders = new("\"(#.*?)\"", RegexOptions.Compiled);
        public static Regex SpecialSenders { get => _SpecialSenders; }

        private static readonly Regex _MessagesSection = new(@"#Messages.*#Reinforcements", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex MessagesSection { get => _MessagesSection; }

        private static readonly Regex _Messages = new(@"\$Name:\s*(.*?)(?=;|\r)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex Messages { get => _Messages; }

        private static readonly Regex _SubtitleTexts = new("(show-subtitle-text\\s*\r\n\\s*\")(.*?)\"", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex SubtitleTexts { get => _SubtitleTexts; }

        private static readonly Regex _HudTexts = new("(\\( (?:hud-set-text|hud-set-directive).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex HudTexts { get => _HudTexts; }

        private static readonly Regex _SendMessages = new(@"\( (send-random-message|send-message).*?\r?\n[ \t]*\)\r?\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex SendMessages { get => _SendMessages; }

        private static readonly Regex _FromObjectsToWaypoints = new("#Objects.*#Waypoints", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex FromObjectsToWaypoints { get => _FromObjectsToWaypoints; }

        private static Regex _FirstSexpParameters = new("(add-nav-waypoint|addnav-ship|del-nav|hide-nav|restrict-nav|unhide-nav|unrestrict-nav|set-nav-visited|unset-nav-visited|select-nav|unselect-nav|is-nav-visited|lua-mark-ship|lua-mark-wing).*?\"(.*?)\"", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex FirstSexpParameters { get => _FirstSexpParameters; }

        private static Regex _SecondSexpParameters = new("(\\( (?:change-subsystem-name|lua-mark-subsystem).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex SecondSexpParameters { get => _SecondSexpParameters; }

        private static Regex _Sexp = new("\\( (set-nav-color|Add-Object-Role).*?\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex Sexp { get => _Sexp; }

        private static Regex _StringParameters = new("\"(.*?)\"", RegexOptions.Compiled);
        public static Regex StringParameters { get => _StringParameters; }

        private static Regex _BeforeSexp = new("(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex BeforeSexp { get => _BeforeSexp; }

        private static Regex _SexpVariablesSection = new("#Sexp_variables.*?(#Fiction Viewer|#Command Briefing)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex SexpVariablesSection { get => _SexpVariablesSection; }

        private static Regex _VariableIds = new(@"^\t\t\d", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex VariableIds { get => _VariableIds; }

        private static Regex _EndOfVariablesSection = new(@"\)\r\n\r\n(#Fiction Viewer|#Command Briefing|#Cutscenes)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex EndOfVariablesSection { get => _EndOfVariablesSection; }

        private static Regex _EventsSection = new(@"#Events.*?\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex EventsSection { get => _EventsSection; }


        public static Regex GetJumpNodeReferences(string jumpNode)
        {
            // (?<=...) => look behind
            return new($"(?<=\\([ \t]*(depart-node-delay|show-jumpnode|hide-jumpnode|set-jumpnode-color|set-jumpnode-name|set-jumpnode-model)[^\\(]*)\"{jumpNode}\"", RegexOptions.Singleline);
        }
    }
}
