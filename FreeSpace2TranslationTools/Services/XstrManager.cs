using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class XstrManager
    {
        public MainWindow Parent { get; set; }
        public object Sender { get; set; }
        public string ModFolder { get; set; }
        public string DestinationFolder { get; set; }
        public List<string> FilesList { get; set; }
        public int CurrentProgress { get; set; }

        public XstrManager(MainWindow parent, object sender, string modFolder, string destinationFolder)
        {
            Parent = parent;
            Sender = sender;
            ModFolder = modFolder;
            DestinationFolder = destinationFolder;
            CurrentProgress = 0;

            FilesList = Utils.GetFilesWithXstrFromFolder(modFolder);

            Parent.InitializeProgress(Sender);
            Parent.SetMaxProgress(FilesList.Count);
        }

        #region public methods
        /// <summary>
        /// Replace all hardcoded credit lines with XSTR
        /// </summary>
        /// <param name="filesList"></param>
        /// <param name="modFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        public void ProcessCreditFiles()
        {
            List<string> creditFiles = FilesList.Where(x => x.Contains("-crd.tbm") || x.Contains("credits.tbl")).ToList();

            foreach (string file in creditFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = Regex.Replace(sourceContent, @"^((?!XSTR).+?\r\n)", new MatchEvaluator(GenerateCredits), RegexOptions.Multiline);

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        /// <summary>
        /// Adds alt names with XSTR variables to medals
        /// </summary>
        /// <param name="filesList"></param>
        /// <param name="modFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        public void ProcessMedalsFile()
        {
            string medalsFile = FilesList.FirstOrDefault(x => x.Contains("medals.tbl"));

            if (medalsFile != null)
            {
                string sourceContent = File.ReadAllText(medalsFile);

                string newContent = Regex.Replace(sourceContent, @"(\$Name:\s*(.*?)\r\n)(\$Bitmap)", new MatchEvaluator(GenerateAltNames), RegexOptions.Multiline);

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(medalsFile, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        /// <summary>
        /// Adds XSTR variables to door descriptions of main halls
        /// </summary>
        /// <param name="filesList"></param>
        /// <param name="modFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        public void ProcessMainHallFiles()
        {
            List<string> mainHallFiles = FilesList.Where(x => x.Contains("-hall.tbm") || x.Contains("mainhall.tbl")).ToList();

            // all door descriptions without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
            Regex regexDoorDescription = new(@"\+Door description:\s*(((?!XSTR).)*)\r\n", RegexOptions.Multiline | RegexOptions.Compiled);

            foreach (string file in mainHallFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = regexDoorDescription.Replace(sourceContent, new MatchEvaluator(GenerateDoorDescriptions));

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        public void ProcessShipFiles()
        {
            List<string> shipFiles = FilesList.Where(x => x.Contains("-shp.tbm") || x.Contains("ships.tbl")).ToList();

            foreach (string file in shipFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string shipsEntries = Regex.Match(sourceContent, @"#Ship Classes.*?(#End|#end)", RegexOptions.Singleline).Value;

                string newContent = Regex.Replace(shipsEntries, @"(\$Subsystem:\s+(.*?),.*?\r\n)(.*?)(?=\$Name|\$Subsystem:|#End)", new MatchEvaluator(GenerateSubsystems), RegexOptions.Singleline);

                newContent = Utils.RegexNoAltNames.Replace(newContent, new MatchEvaluator(GenerateAltNames));

                newContent = sourceContent.Replace(shipsEntries, newContent);

                Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        public void ProcessWeaponFiles()
        {
            List<string> weaponFiles = FilesList.Where(x => x.Contains("-wep.tbm") || x.Contains("weapons.tbl")).ToList();
            List<string> primaryNames = new();
            List<string> secondaryNames = new();

            foreach (string file in weaponFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = Utils.RegexNoAltNames.Replace(sourceContent, new MatchEvaluator(GenerateAltNames));
                MatchCollection weapons = Regex.Matches(newContent, @"\$Name:\s*.*?(?=\$Name|#end|#End)", RegexOptions.Singleline);

                foreach (Match weapon in weapons)
                {
                    if (!weapon.Value.Contains("+Tech Title:") && weapon.Value.Contains("+Tech Description:"))
                    {
                        string newEntry = Regex.Replace(weapon.Value, @"(\$Name:\s*(.*?)\r\n.*?\r\n)(\s*\+Tech Anim:|\s*\+Tech Description:)", new MatchEvaluator(GenerateTechTitle), RegexOptions.Singleline);

                        newContent = newContent.Replace(weapon.Value, newEntry);
                    }
                }

                Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        public void ProcessMissionFiles()
        {
            List<string> missionFiles = FilesList.Where(x => x.Contains(".fs2")).ToList();

            // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
            // ex: $Label: Alpha 1 ==> $Label: XSTR ("Alpha 1", -1)
            Regex regexLabels = new(@"\$label:\s*(((?!XSTR).)*)\r", RegexOptions.Multiline | RegexOptions.Compiled);

            // ex: $Name: Psamtik   ==>     $Name: Psamtik
            //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
            //                      ==>     $Class......
            Regex regexShipNames = new(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline | RegexOptions.Compiled);

            foreach (string file in missionFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = regexLabels.Replace(sourceContent, new MatchEvaluator(GenerateLabels));

                newContent = regexShipNames.Replace(newContent, new MatchEvaluator(GenerateShipNames));

                newContent = ConvertShowSubtitleToShowSubtitleText(newContent);

                newContent = ExtractShowSubtitleTextContentToMessages(newContent);

                newContent = ConvertAltToVariables(newContent);

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        } 
        #endregion

        private string GenerateSubsystems(Match match)
        {
            string newSubsystem = match.Value;
            bool altNameAlreadyExisting = true;
            bool altDamagePopupNameAlreadyExisting = true;

            if (!match.Value.Contains("$Alt Subsystem Name:"))
            {
                altNameAlreadyExisting = false;
                newSubsystem = Regex.Replace(newSubsystem, @"(\$Subsystem:\s*(.*?),.*?\r\n)(.*?)", new MatchEvaluator(AddAltSubsystemName));
            }
            else if (!Regex.IsMatch(match.Value, @"\$Alt Subsystem Name:\s*XSTR"))
            {
                newSubsystem = Regex.Replace(newSubsystem, @"\$Alt Subsystem Name:\s*(.*?)\r\n", new MatchEvaluator(ReplaceAltSubsystemName));
            }

            if (!match.Value.Contains("$Alt Damage Popup Subsystem Name:"))
            {
                altDamagePopupNameAlreadyExisting = false;

                // if existing, copy the alt name to damage popup name
                if (altNameAlreadyExisting)
                {
                    newSubsystem = Regex.Replace(newSubsystem, "(\\$Alt Subsystem Name: XSTR\\(\"(.*?)\", -1\\)\\r\\n)(.*?)", new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
                else
                {
                    // take 2 lines after subsystem to skip alt subsystem line
                    newSubsystem = Regex.Replace(newSubsystem, @"(\$Subsystem:\s*(.*?),.*?\r\n.*?\r\n)(.*?)", new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
            }
            else if (!Regex.IsMatch(match.Value, @"\$Alt Subsystem Name:\s*XSTR"))
            {
                newSubsystem = Regex.Replace(newSubsystem, @"\$Alt Damage Popup Subsystem Name:(.*?)\r\n", new MatchEvaluator(ReplaceAltDamagePopupSubsystemName));
            }

            // if alt damage popup name already existing but not alt name, then copy it to alt name 
            if (!altNameAlreadyExisting && altDamagePopupNameAlreadyExisting)
            {
                string newName = Regex.Match(newSubsystem, "\\$Alt Damage Popup Subsystem Name: XSTR\\(\"(.*?)\", -1\\)").Groups[1].Value;
                newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name: XSTR\\(\"(.*?)\", -1\\)", $"$Alt Subsystem Name: XSTR(\"{newName}\", -1)");
            }

            return newSubsystem;
        }

        private string AddAltSubsystemName(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Subsystem Name", match);
        }

        private string ReplaceAltSubsystemName(Match match)
        {
            return ReplaceHardcodedValueWithXstr("$Alt Subsystem Name: ", match);
        }

        private string AddAltDamagePopupSubsystemName(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Damage Popup Subsystem Name", match);
        }

        private string ReplaceAltDamagePopupSubsystemName(Match match)
        {
            return ReplaceHardcodedValueWithXstr("$Alt Damage Popup Subsystem Name: ", match);
        }

        private string GenerateCredits(Match match)
        {
            return ReplaceHardcodedValueWithXstr("", match);
        }

        private string GenerateAltNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Name", match);
        }

        private string GenerateDoorDescriptions(Match match)
        {
            return ReplaceHardcodedValueWithXstr("+Door description: ", match);
        }

        private string GenerateTechTitle(Match match)
        {
            return AddXstrLineToHardcodedValue("\t+Tech Title", match);
        }

        private string GenerateLabels(Match match)
        {
            return ReplaceHardcodedValueWithXstr("$label: ", match);
        }

        private string GenerateShipNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Display Name", match);
        }

        /// <summary>
        /// Replaces an hardcoded line with an XSTR variable
        /// </summary>
        /// <param name="marker">Marker identifying the line</param>
        /// <param name="match">Groups[1] must be the XSTR value (comments will be removed)</param>
        /// <returns></returns>
        private string ReplaceHardcodedValueWithXstr(string marker, Match match)
        {
            string[] values = match.Groups[1].Value.Trim().Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
            string value = values.Length == 0 ? "" : values[0].Replace("\"", "$quote");
            string result = $"{marker}XSTR(\"{value}\", -1){Environment.NewLine}";

            if (values.Length > 1)
            {
                result += $" ;{values[1]}";
            }

            return result;
        }

        /// <summary>
        /// Adds a new line including an XSTR variable
        /// </summary>
        /// <param name="newMarker">Name of the new marker identifying the XSTR variable</param>
        /// <param name="match">Groups[1]: first original line (including \r\n), Groups[2]: hardcoded value to be translated, Groups[3]: line after the hardcoded value</param>
        /// <returns></returns>
        private string AddXstrLineToHardcodedValue(string newMarker, Match match)
        {
            string valueWithoutComment = match.Groups[2].Value.Split(';', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            string valueWithoutAlias = valueWithoutComment.Split('#', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            return $"{match.Groups[1].Value}{newMarker}: XSTR(\"{valueWithoutAlias.Trim().TrimStart('@')}\", -1){Environment.NewLine}{match.Groups[3].Value}";
        }

        /// <summary>
        /// Convert sexp show-subtitle to show-subtitle-text so they can be translated
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string ConvertShowSubtitleToShowSubtitleText(string content)
        {
            MatchCollection subtitleResults = Regex.Matches(content, @"show-subtitle\s+.*?\)", RegexOptions.Singleline);

            foreach (Match match in subtitleResults)
            {
                MatchCollection parameters = Regex.Matches(match.Value, @"(\s*)(.*?)(\s*\r\n)", RegexOptions.Multiline);

                // Try not to accidentally convert image subtitle to text...
                if (!string.IsNullOrWhiteSpace(parameters[3].Groups[2].Value.Trim('"')))
                {
                    Sexp sexp = new Sexp("show-subtitle-text", parameters[1].Groups[1].Value, parameters[1].Groups[3].Value);

                    // text to display
                    sexp.AddParameter(parameters[3].Groups[2].Value);
                    // X position, from 0 to 100%
                    sexp.AddParameter(ConvertXPositionFromAbsoluteToRelative(parameters[1].Groups[2].Value));
                    // Y position, from 0 to 100%
                    sexp.AddParameter(ConvertYPositionFromAbsoluteToRelative(parameters[2].Groups[2].Value));
                    // Center horizontally?
                    if (parameters.Count < 8)
                    {
                        sexp.AddParameter("( false )");
                    }
                    else
                    {
                        sexp.AddParameter(parameters[7].Groups[2].Value);
                    }
                    // Center vertically?
                    if (parameters.Count < 9)
                    {
                        sexp.AddParameter("( false )");
                    }
                    else
                    {
                        sexp.AddParameter(parameters[8].Groups[2].Value);
                    }
                    // Time (in milliseconds) to be displayed
                    sexp.AddParameter(parameters[4].Groups[2].Value);
                    // Fade time (in milliseconds) (optional)
                    if (parameters.Count > 6)
                    {
                        sexp.AddParameter(parameters[6].Groups[2].Value);
                    }
                    // Paragraph width, from 1 to 100% (optional; 0 uses default 200 pixels)
                    if (parameters.Count > 9)
                    {
                        sexp.AddParameter(parameters[9].Groups[2].Value);
                    }
                    // Text red component (0-255) (optional)
                    if (parameters.Count > 10)
                    {
                        sexp.AddParameter(parameters[10].Groups[2].Value);
                    }
                    // Text green component(0 - 255) (optional)
                    if (parameters.Count > 11)
                    {
                        sexp.AddParameter(parameters[11].Groups[2].Value);
                    }
                    // Text blue component(0 - 255) (optional)
                    if (parameters.Count > 12)
                    {
                        sexp.AddParameter(parameters[12].Groups[2].Value);
                    }

                    sexp.CloseFormula();

                    content = content.Replace(match.Value, sexp.Formula);
                }
            }

            return content;
        }

        private string ConvertXPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 900;
            double output = 88;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString();
        }

        private string ConvertYPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 500;
            double output = 65;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString();
        }

        /// <summary>
        /// Extract hardcoded strings from show-subtitle-text and put them into new messages so they can be translated
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string ExtractShowSubtitleTextContentToMessages(string content)
        {
            // ex: ( show-subtitle-text     ==>     ( show-subtitle-text
            //        "Europa, 2386"        ==>        "AutoGeneratedMessage1"
            //                              ==>----------------------------------
            //                              ==>     #Messages
            //                              ==>     
            //                              ==>     $Name: AutoGeneratedMessage1
            //                              ==>     $Team: -1
            //                              ==>     $MessageNew: XSTR("Europa, 2386", -1)
            //                              ==>     $end_multi_text
            //                              ==>     
            //                              ==>     #Reinforcements
            #region get all existing messages in the mission
            string messagesSection = Regex.Match(content, @"#Messages.*#Reinforcements", RegexOptions.Singleline).Value;
            MatchCollection messages = Regex.Matches(messagesSection, @"\$Name:\s*(.*?)(?=;|\r)", RegexOptions.Multiline);
            List<string> allMessages = new();

            string autoGeneratedMessage = "AutoGeneratedMessage";
            int subtitleMessagesCount = 0;

            foreach (Match match in messages)
            {
                allMessages.Add(match.Groups[1].Value);

                // Check for existing AutoGeneratedMessage to increment the count in order to avoid duplications
                if (match.Groups[1].Value.Contains(autoGeneratedMessage))
                {
                    int iteration = int.Parse(match.Groups[1].Value.Substring(autoGeneratedMessage.Length));

                    if (iteration >= subtitleMessagesCount)
                    {
                        subtitleMessagesCount = iteration++;
                    }
                }
            }
            #endregion

            MatchCollection subtitleTextResults = Regex.Matches(content, "(show-subtitle-text\\s*\r\n\\s*\")(.*?)\"", RegexOptions.Multiline);
            string newMessages = string.Empty;

            foreach (Match match in subtitleTextResults)
            {
                if (!allMessages.Contains(match.Groups[2].Value))
                {
                    subtitleMessagesCount++;

                    content = content.Replace(match.Value, match.Groups[1].Value + autoGeneratedMessage + subtitleMessagesCount + "\"");

                    newMessages += $"$Name: {autoGeneratedMessage}{subtitleMessagesCount}{Environment.NewLine}" +
                        $"$Team: -1{Environment.NewLine}" +
                        $"$MessageNew:  XSTR(\"{match.Groups[2].Value}\", -1){Environment.NewLine}" +
                        $"$end_multi_text{Environment.NewLine}{Environment.NewLine}";

                    allMessages.Add(match.Groups[2].Value);
                }
            }

            if (newMessages != string.Empty)
            {
                content = content.Replace("#Reinforcements", newMessages + "#Reinforcements");
            }

            return content;
        }

        /// <summary>
        /// Convert ship alt names to sexp variables so that they can be translated via modify-variable-xstr sexp
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string ConvertAltToVariables(string content)
        {
            if (Utils.RegexAlternateTypes.IsMatch(content))
            {
                #region alt from '#Objects' section
                string objects = Regex.Match(content, @"#Objects.*#Wings", RegexOptions.Singleline).Value;
                // ((?!\$Name).)* => all characters not containing \$Name
                MatchCollection altShips = Regex.Matches(objects, @"\$Name:\s*(((?!\$Name).)*?)\s*(\r\n|;)((?!\$Name).)*?\$Alt:\s*(.*?)\s*\r\n", RegexOptions.Singleline);
                #endregion

                // Check at least one alt name is used before starting modifications
                if (altShips.Count > 0)
                {
                    #region alt from '#Alternate Types' section
                    string alternateTypes = Utils.RegexAlternateTypes.Match(content).Value;
                    // for unknown reason in this case \r case is captured, so we have to uncapture it
                    // in some cases and Alt can have an empty value... 
                    MatchCollection altTypes = Regex.Matches(alternateTypes, @"\$Alt:\s*(((?!\$Alt).)+)(?=\r)");
                    #endregion

                    List<Alt> altList = new();

                    foreach (Match match in altTypes)
                    {
                        Alt alt = new Alt(match.Groups[1].Value);

                        foreach (Match altShip in altShips)
                        {
                            if (altShip.Groups[5].Value == alt.DefaultValue)
                            {
                                alt.AddShip(altShip.Groups[1].Value);
                            }
                        }

                        // some alt are not used for unknown reasons, so we dont keep them
                        if (alt.Ships.Count > 0)
                        {
                            altList.Add(alt);
                        }
                    }

                    // Remove the 'Alternate Types' section
                    content = content.Replace(alternateTypes, string.Empty);
                    // Remove all '$Alt' from '#Objects' section
                    content = Regex.Replace(content, @"\$Alt:\s*.*?\r\n", string.Empty, RegexOptions.Singleline);

                    content = AddVariablesToSexpVariablesSection(content, altList);

                    content = AddEventToManageAltNames(content, altList);
                }
            }

            return content;
        }

        private string AddVariablesToSexpVariablesSection(string content, List<Alt> altList)
        {
            // Create '#Sexp_variables' section if not exists
            if (!content.Contains("#Sexp_variables"))
            {
                string beforeSexp = Regex.Match(content, @"(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline).Value;

                string newSection = $"#Sexp_variables{Environment.NewLine}{Environment.NewLine}$Variables:{Environment.NewLine}({Environment.NewLine}){Environment.NewLine}{Environment.NewLine}{beforeSexp}";

                content = content.Replace(beforeSexp, newSection);
            }

            string sexpVariablesSection = Regex.Match(content, @"#Sexp_variables.*?(#Fiction Viewer|#Command Briefing)", RegexOptions.Singleline).Value;
            MatchCollection variableIds = Regex.Matches(sexpVariablesSection, @"\t\t(\d+)\t\t", RegexOptions.Multiline);

            int variableId = 0;

            // set the next variable id
            foreach (Match match in variableIds)
            {
                int currentId = int.Parse(match.Groups[1].Value);

                if (currentId >= variableId)
                {
                    variableId++;
                }
            }

            string newSexpVariablesSection = string.Empty;

            // here we add a new variable for each alt
            foreach (Alt alt in altList)
            {
                alt.VariableName = "autoGenVar" + variableId;
                newSexpVariablesSection += $"\t\t{variableId}\t\t\"{alt.VariableName}\"\t\t\"{alt.DefaultValue}\"\t\t\"string\"{Environment.NewLine}";
                variableId++;
            }

            string endOfVariables = Regex.Match(content, @"\)\r\n\r\n(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline).Value;

            return content.Replace(endOfVariables, newSexpVariablesSection + endOfVariables);
        }

        private string AddEventToManageAltNames(string content, List<Alt> altList)
        {
            // very unorthodox way to add the event but it allows me to manage the case when this event already exists in the original file
            string eventForAltNamesTitle = "Auto generated event: alt names";
            string eventEnd = $"){Environment.NewLine}"
                + $"+Name: {eventForAltNamesTitle}{Environment.NewLine}"
                + $"+Repeat Count: 1{Environment.NewLine}"
                + $"+Interval: 1{Environment.NewLine}{Environment.NewLine}";

            if (!content.Contains(eventForAltNamesTitle))
            {
                string events = Regex.Match(content, @"#Events.*?\r\n\r\n", RegexOptions.Singleline).Value;

                string eventBeginning = $"$Formula: ( when {Environment.NewLine}"
                    + $"   ( true ) {Environment.NewLine}";

                content = content.Replace(events, events + eventBeginning + eventEnd);
            }

            string newSexp = string.Empty;

            foreach (Alt alt in altList)
            {
                newSexp += alt.ModifyVariableXstr() + alt.ShipChangeAltName();
            }

            return content.Replace(eventEnd, newSexp + eventEnd);
        }
    }
}
