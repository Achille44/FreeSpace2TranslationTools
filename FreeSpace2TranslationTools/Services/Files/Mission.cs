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
        internal List<string> JumpNodes { get; set; } = new();
        internal List<MissionVariable> Variables { get; set; } = new();
        public Mission(string content)
        {
            Content = content;
        }

        public string GetInternationalizedContent()
        {
            GetOriginalVariables();

            Content = Regexp.Labels.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            Content = Regexp.CallSigns.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            Content = Regexp.MissionShipNames.Replace(Content, new MatchEvaluator(GenerateShipNames));

            ConvertShowSubtitleToShowSubtitleText();

            ExtractShowSubtitleTextContentToMessages();

            ConvertAltToVariables();

            ConvertHardcodedHudTextToVariables();

            ConvertSpecialMessageSendersToVariables();

            Content = Regexp.JumpNodeNames.Replace(Content, new MatchEvaluator(GenerateJumpNodeNames));

			// Only used for FSO < v23.0
			//ConvertJumpNodeReferencesToVariables();

            ConvertFirstSexpParametersToVariables();

            ConvertSecondSexpParametersToVariables();

            ConvertEachSexpStringParameterToVariable();

            // AddVariablesToSexpVariablesSection and AddEventToManageTranslations must be the last methods called here, so add the new ones before those two

            AddVariablesToSexpVariablesSection();

            AddEventToManageTranslations();

            return Content;
        }

        public string GetInternationalizedContent(List<Weapon> modWeapons)
        {
            return GetInternationalizedContent();
        }

        public string GetInternationalizedContent(List<Ship> modShips)
        {
            return GetInternationalizedContent();
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

        private string GenerateShipNames(Match match)
        {
            return XstrManager.AddXstrLineToHardcodedValue("$Display Name", match);
        }

        private string GenerateJumpNodeNames(Match match)
        {
			#region Only used for FSO < v23.0
			//string newName = XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);

			//// if the jump node has already been translated, then don't add it to the list
			//if (match.Value != newName)
			//{
			//    JumpNodes.Add(XstrManager.SanitizeName(match.Groups[2].Value, true));
			//}

			//return newName; 
			#endregion

			return XstrManager.AddXstrLineToHardcodedValue("+Display Name", match);
		}

		/// <summary>
		/// Convert sexp show-subtitle to show-subtitle-text so they can be translated
		/// </summary>
		private void ConvertShowSubtitleToShowSubtitleText()
        {
            foreach (Match match in Regexp.ShowSubtitle.Matches(Content).AsEnumerable())
            {
                MatchCollection parameters = Regexp.ParametersInSexp.Matches(match.Value);

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

                    Content = Content.Replace(match.Value, sexp.Formula);
                }
            }
        }

        /// <summary>
        /// Extract hardcoded strings from show-subtitle-text and put them into new messages so they can be translated
        /// </summary>
        private void ExtractShowSubtitleTextContentToMessages()
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
            string messagesSection = Regexp.MessagesSection.Match(Content).Value;
            IEnumerable<Match> messages = Regexp.Messages.Matches(messagesSection);
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

            IEnumerable<Match> subtitleTextResults = Regexp.SubtitleTexts.Matches(Content);
            string newMessages = "";

            foreach (Match match in subtitleTextResults)
            {
                // skip already existing messages and cases with variables
                if (!allMessages.Contains(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@'))
                {
                    subtitleMessagesCount++;

                    Content = Content.Replace(match.Value, match.Groups[1].Value + autoGeneratedMessage + subtitleMessagesCount + "\"");

                    newMessages += $"$Name: {autoGeneratedMessage}{subtitleMessagesCount}{Environment.NewLine}" +
                        $"$Team: -1{Environment.NewLine}" +
                        $"$MessageNew:  XSTR(\"{match.Groups[2].Value}\", -1){Environment.NewLine}" +
                        $"$end_multi_text{Environment.NewLine}{Environment.NewLine}";

                    allMessages.Add(match.Groups[2].Value);
                }
            }

            if (newMessages != "")
            {
                Content = Content.Replace("#Reinforcements", newMessages + "#Reinforcements");
            }
        }

        /// <summary>
        /// Convert ship alt names to sexp variables so that they can be translated via modify-variable-xstr sexp
        /// </summary>
        private void ConvertAltToVariables()
        {
            if (Regexp.AlternateTypes.IsMatch(Content))
            {
                #region alt from '#Objects' section
                string objects = Regexp.Objects.Match(Content).Value;
                MatchCollection altShips = Regexp.AltShips.Matches(objects);
                #endregion

                // Check at least one alt name is used before starting modifications
                if (altShips.Count > 0)
                {
                    #region alt from '#Alternate Types' section
                    string alternateTypes = Regexp.AlternateTypes.Match(Content).Value;
                    #endregion

                    foreach (Match match in Regexp.AltTypes.Matches(alternateTypes).AsEnumerable())
                    {
                        Alt alt = new(GiveMeAVariableName(match.Groups[1].Value), match.Groups[1].Value);

                        foreach (Match altShip in altShips.AsEnumerable())
                        {
                            if (altShip.Groups[5].Value == alt.DefaultValue)
                            {
                                alt.AddShip(altShip.Groups[1].Value);
                            }
                        }

                        // some alt are not used for unknown reasons, so we don't keep them
                        if (alt.Ships.Count > 0)
                        {
                            AddVariableToListIfNotExisting(alt);
                        }
                    }

                    if (Variables.Where(v => v.Original == false).Count() > 0)
                    {
                        // Remove the 'Alternate Types' section
                        Content = Content.Replace(alternateTypes, "");
                        // Remove all '$Alt' from '#Objects' section
                        Content = Regexp.Alt.Replace(Content, "");
                    }
                }
            }
        }

        /// <summary>
        /// Convert hardcoded texts in hud-set-text sexp to variables to be treated by XSTR
        /// </summary>
        private void ConvertHardcodedHudTextToVariables()
        {
            IEnumerable<Match> textMatches = Regexp.HudTexts.Matches(Content);
            List<MissionVariable> hudVariables = new();
            List<HudText> hudTextList = new();

            foreach (Match match in textMatches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@') && !hudVariables.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    HudText hudText = new(GiveMeAVariableName(match.Groups[2].Value), match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                    hudVariables.Add(hudText);
                    hudTextList.Add(hudText);
                    AddVariableToListIfNotExisting(hudText);
                }
            }

            if (hudVariables.Count > 0)
            {
                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in textMatches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@') && hudVariables.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        HudText hudText = hudTextList.FirstOrDefault(h => h.DefaultValue == match.Groups[2].Value);
                        Content = Content.Replace(match.Value, match.Groups[1] + hudText.NewSexp + match.Groups[3]);
                    }
                }
            }
        }

        private void ConvertSpecialMessageSendersToVariables()
        {
            IEnumerable<Match> messageMatches = Regexp.SendMessages.Matches(Content);
            List<MissionVariable> variableList = new();

            foreach (Match message in messageMatches)
            {
                IEnumerable<Match> specialSenderMatches = Regexp.SpecialSenders.Matches(message.Value);

                // in case of send-message-list, there can be several special senders
                foreach (Match specialSender in specialSenderMatches)
                {
                    if (!variableList.Any(v => v.DefaultValue == specialSender.Groups[1].Value))
                    {
                        MissionVariable variable = new(GiveMeAVariableName(specialSender.Groups[1].Value), specialSender.Groups[1].Value);
                        variableList.Add(variable);
                        AddVariableToListIfNotExisting(variable);
                    }
                }
            }

            if (variableList.Count > 0)
            {
                foreach (Match message in messageMatches)
                {
                    IEnumerable<Match> specialSenderMatches = Regexp.SpecialSenders.Matches(message.Value);

                    string currentMessage = message.Value;

                    // in case of send-message-list, there can be several special senders
                    foreach (Match specialSender in specialSenderMatches)
                    {
                        if (variableList.Any(v => v.DefaultValue == specialSender.Groups[1].Value))
                        {
                            MissionVariable variable = variableList.First(v => v.DefaultValue == specialSender.Groups[1].Value);

                            string newMessage = currentMessage.Replace(specialSender.Value, variable.NewSexp);
                            Content = Content.Replace(currentMessage, newMessage);
                            // once the currentMessage has been replaced in the original content, the newMessage becomes the currentMessage
                            currentMessage = newMessage;
                        }
                    }
                }
            }
        }

		/// <summary>
		/// Only used for FSO < v23.0
		/// </summary>
		private void ConvertJumpNodeReferencesToVariables()
        {
            if (JumpNodes.Count > 0)
            {
                List<MissionVariable> variableList = new();
                // don't take variables section
                string originalContent = Regexp.FromObjectsToWaypoints.Match(Content).Value;

                foreach (string jumpNode in JumpNodes)
                {
                    // find all references outside XSTR
                    if (Regexp.GetJumpNodeReferences(jumpNode).IsMatch(originalContent))
                    {
                        MissionVariable variable = new(GiveMeAVariableName(jumpNode), jumpNode);
                        variableList.Add(variable);
                        AddVariableToListIfNotExisting(variable);
                    }
                }

                if (variableList.Count > 0)
                {
                    string newContent = originalContent;

                    foreach (MissionVariable variable in variableList)
                    {
                        newContent = Regexp.GetJumpNodeReferences(variable.DefaultValue).Replace(newContent, variable.NewSexp);
                    }

                    Content = Content.Replace(originalContent, newContent);
                }
            }
        }

        private void ConvertFirstSexpParametersToVariables()
        {
            List<MissionVariable> variableList = new();

            IEnumerable<Match> matches = Regexp.FirstSexpParameters.Matches(Content);

            foreach (Match match in matches)
            {
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@') && match.Groups[2].Value != "<argument>" && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    MissionVariable variable = new(GiveMeAVariableName(match.Groups[2].Value), match.Groups[2].Value);
                    variableList.Add(variable);
                    AddVariableToListIfNotExisting(variable);
                }
            }

            if (variableList.Count > 0)
            {
                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@') && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[2].Value);

                        string newSexp = match.Value.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        Content = Content.Replace(match.Value, newSexp);
                    }
                }
            }
        }

        private void ConvertSecondSexpParametersToVariables()
        {
            IEnumerable<Match> matches = Regexp.SecondSexpParameters.Matches(Content);
            List<MissionVariable> variableList = new();

            foreach (Match match in matches)
            {
                // Only treat sexp not using variables
                if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@') && !variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                {
                    MissionVariable variable = new(GiveMeAVariableName(match.Groups[2].Value), match.Groups[2].Value);
                    variableList.Add(variable);
                    AddVariableToListIfNotExisting(variable);
                }
            }

            if (variableList.Count > 0)
            {
                // let's cycle again to catch all sexps that could have different conditions or space/tab count...
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Groups[2].Value) && !match.Groups[2].Value.StartsWith('@') && variableList.Any(v => v.DefaultValue == match.Groups[2].Value))
                    {
                        MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == match.Groups[2].Value);

                        string newSexp = match.Value.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                        Content = Content.Replace(match.Value, newSexp);
                    }
                }
            }
        }

        private void ConvertEachSexpStringParameterToVariable()
        {
            List<MissionVariable> variableList = new();

            IEnumerable<Match> sexpMatches = Regexp.Sexp.Matches(Content);

            foreach (Match sexp in sexpMatches)
            {
                IEnumerable<Match> nameMatches = Regexp.StringParameters.Matches(sexp.Value);

                foreach (Match name in nameMatches)
                {
                    if (!variableList.Any(v => v.DefaultValue == name.Groups[1].Value) && !name.Groups[1].Value.StartsWith('@'))
                    {
                        MissionVariable variable = new(GiveMeAVariableName(name.Groups[1].Value), name.Groups[1].Value);
                        variableList.Add(variable);
                        AddVariableToListIfNotExisting(variable);
                    }
                }
            }

            if (variableList.Count > 0)
            {
                foreach (Match sexp in sexpMatches)
                {
                    string newNavSexp = sexp.Value;

                    foreach (MissionVariable variable in variableList)
                    {
                        newNavSexp = newNavSexp.Replace($"\"{variable.DefaultValue}\"", variable.NewSexp);
                    }

                    Content = Content.Replace(sexp.Value, newNavSexp);
                }
            }
        }

        private void AddVariablesToSexpVariablesSection()
        {
            if (Variables.Where(v => v.Original == false).Count() > 0)
            {
                // Create '#Sexp_variables' section if not exists
                if (!Content.Contains("#Sexp_variables"))
                {
                    string beforeSexp = Regexp.BeforeSexp.Match(Content).Value;

                    string newSection = $"#Sexp_variables{Environment.NewLine}" +
                        $"{Environment.NewLine}" +
                        $"$Variables:{Environment.NewLine}" +
                        $"({Environment.NewLine}" +
                        $"){Environment.NewLine}" +
                        $"{Environment.NewLine}" +
                        $"{beforeSexp}";

                    Content = Content.Replace(beforeSexp, newSection);
                }

                string sexpVariablesSection = Regexp.SexpVariablesSection.Match(Content).Value;
                int variableId = Regexp.VariableIds.Matches(sexpVariablesSection).Count;

                string newSexpVariablesSection = "";

                foreach (MissionVariable variable in Variables.Where(v => v.Original == false))
                {
                    newSexpVariablesSection += $"\t\t{variableId}\t\t\"{variable.Name}\"\t\t\"{variable.DefaultValue}\"\t\t\"string\"{Environment.NewLine}";
                    variableId++;
                }

                string endOfVariables = Regexp.EndOfVariablesSection.Match(Content).Value;

                Content = Content.Replace(endOfVariables, newSexpVariablesSection + endOfVariables);
            }
        }

        private void AddEventToManageTranslations()
        {
            if (Variables.Any(v => v.Original == false))
            {
                // very unorthodox way to add the event but it allows me to manage the case when this event already exists in the original file
                string eventForAltNamesTitle = "Manage translation variables";
                string eventEnd = $"){Environment.NewLine}"
                    + $"+Name: {eventForAltNamesTitle}{Environment.NewLine}"
                    + $"+Repeat Count: 1{Environment.NewLine}"
                    + $"+Interval: 1{Environment.NewLine}{Environment.NewLine}";

                if (!Content.Contains(eventForAltNamesTitle))
                {
                    string events = Regexp.EventsSection.Match(Content).Value;

                    string eventBeginning = $"$Formula: ( when {Environment.NewLine}"
                        + $"   ( true ) {Environment.NewLine}";

                    Content = Content.Replace(events, events + eventBeginning + eventEnd);
                }

                string newSexp = PrepareNewSexpForVariables();

                Content = Content.Replace(eventEnd, newSexp + eventEnd);
            }
        }

        private string GiveMeAVariableName(string defaultValue)
        {
            if (!Variables.Any(v => v.DefaultValue == defaultValue && v.Original == false))
            {
                int i = 0;
                string name;

                do
                {
                    i++;
                    name = "i18nVariable" + i;
                } while (Variables.Any(v => v.Name == name));

                return name;
            }
            else
            {
                return Variables.First(v => v.DefaultValue == defaultValue).Name;
            }
        }

        private void AddVariableToListIfNotExisting(MissionVariable variable)
        {
            if (!Variables.Any(v => v.Name == variable.Name))
            {
                Variables.Add(variable);
            }
        }

        private string PrepareNewSexpForVariables()
        {
            string newSexp = "";

            foreach (MissionVariable variable in Variables.Where(v => v.Original == false))
            {
                newSexp += variable.ModifyVariableXstr();

                if (variable.GetType() == typeof(Alt))
                {
                    Alt alt = (Alt)variable;

                    newSexp += alt.ShipChangeAltName();
                }
            }

            return newSexp;
        }

        private void GetOriginalVariables()
        {
            string sexpVariablesSection = Regexp.SexpVariablesSection.Match(Content).Value;
            IEnumerable<Match> variableNames = Regexp.Variables.Matches(sexpVariablesSection);

            foreach (Match variableName in variableNames)
            {
                Variables.Add(new(variableName.Groups[1].Value, variableName.Groups[2].Value, true));
            }
        }
    }
}
