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
        // (?<=...) : look behind
        // -----------------------------------------------------------------------------------------------------------

        private static readonly Regex _Xstr = new("XSTR\\s*\\(\\s*(\".*?\")\\s*,\\s*(-?\\d+)\\s*\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex Xstr { get => _Xstr; }

        // don't select entries in comment... but take into account comments between $Name and +nocreate
        private static readonly Regex _NoAltNames = new(@"([^;]\$Name:[ \t]*(.*?)\r\n(?:;.*?\r\n)?(?:[ \t]*\+nocreate[ \t]*\r\n)?)(((?!\$Alt Name|\+nocreate).)*?\r\n)", RegexOptions.Singleline | RegexOptions.Compiled);
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
        private static readonly Regex _MissionShipNames = new(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex MissionShipNames { get => _MissionShipNames; }

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
        private static readonly Regex _AltTypes = new(@"\$Alt:\s*(((?!\$Alt).)+)(?=\r)", RegexOptions.Compiled);
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

        private static readonly Regex _FirstSexpParameters = new("(add-nav-waypoint|addnav-ship|del-nav|hide-nav|restrict-nav|unhide-nav|unrestrict-nav|set-nav-visited|unset-nav-visited|select-nav|unselect-nav|is-nav-visited|lua-mark-ship|lua-mark-wing|ship-create).*?\"(.*?)\"", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex FirstSexpParameters { get => _FirstSexpParameters; }

        private static readonly Regex _SecondSexpParameters = new("(\\( (?:change-subsystem-name|lua-mark-subsystem).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex SecondSexpParameters { get => _SecondSexpParameters; }

        private static readonly Regex _Sexp = new("\\( (set-nav-color|Add-Object-Role).*?\\)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex Sexp { get => _Sexp; }

        private static readonly Regex _StringParameters = new("\"(.*?)\"", RegexOptions.Compiled);
        public static Regex StringParameters { get => _StringParameters; }

        private static readonly Regex _BeforeSexp = new("(^.*?#Cutscenes|#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex BeforeSexp { get => _BeforeSexp; }

        private static readonly Regex _SexpVariablesSection = new("#Sexp_variables.*?(#Fiction Viewer|#Command Briefing)", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex SexpVariablesSection { get => _SexpVariablesSection; }

        private static readonly Regex _VariableIds = new(@"^\t\t\d", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex VariableIds { get => _VariableIds; }

        private static readonly Regex _EndOfVariablesSection = new(@"\)\r\n\r\n(#Fiction Viewer|#Command Briefing|#Sexp_containers|.*?#Cutscenes)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex EndOfVariablesSection { get => _EndOfVariablesSection; }

        private static readonly Regex _EventsSection = new(@"#Events.*?\r\n\r\n", RegexOptions.Singleline | RegexOptions.Compiled);
        public static Regex EventsSection { get => _EventsSection; }

        private static readonly Regex _HardcodedNames = new(@"(.*?\$Name:[ \t]*)((?!XSTR).*)\r\n", RegexOptions.Compiled);
        public static Regex HardcodedNames { get => _HardcodedNames; }

        private static readonly Regex _HardcodedMedalNames = new(@"(\$Name:[ \t]*(.*?)\r\n)([^\r]*\$Bitmap)", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex HardcodedMedalNames { get => _HardcodedMedalNames; }

        private static readonly Regex _HardcodedLines = new(@"(^)((?!(XSTR|\$|#End|#end)).+?)\r\n", RegexOptions.Multiline | RegexOptions.Compiled);
        public static Regex HardcodedLines { get => _HardcodedLines; }

        private static readonly Regex _HardcodedTexts = new(@"(.*?Text:[ \t]*)((?!XSTR).*)\r?\n", RegexOptions.Compiled);
        public static Regex HardcodedTexts { get => _HardcodedTexts; }

        private static readonly Regex _HardcodedDoorDescriptions = new(@"(.*\+Door description:\s*)((?!XSTR).*)\r\n", RegexOptions.Compiled);
        public static Regex HardcodedDoorDescriptions { get => _HardcodedDoorDescriptions; }

        private static readonly Regex _NotEmptyStrings = new("\".+?\"", RegexOptions.Compiled);
        public static Regex NotEmptyStrings { get => _NotEmptyStrings; }

        private static readonly Regex _LinesStartingWithAWord = new(@"^\w+", RegexOptions.Compiled);
        public static Regex LinesStartingWithAWord { get => _LinesStartingWithAWord; }

        private static readonly Regex _HardCodedAltNames = new(@"([^;]\$Alt Name:[ \t]*)((?!XSTR).*)\r\n", RegexOptions.Compiled);
        public static Regex HardCodedAltNames { get => _HardCodedAltNames; }

        private static readonly Regex _Titles = new(@"(\+Title:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);
        public static Regex Titles { get => _Titles; }

        private static readonly Regex _Descriptions = new(@"(\+Description:[ \t]*)(.*?)\r\n(?=\$end_multi_text)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex Descriptions { get => _Descriptions; }

        private static readonly Regex _Weapons = new(@"\$Name:\s*.*?(?=\$Name|#end)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex Weapons { get => _Weapons; }

        private static readonly Regex _NoTechTitles = new(@"(\$Name:\s*(.*?)\r\n.*?\r\n)(\s*\+Tech Anim:|\s*\+Tech Description:)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex NoTechTitles { get => _NoTechTitles; }

        private static readonly Regex _NoTitles = new(@"(\$Name:\s*(.*?)\r\n.*?\r\n)(\s*\+Description:)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex NoTitles { get => _NoTitles; }

        private static readonly Regex _WeaponNames = new(@"\$Name:[ \t]*([^\r]*)", RegexOptions.Compiled);
        public static Regex WeaponNames { get => _WeaponNames; }

        private static readonly Regex _Flags = new("\\$Flags:(.*?)\"[ \t]*\\)", RegexOptions.Compiled);
        public static Regex Flags { get => _Flags; }

        private static readonly Regex _ShipSection = new(@"#Ship Classes.*?#end", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        public static Regex ShipSection { get => _ShipSection; }

        private static readonly Regex _ShipEntries = new(@"\n\$Name:.*?(?=\n\$Name|#end)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        public static Regex ShipEntries { get => _ShipEntries; }

        private static readonly Regex _ShipNames = new(@"\$Name:(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        public static Regex ShipNames { get => _ShipNames; }

        private static readonly Regex _Subsystems = new(@"(?<!;)(\$Subsystem:[ \t]*([^\r\n]*?),[^\r\n]*?\r?\n)(.*?)(?=\$Subsystem:|$)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex Subsystems { get => _Subsystems; }

        private static readonly Regex _TechDescriptions = new(@"(\+Tech Description:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);
        public static Regex TechDescriptions { get => _TechDescriptions; }

        // the main problem is that there are two different +Length properties, and only one of them should be translated (the one before $thruster property)
        private static readonly Regex _ShipLength = new(@"(\$Name:(?:(?!\$Name:|\$Thruster).)*?\r\n)([ \t]*\+Length:[ \t]*)([^\r]*?)(\r\n)", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex ShipLength { get => _ShipLength; }

        private static readonly Regex _SubsystemNames = new(@"(\$Subsystem:[ \t]*(.*?),.*?\n)(.*?)", RegexOptions.Compiled);
        public static Regex SubsystemNames { get => _SubsystemNames; }

        private static readonly Regex _AltSubsystemNames = new(@"(.*\$Alt Subsystem Name:[ \t]*)(.*)\r?\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex AltSubsystemNames { get => _AltSubsystemNames; }

        private static readonly Regex _InternationalizedAltSubsystemNamesWithFollowingLine = new("(\\$Alt Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)\\r?\\n)(.*?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex InternationalizedAltSubsystemNamesWithFollowingLine { get => _InternationalizedAltSubsystemNamesWithFollowingLine; }

        // take 2 lines after subsystem to skip alt subsystem line
        private static readonly Regex _SubsystemsWithAltSubsystems = new(@"(\$Subsystem:[ \t]*(.*?),.*?\n.*?\n)(.*?)", RegexOptions.Compiled);
        public static Regex SubsystemsWithAltSubsystems { get => _SubsystemsWithAltSubsystems; }

        private static readonly Regex _InternationalizedSubsystemNames = new(@"\$Alt Subsystem Name:[ \t]*XSTR", RegexOptions.IgnoreCase);
        public static Regex InternationalizedSubsystemNames { get => _InternationalizedSubsystemNames; }

        // [ \t] because \s includes \r and \n
        private static readonly Regex _AltDamagePopupSubsystemNames = new(@"(.*\$Alt Damage Popup Subsystem Name:[ \t]*)(.*)\r?\n", RegexOptions.Compiled);
        public static Regex AltDamagePopupSubsystemNames { get => _AltDamagePopupSubsystemNames; }

        private static readonly Regex _InternationalizedAltDamagePopupSubsystemNames = new("\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", RegexOptions.Compiled);
        public static Regex InternationalizedAltDamagePopupSubsystemNames { get => _InternationalizedAltDamagePopupSubsystemNames; }

        private static readonly Regex _InternationalizedAltSubsystemNames = new("\\$Alt Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex InternationalizedAltSubsystemNames { get => _InternationalizedAltSubsystemNames; }

        private static readonly Regex _DefaultPBanks = new("\\$Default PBanks:[ \t]*\\([ \t]*\"(.*?)\"", RegexOptions.Compiled);
        public static Regex DefaultPBanks { get => _DefaultPBanks; }
        
        private static readonly Regex _XstrInTstrings = new("(\\d+), (\".*?\")", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex XstrInTstrings { get => _XstrInTstrings; }

        private static readonly Regex _OnlyDigits = new("[^0-9.-]+", RegexOptions.Compiled);
        public static Regex OnlyDigits { get => _OnlyDigits; }

        private static readonly Regex _ModifyVariableXstr = new("(\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex ModifyVariableXstr { get => _ModifyVariableXstr; }

        private static readonly Regex _StringAndId = new("(\".*?\" )(-?\\d+)", RegexOptions.Compiled);
        public static Regex StringAndId { get => _StringAndId; }

        private static readonly Regex _MsgXstr = new("(?<=MSGXSTR.+)(\".+?\") (-?\\d+)", RegexOptions.Compiled);
        public static Regex MsgXstr { get => _MsgXstr; }

        private static readonly Regex _ShowIcon = new(@"(SHOWICON.+?text=("".+?"").+?xstrid=)(-?\d+)(.*$)", RegexOptions.Compiled | RegexOptions.Multiline);
        public static Regex ShowIcon { get => _ShowIcon; }

        private static readonly Regex _TechAddIntelXstr = new("(\\(\\s*tech-add-intel-xstr\\s*(\".*?\")\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex TechAddIntelXstr { get => _TechAddIntelXstr; }


        public static Regex GetJumpNodeReferences(string jumpNode)
        {
            return new($"(?<=\\([ \t]*(depart-node-delay|show-jumpnode|hide-jumpnode|set-jumpnode-color|set-jumpnode-name|set-jumpnode-model)[^\\(]*)\"{jumpNode}\"", RegexOptions.Singleline);
        }
    }
}
