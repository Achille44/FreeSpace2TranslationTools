using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    internal class Mission : IFile
    {
        internal string Content { get; set; }

        internal List<string> JumpNodes { get; set; } = new List<string>();

        // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
        // ex: $Label: Alpha 1 ==> $Label: XSTR("Alpha 1", -1)
        internal static Regex RegexLabels = new(@"(.*\$label:\s*)((?!XSTR).*)\r\n", RegexOptions.Compiled);

        internal static Regex RegexCallSigns = new(@"(.*\$Callsign:[ \t]*)(.*?)\r\n", RegexOptions.Compiled);

        // ex: $Name: Psamtik   ==>     $Name: Psamtik
        //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
        //                      ==>     $Class......
        internal static Regex RegexShipNames = new(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline | RegexOptions.Compiled);

        public Mission(string content)
        {
            Content = content;
        }

        public string GetInternationalizedContent()
        {
            Content = RegexLabels.Replace(Content, new MatchEvaluator(GenerateLabels));

            Content = RegexCallSigns.Replace(Content, new MatchEvaluator(GenerateCallSigns));

            Content = RegexShipNames.Replace(Content, new MatchEvaluator(GenerateShipNames));

            Content = ConvertShowSubtitleToShowSubtitleText(Content);

            Content = ExtractShowSubtitleTextContentToMessages(Content);

            Content = ConvertAltToVariables(Content);

            Content = ConvertHardcodedHudTextToVariables(Content);

            Content = ConvertSpecialMessageSendersToVariables(Content);

            Content = Regex.Replace(Content, @"(.*\$Jump Node Name:[ \t]*)(.*?)\r\n", new MatchEvaluator(GenerateJumpNodeNames));
            Content = ConvertJumpNodeReferencesToVariables(Content);

            Content = ConvertNavpointsToVariables(Content);

            Content = ConvertNavColorToVariables(Content);

            Content = ConvertHardcodedSubsystemNamesToVariables(Content);

            Content = ConvertHardcodedObjectRolesToVariables(Content);

            Content = ConvertHardcodedMarkedShipsAndWingsToVariables(Content);

            Content = ConvertHardcodedMarkedSubsystemsToVariables(Content);

            return Content;
        }

        internal static string ReplaceHardcodedValueWithXstr(string originalMatch, string beginningOfLine, string value)
        {
            // if this is a comment or if it's already XSTR, then don't touch it and return the original match
            if (beginningOfLine.Contains(';') || value.Contains("XSTR"))
            {
                return originalMatch;
            }
            else
            {
                string[] values = value.Trim().Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
                string sanatizedValue = values.Length == 0 ? "" : values[0].Replace("\"", "$quote");

                // in case no value, keep original
                if (sanatizedValue == "")
                {
                    return originalMatch;
                }

                string result = $"{beginningOfLine}XSTR(\"{sanatizedValue}\", -1)";

                if (values.Length > 1)
                {
                    result += $" ;{values[1]}";
                }

                if (originalMatch.EndsWith("\r\n"))
                {
                    result += "\r\n";
                }
                else if (originalMatch.EndsWith("\n"))
                {
                    result += "\n";
                }

                return result;
            }
        }

        /// <summary>
        /// Adds a new line including an XSTR variable
        /// </summary>
        /// <param name="newMarker">Name of the new marker identifying the XSTR variable</param>
        /// <param name="match">Groups[1]: first original line (including \r\n), Groups[2]: hardcoded value to be translated, Groups[3]: line after the hardcoded value</param>
        /// <returns></returns>
        private static string AddXstrLineToHardcodedValue(string newMarker, Match match)
        {
            // if marker already present, then don't touch anything
            if (match.Value.Contains(newMarker))
            {
                return match.Value;
            }
            else
            {
                string valueWithoutComment = match.Groups[2].Value.Split(';', 2, StringSplitOptions.RemoveEmptyEntries)[0];
                string valueWithoutAlias = valueWithoutComment.Split('#', 2, StringSplitOptions.RemoveEmptyEntries)[0];
                return $"{match.Groups[1].Value}{newMarker}: XSTR(\"{valueWithoutAlias.Trim().TrimStart('@')}\", -1){Environment.NewLine}{match.Groups[3].Value}";
            }
        }

        private static string ConvertXPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 900;
            double output = 88;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString(CultureInfo.InvariantCulture);
        }

        private static string ConvertYPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 500;
            double output = 65;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString(CultureInfo.InvariantCulture);
        }

        private string GenerateLabels(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateCallSigns(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateShipNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Display Name", match);
        }

        private string GenerateJumpNodeNames(Match match)
        {
            string newName = ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);

            // if the jump node has already been translated, then don't add it to the list
            if (match.Value != newName)
            {
                JumpNodes.Add(SanitizeName(match.Groups[2].Value, true));
            }

            return newName;
        }

        /// <summary>
        /// Convert sexp show-subtitle to show-subtitle-text so they can be translated
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string ConvertShowSubtitleToShowSubtitleText(string content)
        {
            MatchCollection subtitleResults = Regex.Matches(content, @"show-subtitle\s+.*?\r\n[ \t]+\)", RegexOptions.Singleline);

            foreach (Match match in subtitleResults)
            {
                MatchCollection parameters = Regex.Matches(match.Value, @"(\s*)(.*?)(\s*\r\n)", RegexOptions.Multiline);

                // Try not to accidentally convert image subtitle to text...
                if (!string.IsNullOrWhiteSpace(parameters[3].Groups[2].Value.Trim('"')))
                {
                    Sexp sexp = new("show-subtitle-text", parameters[1].Groups[1].Value, parameters[1].Groups[3].Value);

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
                    // if no original width, just add one with max value to avoid splitting text into several lines
                    else
                    {
                        sexp.AddParameter("100");
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

            foreach (Match match in subtitleTextResults.AsEnumerable())
            {
                // skip already existing messages and cases with variables
                if (!allMessages.Contains(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@"))
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
        private static string ConvertAltToVariables(string content)
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

                    List<MissionVariable> variableList = new();
                    List<Alt> altList = new();

                    foreach (Match match in altTypes)
                    {
                        Alt alt = new(match.Groups[1].Value);

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
                            variableList.Add(alt);
                            altList.Add(alt);
                        }
                    }

                    if (variableList.Count > 0)
                    {
                        // Remove the 'Alternate Types' section
                        content = content.Replace(alternateTypes, string.Empty);
                        // Remove all '$Alt' from '#Objects' section
                        content = Regex.Replace(content, @"\$Alt:\s*.*?\r\n", string.Empty, RegexOptions.Singleline);

                        content = AddVariablesToSexpVariablesSection(content, variableList);

                        string newSexp = PrepareNewSexpForAltNames(altList);

                        content = AddEventToManageTranslations(content, newSexp);
                    }
                }
            }

            return content;
        }

        private static string AddVariablesToSexpVariablesSection(string content, List<MissionVariable> variableList)
        {
            // Create '#Sexp_variables' section if not exists
            if (!content.Contains("#Sexp_variables"))
            {
                string beforeSexp = Regex.Match(content, @"(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline).Value;

                string newSection = $"#Sexp_variables{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"$Variables:{Environment.NewLine}" +
                    $"({Environment.NewLine}" +
                    $"){Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"{beforeSexp}";

                content = content.Replace(beforeSexp, newSection);
            }

            string autoGeneratedVariableName = "autoGenVar";
            string sexpVariablesSection = Regex.Match(content, @"#Sexp_variables.*?(#Fiction Viewer|#Command Briefing)", RegexOptions.Singleline).Value;
            MatchCollection variableNameIds = Regex.Matches(sexpVariablesSection, $"{autoGeneratedVariableName}(\\d+)", RegexOptions.Multiline);
            int variableId = Regex.Matches(sexpVariablesSection, @"^\t\t\d", RegexOptions.Multiline).Count;

            int variableNameId = 1;

            // set the next variable id
            foreach (Match match in variableNameIds)
            {
                int currentId = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

                if (currentId >= variableNameId)
                {
                    variableNameId++;
                }
            }

            string newSexpVariablesSection = string.Empty;

            // here we add a new variable for each alt
            foreach (MissionVariable variable in variableList)
            {
                variable.VariableName = autoGeneratedVariableName + variableNameId;
                newSexpVariablesSection += $"\t\t{variableId}\t\t\"{variable.VariableName}\"\t\t\"{variable.DefaultValue}\"\t\t\"string\"{Environment.NewLine}";
                variableNameId++;
                variableId++;
            }

            string endOfVariables = Regex.Match(content, @"\)\r\n\r\n(#Fiction Viewer|#Command Briefing|#Cutscenes)", RegexOptions.Multiline).Value;

            return content.Replace(endOfVariables, newSexpVariablesSection + endOfVariables);
        }

        private static string PrepareNewSexpForAltNames(List<Alt> altList)
        {
            string newSexp = string.Empty;

            foreach (Alt alt in altList)
            {
                newSexp += alt.ModifyVariableXstr() + alt.ShipChangeAltName();
            }

            return newSexp;
        }

        private static string PrepareNewSexpForVariables(List<MissionVariable> VariableList)
        {
            string newSexp = string.Empty;

            foreach (MissionVariable variable in VariableList)
            {
                newSexp += variable.ModifyVariableXstr();
            }

            return newSexp;
        }

        private static string AddEventToManageTranslations(string content, string newSexp)
        {
            // very unorthodox way to add the event but it allows me to manage the case when this event already exists in the original file
            string eventForAltNamesTitle = "Manage translation variables";
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

            return content.Replace(eventEnd, newSexp + eventEnd);
        }

        /// <summary>
        /// Convert hardcoded texts in hud-set-text sexp to variables to be treated by XSTR
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string ConvertHardcodedHudTextToVariables(string content)
        {
            MatchCollection textMatches = Regex.Matches(content, "(\\( (?:hud-set-text|hud-set-directive).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline);
            List<MissionVariable> variableList = new();
            List<HudText> hudTextList = new();

            foreach (Match match in textMatches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    HudText hudText = new(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                    variableList.Add(hudText);
                    hudTextList.Add(hudText);
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);

                string newSexp = PrepareNewSexpForVariables(variableList);

                content = AddEventToManageTranslations(content, newSexp);

                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in textMatches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        HudText hudText = hudTextList.FirstOrDefault(h => h.DefaultValue == match.Groups[2].Value);
                        content = content.Replace(match.Value, match.Groups[1] + hudText.NewSexp + match.Groups[3]);
                    }
                }
            }

            return content;
        }

        private static string ConvertSpecialMessageSendersToVariables(string content)
        {
            MatchCollection messageMatches = Regex.Matches(content, @"\( (send-random-message|send-message).*?\r?\n[ \t]*\)\r?\n", RegexOptions.Singleline);
            List<MissionVariable> variableList = new();

            foreach (Match message in messageMatches)
            {
                MatchCollection specialSenderMatches = Regex.Matches(message.Value, "\"(#.*?)\"");

                // in case of send-message-list, there can be several special senders
                foreach (Match specialSender in specialSenderMatches)
                {
                    if (!variableList.Any(v => v.DefaultValue == specialSender.Groups[1].Value))
                    {
                        MissionVariable variable = new(specialSender.Groups[1].Value);
                        variableList.Add(variable);
                    }
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                foreach (Match message in messageMatches)
                {
                    MatchCollection specialSenderMatches = Regex.Matches(message.Value, "\"(#.*?)\"");

                    string currentMessage = message.Value;

                    // in case of send-message-list, there can be several special senders
                    foreach (Match specialSender in specialSenderMatches)
                    {
                        if (variableList.Any(v => v.DefaultValue == specialSender.Groups[1].Value))
                        {
                            MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == specialSender.Groups[1].Value);

                            string newMessage = currentMessage.Replace(specialSender.Value, variable.NewSexp);
                            content = content.Replace(currentMessage, newMessage);
                            // once the currentMessage has been replaced in the original content, the newMessage becomes the currentMessage
                            currentMessage = newMessage;
                        }
                    }
                }
            }

            return content;
        }

        /// <summary>
        /// Removes comments, alias and spaces from a name
        /// </summary>
        private static string SanitizeName(string rawName, bool fullSanatizing = false)
        {
            if (fullSanatizing)
            {
                return rawName.Split(';')[0].Split('#')[0].Trim().TrimStart('@');
            }
            else
            {
                return rawName.Split(';')[0].Trim();
            }
        }

        private string ConvertJumpNodeReferencesToVariables(string content)
        {
            if (JumpNodes.Count > 0)
            {
                List<MissionVariable> variableList = new();
                // don't take variables section
                string fromObjectToWaypoints = "#Objects.*#Waypoints";
                string originalContent = Regex.Match(content, fromObjectToWaypoints, RegexOptions.Singleline).Value;
                string jumpNodesSexp = "depart-node-delay|show-jumpnode|hide-jumpnode|set-jumpnode-color|set-jumpnode-name|set-jumpnode-model";

                foreach (string jumpNode in JumpNodes)
                {
                    // find all references outside XSTR
                    //MatchCollection jumpNodeReferences = Regex.Matches(content, $"(?<=\\( (depart-node-delay.*?\\d+|show-jumpnode|hide-jumpnode) \r\n[ \t]*)\"{jumpNode}\"", RegexOptions.Singleline);
                    MatchCollection jumpNodeReferences = Regex.Matches(originalContent, $"(?<=\\([ \t]*({jumpNodesSexp})[^\\(]*)\"{jumpNode}\"", RegexOptions.Singleline);

                    if (jumpNodeReferences.Count > 0)
                    {
                        variableList.Add(new(jumpNode));
                    }
                }

                if (variableList.Count > 0)
                {
                    content = AddVariablesToSexpVariablesSection(content, variableList);
                    string newSexp = PrepareNewSexpForVariables(variableList);
                    content = AddEventToManageTranslations(content, newSexp);
                    originalContent = Regex.Match(content, fromObjectToWaypoints, RegexOptions.Singleline).Value;
                    string newContent = originalContent;

                    foreach (MissionVariable variable in variableList)
                    {
                        // (?<=...) => look behind
                        //content = Regex.Replace(content, $"(?<=\\( (depart-node-delay.*?\\d+|show-jumpnode|hide-jumpnode) \r\n[ \t]*)\"{variable.DefaultValue}\"", variable.NewSexp, RegexOptions.Singleline);
                        newContent = Regex.Replace(newContent, $"(?<=\\([ \t]*({jumpNodesSexp})[^\\(]*)\"{variable.DefaultValue}\"", variable.NewSexp, RegexOptions.Singleline);
                    }

                    content = content.Replace(originalContent, newContent);
                }

                return content;

            }
            else
            {
                return content;
            }
        }

        private static string ConvertNavpointsToVariables(string content)
        {
            List<MissionVariable> variableList = new();

            MatchCollection navSexpMatches = Regex.Matches(content, "(add-nav-waypoint|addnav-ship|del-nav|hide-nav|restrict-nav|unhide-nav|unrestrict-nav|set-nav-visited|unset-nav-visited|select-nav|unselect-nav|is-nav-visited).*?\"(.*?)\"", RegexOptions.Singleline);

            foreach (Match navSexp in navSexpMatches)
            {
                if (!variableList.Any(v => v.DefaultValue == navSexp.Groups[2].Value) && !navSexp.Groups[2].Value.StartsWith('@'))
                {
                    variableList.Add(new MissionVariable(navSexp.Groups[2].Value));
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                foreach (Match navSexp in navSexpMatches)
                {
                    MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == navSexp.Groups[2].Value);

                    string newNavSexp = navSexp.Value.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                    content = content.Replace(navSexp.Value, newNavSexp);
                }
            }

            return content;
        }

        private static string ConvertNavColorToVariables(string content)
        {
            List<MissionVariable> variableList = new();

            MatchCollection navSexpMatches = Regex.Matches(content, "\\( set-nav-color.*?\\)", RegexOptions.Singleline);

            foreach (Match navSexp in navSexpMatches)
            {
                MatchCollection navNameMatches = Regex.Matches(navSexp.Value, "\"(.*?)\"");

                foreach (Match navName in navNameMatches)
                {
                    if (!variableList.Any(v => v.DefaultValue == navName.Groups[1].Value) && !navName.Groups[1].Value.StartsWith('@'))
                    {
                        variableList.Add(new MissionVariable(navName.Groups[1].Value));
                    }
                }
            }

            if (variableList.Count > 0)
            {
                // TODO: check that the same variable doesn't already exit, to avoid duplicates
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                foreach (Match navSexp in navSexpMatches)
                {
                    string newNavSexp = navSexp.Value;

                    foreach (MissionVariable variable in variableList)
                    {
                        newNavSexp = newNavSexp.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                    }

                    content = content.Replace(navSexp.Value, newNavSexp);
                }
            }

            return content;
        }

        private static string ConvertHardcodedSubsystemNamesToVariables(string content)
        {
            MatchCollection subsystemMatches = Regex.Matches(content, "(\\( (?:change-subsystem-name).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline);
            List<MissionVariable> variableList = new();

            foreach (Match match in subsystemMatches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    variableList.Add(new MissionVariable(match.Groups[2].Value));
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in subsystemMatches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[2].Value);

                        string newSubsystemSexp = match.Value.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        content = content.Replace(match.Value, newSubsystemSexp);
                    }
                }
            }

            return content;
        }

        private static string ConvertHardcodedObjectRolesToVariables(string content)
        {
            MatchCollection objectRoleMatches = Regex.Matches(content, "(\\( (?:Add-Object-Role).*?\"(.*?)\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline);
            List<MissionVariable> variableList = new();

            foreach (Match match in objectRoleMatches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    variableList.Add(new MissionVariable(match.Groups[2].Value));
                }
                if (!string.IsNullOrEmpty(match.Groups[3].Value) && !match.Groups[3].Value.StartsWith("@", StringComparison.InvariantCulture) && !variableList.Any(v => v.DefaultValue == match.Groups[3].Value))
                {
                    variableList.Add(new MissionVariable(match.Groups[3].Value));
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in objectRoleMatches)
                {
                    string currentObjectRoleSexp = match.Value;

                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[2].Value);

                        string newObjectRoleSexp = currentObjectRoleSexp.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        content = content.Replace(currentObjectRoleSexp, newObjectRoleSexp);
                        currentObjectRoleSexp = newObjectRoleSexp;
                    }
                    if (!string.IsNullOrEmpty(match.Groups[3].Value) && !match.Groups[3].Value.StartsWith("@", StringComparison.InvariantCulture) && variableList.Any(v => v.DefaultValue == match.Groups[3].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[3].Value);

                        string newObjectRoleSexp = currentObjectRoleSexp.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        content = content.Replace(currentObjectRoleSexp, newObjectRoleSexp);
                    }
                }
            }

            return content;
        }

        private static string ConvertHardcodedMarkedShipsAndWingsToVariables(string content)
        {
            MatchCollection matches = Regex.Matches(content, "(\\( (?:lua-mark-ship|lua-mark-wing).*?\")(.*?)(\".*?\\))", RegexOptions.Singleline);
            List<MissionVariable> variableList = new();

            foreach (Match match in matches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    variableList.Add(new MissionVariable(match.Groups[2].Value));
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[2].Value);

                        string newMarkedObjectSexp = match.Value.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        content = content.Replace(match.Value, newMarkedObjectSexp);
                    }
                }
            }

            return content;
        }

        private static string ConvertHardcodedMarkedSubsystemsToVariables(string content)
        {
            MatchCollection matches = Regex.Matches(content, "(\\( (?:lua-mark-subsystem).*?\".*?\".*?\")(.*?)(\".*?\\))", RegexOptions.Singleline);
            List<MissionVariable> variableList = new();

            foreach (Match match in matches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    variableList.Add(new MissionVariable(match.Groups[2].Value));
                }
            }

            if (variableList.Count > 0)
            {
                content = AddVariablesToSexpVariablesSection(content, variableList);
                string newSexp = PrepareNewSexpForVariables(variableList);
                content = AddEventToManageTranslations(content, newSexp);

                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith("@", StringComparison.InvariantCulture) && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[2].Value);

                        string newMarkedObjectSexp = match.Value.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        content = content.Replace(match.Value, newMarkedObjectSexp);
                    }
                }
            }

            return content;
        }
    }
}
