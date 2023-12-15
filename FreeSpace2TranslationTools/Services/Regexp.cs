using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
	public static partial class Regexp
    {
        // REGEX HELP
        // (?=): Positive lookahead. Matches a group after the main expression without including it in the result
        // (?<=...): look behind
        // (?<!;): Negative lookbehind
        // (?:...): non capturing group
        // -----------------------------------------------------------------------------------------------------------

        //[GeneratedRegex("XSTR\\s*\\(\\s*(\"(?:(?!XSTR).)*?\")\\s*,\\s*(-?\\d+)\\s*(\\)|,)", RegexOptions.Singleline)]
		[GeneratedRegex("XSTR\\s*\\(\\s*(\"(?:(?!XSTR).)*?\")\\s*,\\s*(-?\\d+)\\s*(\\)|,)(.*?\\r?\\n)", RegexOptions.Singleline)]
        private static partial Regex _Xstr();
        public static Regex Xstr { get => _Xstr(); }

		// don't select entries in comment... but take into account comments between $Name and +nocreate
		//[GeneratedRegex("(?<!;)(\\$Name:[ \\t]*(.*?)\\r\\n(?:;.*?\\r\\n)?(?:[ \\t]*\\+nocreate[ \\t]*\\r\\n)?)(((?!\\$Alt Name|\\+nocreate).)*?\\r\\n)", RegexOptions.Singleline)]
		[GeneratedRegex("(?<!;)(\\$Name:[ \\t]*(.*?)\\r\\n)(.)")]
        private static partial Regex _NoAltNames();
        public static Regex NoAltNames { get => _NoAltNames(); }

        [GeneratedRegex("#Alternate Types:.*?#end\\r\\n\\r\\n", RegexOptions.Singleline)]
        private static partial Regex _AlternateTypes();
        public static Regex AlternateTypes { get => _AlternateTypes(); }

        // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
        // ex: $Label: Alpha 1 ==> $Label: XSTR("Alpha 1", -1)
        [GeneratedRegex("(.*\\$label:\\s*)((?!XSTR).*)\\r\\n")]
        private static partial Regex _Labels();
        public static Regex Labels { get => _Labels(); }

        [GeneratedRegex("(.*\\$Callsign:[ \\t]*)(.*?)\\r\\n")]
        private static partial Regex _CallSigns();
        public static Regex CallSigns { get => _CallSigns(); }

        // ex: $Name: Psamtik   ==>     $Name: Psamtik
        //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
        //                      ==>     $Class......
        [GeneratedRegex("(\\$Name:\\s*(.*?)\\r\\n)(\\$Class)", RegexOptions.Multiline)]
        private static partial Regex _MissionShipNames();
        public static Regex MissionShipNames { get => _MissionShipNames(); }

        [GeneratedRegex("show-subtitle\\s+.*?\\r\\n[ \\t]+\\)", RegexOptions.Singleline)]
        private static partial Regex _ShowSubtitle();
        public static Regex ShowSubtitle { get => _ShowSubtitle(); }

        [GeneratedRegex("(\\s*)(.*?)(\\s*\\r\\n)", RegexOptions.Multiline)]
        private static partial Regex _ParametersInSexp();
        public static Regex ParametersInSexp { get => _ParametersInSexp(); }

		// Only used for FSO < v23.0
		//[GeneratedRegex("(.*\\$Jump Node Name:[ \\t]*)(.*?)\\r\\n")]
		// Only used for FSO => v23.0
		[GeneratedRegex("(.*\\$Jump Node Name:[ \\t]*(.*?)\\r\\n)((?!\\+Display Name).)")]
        private static partial Regex _JumpNodeNames();
        public static Regex JumpNodeNames { get => _JumpNodeNames(); }

        [GeneratedRegex("#Objects.*#Wings", RegexOptions.Singleline)]
        private static partial Regex _Objects();
        public static Regex Objects { get => _Objects(); }

        // ((?!\$Name).)* => all characters not containing \$Name
        [GeneratedRegex("\\$Name:\\s*(((?!\\$Name).)*?)\\s*(\\r\\n|;)((?!\\$Name).)*?\\$Alt:\\s*(.*?)\\s*\\r\\n", RegexOptions.Singleline)]
        private static partial Regex _AltShips();
        public static Regex AltShips { get => _AltShips(); }

        // for unknown reason in this case \r is captured, so we have to uncapture it.
        // in some cases Alt can have an empty value...
        [GeneratedRegex("\\$Alt:\\s*(((?!\\$Alt).)+)(?=\\r)")]
        private static partial Regex _AltTypes();
        public static Regex AltTypes { get => _AltTypes(); }

        [GeneratedRegex("\\$Alt:.*?\\r\\n", RegexOptions.Singleline)]
        private static partial Regex _Alt();
        public static Regex Alt { get => _Alt(); }

        [GeneratedRegex("\"(#.*?)\"")]
        private static partial Regex _SpecialSenders();
        public static Regex SpecialSenders { get => _SpecialSenders(); }

        [GeneratedRegex("#Messages.*#Reinforcements", RegexOptions.Singleline)]
        private static partial Regex _MessagesSection();
        public static Regex MessagesSection { get => _MessagesSection(); }

		[GeneratedRegex("\\$Name:\\s*(.*?)(?=;|\\r)", RegexOptions.Multiline)]
		private static partial Regex _Messages();
		public static Regex Messages { get => _Messages(); }

		[GeneratedRegex("(?<=\\$Name:[ \\t]*)(.*?)(?=;|\\r)")]
        private static partial Regex _Names();
        public static Regex Names { get => _Names(); }

		[GeneratedRegex("(?<=\\+Name:[ \\t]*)(.*?)(?=;|\\r)")]
		private static partial Regex _MainhallNames();
		public static Regex MainhallNames { get => _MainhallNames(); }

		[GeneratedRegex("(?<=\\$Filename:[ \\t]*)(.*?)(?=;|\\r)")]
		private static partial Regex _FileNames();
		public static Regex FileNames { get => _FileNames(); }

		[GeneratedRegex("(?<=\\$Alt Name:[ \\t]*)(.*?)(?=;|\\r)")]
		private static partial Regex _AltNames();
		public static Regex AltNames { get => _AltNames(); }

		[GeneratedRegex("(?<=\\$Title:[ \\t]*)(.*?)(?=;|\\r)")]
		private static partial Regex _RankTitles();
		public static Regex RankTitles { get => _RankTitles(); }

		[GeneratedRegex("(?<=\\$Turret Name:[ \\t]*)(.*?)(?=;|\\r)", RegexOptions.IgnoreCase)]
		private static partial Regex _TurretNames();
		public static Regex TurretNames { get => _TurretNames(); }

		[GeneratedRegex("(show-subtitle-text\\s*\r\n\\s*\")(.*?)\"", RegexOptions.Multiline)]
        private static partial Regex _SubtitleTexts();
        public static Regex SubtitleTexts { get => _SubtitleTexts(); }

        [GeneratedRegex("(\\( (?:hud-set-text|hud-set-directive).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline)]
        private static partial Regex _HudTexts();
        public static Regex HudTexts { get => _HudTexts(); }

        [GeneratedRegex("\\( (send-random-message|send-message).*?\\r?\\n[ \\t]*\\)\\r?\\n", RegexOptions.Singleline)]
        private static partial Regex _SendMessages();
        public static Regex SendMessages { get => _SendMessages(); }

        [GeneratedRegex("#Objects.*#Waypoints", RegexOptions.Singleline)]
        private static partial Regex _FromObjectsToWaypoints();
        public static Regex FromObjectsToWaypoints { get => _FromObjectsToWaypoints(); }

        [GeneratedRegex("(add-nav-waypoint|addnav-ship|del-nav|hide-nav|restrict-nav|unhide-nav|unrestrict-nav|set-nav-visited|unset-nav-visited|select-nav|unselect-nav|is-nav-visited|lua-mark-ship|lua-mark-wing|ship-create).*?\"(.*?)\"", RegexOptions.Singleline)]
        private static partial Regex _FirstSexpParameters();
        public static Regex FirstSexpParameters { get => _FirstSexpParameters(); }

        [GeneratedRegex("(\\( (?:change-subsystem-name|lua-mark-subsystem).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline)]
        private static partial Regex _SecondSexpParameters();
        public static Regex SecondSexpParameters { get => _SecondSexpParameters(); }

        [GeneratedRegex("\\( (set-nav-color|Add-Object-Role).*?\\)", RegexOptions.Singleline)]
        private static partial Regex _Sexp();
        public static Regex Sexp { get => _Sexp(); }

        [GeneratedRegex("\"(.*?)\"")]
        private static partial Regex _StringParameters();
        public static Regex StringParameters { get => _StringParameters(); }

        [GeneratedRegex("(^.*?#Cutscenes|#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline)]
        private static partial Regex _BeforeSexp();
        public static Regex BeforeSexp { get => _BeforeSexp(); }

        [GeneratedRegex("#Sexp_variables.*?(#Fiction Viewer|#Command Briefing)", RegexOptions.Singleline)]
        private static partial Regex _SexpVariablesSection();
        public static Regex SexpVariablesSection { get => _SexpVariablesSection(); }

        [GeneratedRegex("^\\t\\t\\d", RegexOptions.Multiline)]
        private static partial Regex _VariableIds();
        public static Regex VariableIds { get => _VariableIds(); }

        [GeneratedRegex("^\\t\\t\\d+\\t\\t\"(.+?)\"\\t\\t\"(.+?)\"", RegexOptions.Multiline)]
        private static partial Regex _Variables();
        public static Regex Variables { get => _Variables(); }

        [GeneratedRegex("\\)\\r\\n\\r\\n(#Fiction Viewer|#Command Briefing|#Sexp_containers|.*?#Cutscenes)", RegexOptions.Multiline)]
        private static partial Regex _EndOfVariablesSection();
        public static Regex EndOfVariablesSection { get => _EndOfVariablesSection(); }

        [GeneratedRegex("#Events.*?\\r\\n\\r\\n", RegexOptions.Singleline)]
        private static partial Regex _EventsSection();
        public static Regex EventsSection { get => _EventsSection(); }

        [GeneratedRegex("(.*?\\$Name:[ \\t]*)((?!XSTR).*)\\r\\n")]
        private static partial Regex _HardcodedNames();
        public static Regex HardcodedNames { get => _HardcodedNames(); }

        [GeneratedRegex("(\\$Name:[ \\t]*(.*?)\\r\\n)([^\\r]*\\$Bitmap)", RegexOptions.Multiline)]
        private static partial Regex _HardcodedMedalNames();
        public static Regex HardcodedMedalNames { get => _HardcodedMedalNames(); }

        [GeneratedRegex("(^)((?!(XSTR|\\$|#end)).+?)\\r\\n", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
        private static partial Regex _HardcodedLines();
        public static Regex HardcodedLines { get => _HardcodedLines(); }

        [GeneratedRegex("(.*?Text:[ \\t]*)((?!XSTR).*)\\r?\\n")]
        private static partial Regex _HardcodedTexts();
        public static Regex HardcodedTexts { get => _HardcodedTexts(); }

        [GeneratedRegex("(.*\\+Door description:\\s*)((?!XSTR).*)\\r\\n")]
        private static partial Regex _HardcodedDoorDescriptions();
        public static Regex HardcodedDoorDescriptions { get => _HardcodedDoorDescriptions(); }

		[GeneratedRegex("(?<=\\+Door description:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _DoorDescriptions();
		public static Regex DoorDescriptions { get => _DoorDescriptions(); }

		[GeneratedRegex("\".+?\"")]
        private static partial Regex _NotEmptyStrings();
        public static Regex NotEmptyStrings { get => _NotEmptyStrings(); }

        [GeneratedRegex("^\\w+")]
        private static partial Regex _LinesStartingWithAWord();
        public static Regex LinesStartingWithAWord { get => _LinesStartingWithAWord(); }

        [GeneratedRegex("([^;]\\$Alt Name:[ \\t]*)((?!XSTR).*)\\r\\n")]
        private static partial Regex _HardCodedAltNames();
        public static Regex HardCodedAltNames { get => _HardCodedAltNames(); }

        [GeneratedRegex("([^;]\\$Turret Name:[ \\t]*)((?!XSTR).*)\\r\\n", RegexOptions.IgnoreCase)]
        private static partial Regex _HardCodedTurretNames();
        public static Regex HardCodedTurretNames { get => _HardCodedTurretNames(); }

        [GeneratedRegex("[^;]\\$Turret Name: XSTR\\(\\\"(.*)\", -1\\)\\r\\n", RegexOptions.IgnoreCase)]
        private static partial Regex _TurretNamesInsideXstr();
        public static Regex TurretNamesInsideXstr { get => _TurretNamesInsideXstr(); }

        [GeneratedRegex("(\\+Title:[ \\t]*)(.*?)\\r\\n")]
        private static partial Regex _TitlesFullLine();
        public static Regex TitlesFullLine { get => _TitlesFullLine(); }

		[GeneratedRegex("(?<=\\+Title:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _Titles();
		public static Regex Titles { get => _Titles(); }

		[GeneratedRegex("(?<=\\+Tech Title:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _TechTitles();
		public static Regex TechTitles { get => _TechTitles(); }

		[GeneratedRegex("(?<=\\+Type:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _Types();
		public static Regex Types { get => _Types(); }

		[GeneratedRegex("(?<=\\+Maneuverability:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _Maneuverabilities();
		public static Regex Maneuverabilities { get => _Maneuverabilities(); }

		[GeneratedRegex("(?<=\\+Armor:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _Armors();
		public static Regex Armors { get => _Armors(); }

		[GeneratedRegex("(?<=\\+Manufacturer:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _Manufacturers();
		public static Regex Manufacturers { get => _Manufacturers(); }

		[GeneratedRegex("(?<=^[^;]*\\+Length:[ \\t]*)(.*?)(?=$)", RegexOptions.Multiline)]
		private static partial Regex _Lengths();
		public static Regex Lengths { get => _Lengths(); }

		[GeneratedRegex("(?<=^[^;]*\\$Alt Subsystem Name:[ \\t]*)(.*?)(?=$)", RegexOptions.Multiline)]
		private static partial Regex _AltSubsystemNames();
		public static Regex AltSubsystemNames { get => _AltSubsystemNames(); }

		[GeneratedRegex("(?<=\\$Alt Damage Popup Subsystem Name:[ \\t]*)(.*?)(?=\\r)")]
		private static partial Regex _AltDamagePopupSubsystemNames();
		public static Regex AltDamagePopupSubsystemNames { get => _AltDamagePopupSubsystemNames(); }

		[GeneratedRegex("(\\+Description:[ \\t]*)(.*?)\\r\\n(?=\\$end_multi_text)", RegexOptions.Singleline)]
        private static partial Regex _DescriptionsFullLine();
        public static Regex DescriptionsFullLine { get => _DescriptionsFullLine(); }

		[GeneratedRegex("(?<=\\+Description:[ \\t]*)(.*?)(?=\\r\\n\\$end_multi_text)", RegexOptions.Singleline)]
		private static partial Regex _Descriptions();
		public static Regex Descriptions { get => _Descriptions(); }

		[GeneratedRegex("(?<=\\$Description:.*?\")(.*?)(?=\".*?\\r\\n\\$end_multi_text)", RegexOptions.Singleline)]
		private static partial Regex _CutsceneDescriptions();
		public static Regex CutsceneDescriptions { get => _CutsceneDescriptions(); }

		[GeneratedRegex("(?<=\\+Tech Description:[ \\t]*)(.*?)(?=\\r\\n\\$end_multi_text)", RegexOptions.Singleline)]
		private static partial Regex _TechDescriptions();
		public static Regex TechDescriptions { get => _TechDescriptions(); }

		[GeneratedRegex("(?<=\\$Promotion Text:.*?\")(.*?)(?=\".*?\\r\\n\\$end_multi_text)", RegexOptions.Singleline)]
		private static partial Regex _PromotionTexts();
		public static Regex PromotionTexts { get => _PromotionTexts(); }

		[GeneratedRegex("(?<!;)\\$Name:\\s*.*?(?=\\$Name|#end)", RegexOptions.IgnoreCase |RegexOptions.Singleline)]
        private static partial Regex _GenericEntries();
        public static Regex GenericEntries { get => _GenericEntries(); }

		[GeneratedRegex("(?<!;)\\+Name:\\s*.*?(?=\\+Name|#end)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
		private static partial Regex _MainhallEntries();
		public static Regex MainhallEntries { get => _MainhallEntries(); }

		[GeneratedRegex("(?<!;)\\$Filename:\\s*.*?(?=\\$Filename|#end)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
		private static partial Regex _CutsceneEntries();
		public static Regex CutsceneEntries { get => _CutsceneEntries(); }

		[GeneratedRegex("(\\$Name:\\s*(.*?)\\r\\n.*?\\r\\n)(\\s*\\+Tech Anim:|\\s*\\+Tech Description:)", RegexOptions.Singleline)]
        private static partial Regex _NoTechTitles();
        public static Regex NoTechTitles { get => _NoTechTitles(); }

        [GeneratedRegex("(\\$Name:\\s*(.*?)\\r\\n.*?\\r\\n)(\\s*\\+Description:)", RegexOptions.Singleline)]
        private static partial Regex _NoTitles();
        public static Regex NoTitles { get => _NoTitles(); }

        [GeneratedRegex("\\$Name:[ \\t]*([^\\r]*)")]
        private static partial Regex _WeaponNames();
        public static Regex WeaponNames { get => _WeaponNames(); }

        [GeneratedRegex("\\$Flags:(.*?)\"[ \t]*\\)")]
        private static partial Regex _Flags();
        public static Regex Flags { get => _Flags(); }

        [GeneratedRegex("#Ship Classes.*?#end", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex _ShipSection();
        public static Regex ShipSection { get => _ShipSection(); }

        [GeneratedRegex("(?<!;)\\$Name:.*?(?=(?<!;)\\$Name|#end)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex _ShipEntries();
        public static Regex ShipEntries { get => _ShipEntries(); }

        [GeneratedRegex("\\$Name:(.*)$", RegexOptions.Multiline)]
        private static partial Regex _ShipNames();
        public static Regex ShipNames { get => _ShipNames(); }

		[GeneratedRegex("(?<!;)\\$Subsystem:.*?(?=\\$Subsystem:|\\r?\\n\\r?\\n)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
		private static partial Regex _Subsystems();
		public static Regex Subsystems { get => _Subsystems(); }

		[GeneratedRegex("(?<=\\$Subsystem:[ \\t]*)(.*?)(?=$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
		private static partial Regex _SubsystemNamesAndCoordinates();
		public static Regex SubsystemNamesAndCoordinates { get => _SubsystemNamesAndCoordinates(); }

		[GeneratedRegex("(?<!;)(\\$Subsystem:[ \\t]*([^\\r\\n]*?),[^\\r\\n]*?\\r?\\n)(.*?)(?=\\$Subsystem:|$)", RegexOptions.Singleline)]
        private static partial Regex _FullSubsystems();
        public static Regex FullSubsystems { get => _FullSubsystems(); }

        [GeneratedRegex("(\\+Tech Description:[ \\t]*)(.*?)\\r\\n")]
        private static partial Regex _FullTechDescriptions();
        public static Regex FullTechDescriptions { get => _FullTechDescriptions(); }

        // the main problem is that there are two different +Length properties, and only one of them should be translated (the one before $thruster property)
        [GeneratedRegex("(\\$Name:(?:(?!\\$Name:|\\$Thruster).)*?\\r\\n)([ \\t]*\\+Length:[ \\t]*)([^\\r]*?)(\\r\\n)", RegexOptions.Singleline)]
        private static partial Regex _ShipLength();
        public static Regex ShipLength { get => _ShipLength(); }

        [GeneratedRegex("(\\$Subsystem:[ \\t]*(.*?),.*?\\n)(.*?)")]
        private static partial Regex _SubsystemNames();
        public static Regex SubsystemNames { get => _SubsystemNames(); }

        [GeneratedRegex("(.*\\$Alt Subsystem Name:[ \\t]*)(.*)\\r?\\n", RegexOptions.IgnoreCase)]
        private static partial Regex _AltSubsystemNamesToModify();
        public static Regex AltSubsystemNamesToModify { get => _AltSubsystemNamesToModify(); }

        [GeneratedRegex("(\\$Alt Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)\\r?\\n)(.*?)", RegexOptions.IgnoreCase)]
        private static partial Regex _InternationalizedAltSubsystemNamesWithFollowingLine();
        public static Regex InternationalizedAltSubsystemNamesWithFollowingLine { get => _InternationalizedAltSubsystemNamesWithFollowingLine(); }

        // take 2 lines after subsystem to skip alt subsystem line
        [GeneratedRegex("(\\$Subsystem:[ \\t]*(.*?),.*?\\n.*?\\n)(.*?)")]
        private static partial Regex _SubsystemsWithAltSubsystems();
        public static Regex SubsystemsWithAltSubsystems { get => _SubsystemsWithAltSubsystems(); }

        [GeneratedRegex("\\$Alt Subsystem Name:[ \\t]*XSTR", RegexOptions.IgnoreCase)]
        private static partial Regex _InternationalizedSubsystemNames();
        public static Regex InternationalizedSubsystemNames { get => _InternationalizedSubsystemNames(); }

        // [ \t] because \s includes \r and \n
        [GeneratedRegex("(.*\\$Alt Damage Popup Subsystem Name:[ \\t]*)(.*)\\r?\\n")]
        private static partial Regex _AltDamagePopupSubsystemNamesToModify();
        public static Regex AltDamagePopupSubsystemNamesToModify { get => _AltDamagePopupSubsystemNamesToModify(); }

        [GeneratedRegex("\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)")]
        private static partial Regex _InternationalizedAltDamagePopupSubsystemNames();
        public static Regex InternationalizedAltDamagePopupSubsystemNames { get => _InternationalizedAltDamagePopupSubsystemNames(); }

        [GeneratedRegex("\\$Alt Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", RegexOptions.IgnoreCase)]
        private static partial Regex _InternationalizedAltSubsystemNames();
        public static Regex InternationalizedAltSubsystemNames { get => _InternationalizedAltSubsystemNames(); }

        [GeneratedRegex("(?<=\\$Default PBanks:[ \t]*\\([ \t]*\")(.*?)(?=\")")]
        private static partial Regex _DefaultPBanks();
        public static Regex DefaultPBanks { get => _DefaultPBanks(); }

		[GeneratedRegex("(?<=\\$Default SBanks:[ \t]*\\([ \t]*\")(.*?)(?=\")")]
		private static partial Regex _DefaultSBanks();
		public static Regex DefaultSBanks { get => _DefaultSBanks(); }

		//[GeneratedRegex("(\\d+), (\".*?\")", RegexOptions.Singleline)]
		[GeneratedRegex("^(\\d+), (\".*?\")(.*?)\\r?\\n", RegexOptions.Singleline | RegexOptions.Multiline)]
        private static partial Regex _XstrInTstrings();
        public static Regex XstrInTstrings { get => _XstrInTstrings(); }

        [GeneratedRegex("[^0-9.-]+")]
        private static partial Regex _OnlyDigits();
        public static Regex OnlyDigits { get => _OnlyDigits(); }

        [GeneratedRegex("(\\(\\s*modify-variable-xstr\\s*\".*?\"\\s*(\".*?\")\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Singleline)]
        private static partial Regex _ModifyVariableXstr();
        public static Regex ModifyVariableXstr { get => _ModifyVariableXstr(); }

        [GeneratedRegex("(\".*?\" )(-?\\d+)")]
        private static partial Regex _StringAndId();
        public static Regex StringAndId { get => _StringAndId(); }

        [GeneratedRegex("(?<=MSGXSTR.+)(\".+?\") (-?\\d+)")]
        private static partial Regex _MsgXstr();
        public static Regex MsgXstr { get => _MsgXstr(); }

        [GeneratedRegex("(SHOWICON.+?text=(\".+?\").+?xstrid=)(-?\\d+)(.*$)", RegexOptions.Multiline)]
        private static partial Regex _ShowIcon();
        public static Regex ShowIcon { get => _ShowIcon(); }

        [GeneratedRegex("(\\(\\s*tech-add-intel-xstr\\s*(\".*?\")\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Singleline)]
        private static partial Regex _TechAddIntelXstr();
        public static Regex TechAddIntelXstr { get => _TechAddIntelXstr(); }

		[GeneratedRegex("(?<!;)(\\$Name:[ \\t]*(.*?)\\r\\n.*\\+Use Ship as Template.*?\\r\\n)(.)", RegexOptions.Singleline)]
		private static partial Regex _UseShipAsTemplate();
		public static Regex UseShipAsTemplate { get => _UseShipAsTemplate(); }

		[GeneratedRegex("(?<!;)(\\$Name:[ \\t]*(.*?)\\r\\n.*?\\+Use Template.*?\\r\\n)(.)", RegexOptions.Singleline)]
		private static partial Regex _UseTemplate();
		public static Regex UseTemplate { get => _UseTemplate(); }

		[GeneratedRegex("(?<!;)(\\$Name:[ \\t]*(.*?)\\r\\n.*?\\+nocreate.*?\\r\\n)(.)", RegexOptions.Singleline)]
		private static partial Regex _Nocreate();
		public static Regex Nocreate { get => _Nocreate(); }

		[GeneratedRegex("(?<!;)(\\$Name:[ \\t]*(.*?)\\r\\n.*?\\+remove.*?\\r\\n)(.)", RegexOptions.Singleline)]
		private static partial Regex _Remove();
		public static Regex Remove { get => _Remove(); }

		[GeneratedRegex("#Primary Weapons.*?#End", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
		private static partial Regex _Primaries();
		public static Regex Primaries { get => _Primaries(); }

		[GeneratedRegex("#Secondary Weapons.*?#End", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
		private static partial Regex _Secondaries();
		public static Regex Secondaries { get => _Secondaries(); }

		// Only used for FSO < v23.0
		public static Regex GetJumpNodeReferences(string jumpNode)
        {
            return new($"(?<=\\([ \t]*(depart-node-delay|show-jumpnode|hide-jumpnode|set-jumpnode-color|set-jumpnode-name|set-jumpnode-model)[^\\(]*)\"{jumpNode}\"", RegexOptions.Singleline);
        }
    }
}
