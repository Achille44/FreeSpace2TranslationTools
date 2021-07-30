using System;
using System.Collections.Generic;
using System.Globalization;
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
        public List<Weapon> Weapons { get; set; }

        public List<Ship> Ships { get; set; }

        public XstrManager(MainWindow parent, object sender, string modFolder, string destinationFolder)
        {
            Parent = parent;
            Sender = sender;
            ModFolder = modFolder;
            DestinationFolder = destinationFolder;
            CurrentProgress = 0;
            Weapons = new List<Weapon>();
            Ships = new List<Ship>();

            FilesList = Utils.GetFilesWithXstrFromFolder(modFolder);

            Parent.InitializeProgress(Sender);
            Parent.SetMaxProgress(FilesList.Count);
        }

        #region public methods

        public void LaunchXstrProcess()
        {
            ProcessCampaignFiles();
            ProcessCreditFiles();
            ProcessHudGaugeFiles();
            ProcessMainHallFiles();
            ProcessMedalsFile();
            ProcessRankFile();
            // Weapons must be treated before ships!
            ProcessWeaponFiles();
            ProcessShipFiles();
            ProcessMissionFiles();
        }
        #endregion

        private void ProcessCampaignFiles()
        {
            List<string> campaignFiles = FilesList.Where(x => x.EndsWith(".fc2")).ToList();

            foreach (string file in campaignFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = Regex.Replace(sourceContent, @"(.*?\$Name: )((?!XSTR).*)\r\n", new MatchEvaluator(GenerateCampaignNames));

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        /// <summary>
        /// Replace all hardcoded credit lines with XSTR
        /// </summary>
        /// <param name="filesList"></param>
        /// <param name="modFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="currentProgress"></param>
        /// <param name="sender"></param>
        private void ProcessCreditFiles()
        {
            List<string> creditFiles = FilesList.Where(x => x.Contains("-crd.tbm") || x.Contains("credits.tbl")).ToList();

            foreach (string file in creditFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = Regex.Replace(sourceContent, @"(^)((?!(XSTR|\$|#End|#end)).+?)\r\n", new MatchEvaluator(GenerateCredits), RegexOptions.Multiline);

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        private void ProcessHudGaugeFiles()
        {
            List<string> creditFiles = FilesList.Where(x => x.EndsWith("-hdg.tbm") || x.EndsWith("hud gauges.tbl")).ToList();

            foreach (string file in creditFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = Regex.Replace(sourceContent, @"(.*?Header Text: )((?!XSTR).*)\r\n", new MatchEvaluator(GenerateHudGauges));

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
        private void ProcessMedalsFile()
        {
            string medalsFile = FilesList.FirstOrDefault(x => x.Contains("medals.tbl"));

            if (medalsFile != null)
            {
                string sourceContent = File.ReadAllText(medalsFile);

                string newContent = Regex.Replace(sourceContent, @"(\$Name:[ \t]*(.*?)\r\n)([^\r]*\$Bitmap)", new MatchEvaluator(GenerateAltNames), RegexOptions.Multiline);

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
        private void ProcessMainHallFiles()
        {
            List<string> mainHallFiles = FilesList.Where(x => x.Contains("-hall.tbm") || x.Contains("mainhall.tbl")).ToList();

            // all door descriptions without XSTR variable
            Regex regexDoorDescription = new(@"(.*\+Door description:\s*)((?!XSTR).*)\r\n", RegexOptions.Compiled);

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

        private void ProcessRankFile()
        {
            string medalsFile = FilesList.FirstOrDefault(x => x.Contains("rank.tbl"));

            if (medalsFile != null)
            {
                string sourceContent = File.ReadAllText(medalsFile);

                string newContent = Regex.Replace(sourceContent, @"(.*\$Name:\s*)(.*?)\r\n", new MatchEvaluator(GenerateRanks));

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(medalsFile, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        private void ProcessShipFiles()
        {
            // Start with ships.tbl
            List<string> shipFiles = FilesList.Where(x => x.Contains("ships.tbl")).ToList();
            shipFiles.AddRange(FilesList.Where(x => x.Contains("-shp.tbm")).ToList());

            foreach (string file in shipFiles)
            {
                string sourceContent = File.ReadAllText(file);
                string shipSection = Regex.Match(sourceContent, @"#Ship Classes.*?(#End|#end)", RegexOptions.Singleline).Value;
                string newContent = shipSection;
                MatchCollection shipEntries = Regex.Matches(shipSection, @"\n\$Name:.*?(?=\n\$Name|#end|#End)", RegexOptions.Singleline);

                foreach (Match shipEntry in shipEntries)
                {
                    string newEntry = shipEntry.Value;
                    string shipName = Utils.SanitizeName(Regex.Match(shipEntry.Value, @"\$Name:([^\r]*)").Groups[1].Value);

                    if (!Ships.Any(s => s.Name == shipName))
                    {
                        newEntry = Utils.RegexNoAltNames.Replace(newEntry, new MatchEvaluator(GenerateAltNames));
                        Ships.Add(new Ship { Name = shipName });
                    }

                    Ship ship = Ships.FirstOrDefault(s => s.Name == shipName);

                    MatchCollection subsystems = Regex.Matches(newEntry, @"(\$Subsystem:\s+(.*?),.*?\r\n)(.*?)(?=\r|\$Subsystem:)", RegexOptions.Singleline);

                    foreach (Match subsystem in subsystems)
                    {
                        string subsystemName = subsystem.Groups[2].Value.Trim();

                        if (!ship.Subsystems.Any(s => s.Name == subsystemName))
                        {
                            ship.Subsystems.Add(new Subsystem { Name = subsystemName });
                            newEntry = newEntry.Replace(subsystem.Value, GenerateSubsystems(subsystem));
                        }
                    }

                    if (newEntry != shipEntry.Value)
                    {
                        newContent = newContent.Replace(shipEntry.Value, newEntry);
                    }
                }

                //string newContent = Regex.Replace(shipSection, @"(\$Subsystem:\s+(.*?),.*?\r\n)(.*?)(?=\$Name|\$Subsystem:|#End)", new MatchEvaluator(GenerateSubsystems), RegexOptions.Singleline);

                //newContent = Utils.RegexNoAltNames.Replace(newContent, new MatchEvaluator(GenerateAltNames));

                // the main problem is that there are two different +Length properties, and only one of them should be translated (the one before $thruster property)
                newContent = Regex.Replace(newContent, @"(\$Name:(?:(?!\$Name:).)*?\r\n)([ \t]*\+Length:[ \t]*)([^\r]*?)(\r\n)", new MatchEvaluator(GenerateShipLength), RegexOptions.Singleline);

                newContent = sourceContent.Replace(shipSection, newContent);

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        private void ProcessWeaponFiles()
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
                    if (!weapon.Value.Contains("+nocreate"))
                    {
                        if (!weapon.Value.Contains("+Tech Title:") && weapon.Value.Contains("+Tech Description:"))
                        {
                            string newEntry = Regex.Replace(weapon.Value, @"(\$Name:\s*(.*?)\r\n.*?\r\n)(\s*\+Tech Anim:|\s*\+Tech Description:)", new MatchEvaluator(GenerateTechTitle), RegexOptions.Singleline);

                            newContent = newContent.Replace(weapon.Value, newEntry);
                        }

                        if (!weapon.Value.Contains("+Title:") && weapon.Value.Contains("+Description:"))
                        {
                            string newEntry = Regex.Replace(weapon.Value, @"(\$Name:\s*(.*?)\r\n.*?\r\n)(\s*\+Description:)", new MatchEvaluator(GenerateTitle), RegexOptions.Singleline);

                            newContent = newContent.Replace(weapon.Value, newEntry);
                        }
                    }

                    // Here we save weapons to use them in ships files for subsystems
                    if (weapon.Value.Contains("$Flags:"))
                    {
                        string name = Regex.Match(weapon.Value, @"\$Name:[ \t]*([^\r]*)").Groups[1].Value.Trim();

                        if (!Weapons.Any(w => w.Name == name))
                        {
                            string type = "Laser turret";

                            string flags = Regex.Match(weapon.Value, "\\$Flags:(.*?)\"[ \t]*\\)").Value;

                            if (flags.Contains("beam"))
                            {
                                type = "Beam turret";
                            }
                            else if (flags.Contains("Flak"))
                            {
                                type = "Flak turret";
                            }
                            else if (flags.Contains("Bomb"))
                            {
                                type = "Missile lnchr";
                            }
                            else if (flags.Contains("Ballistic"))
                            {
                                type = "Turret";
                            }

                            Weapons.Add(new Weapon(name, type));
                        }
                    }
                }

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        private void ProcessMissionFiles()
        {
            List<string> missionFiles = FilesList.Where(x => x.Contains(".fs2")).ToList();

            // all labels without XSTR variable (everything after ':' is selected in group 1, so comments (;) must be taken away
            // ex: $Label: Alpha 1 ==> $Label: XSTR("Alpha 1", -1)
            Regex regexLabels = new(@"(.*\$label:\s*)((?!XSTR).*)\r\n", RegexOptions.Compiled);

            // ex: $Name: Psamtik   ==>     $Name: Psamtik
            //     $Class.......    ==>     $Display Name: XSTR("Psamtik", -1)
            //                      ==>     $Class......
            Regex regexShipNames = new(@"(\$Name:\s*(.*?)\r\n)(\$Class)", RegexOptions.Multiline | RegexOptions.Compiled);

            foreach (string file in missionFiles)
            {
                string sourceContent = File.ReadAllText(file);

                string newContent = regexLabels.Replace(sourceContent, new MatchEvaluator(GenerateLabels));

                newContent = Regex.Replace(newContent, @"(.*\$Jump Node Name:[ \t]*)(.*?)\r\n", new MatchEvaluator(GenerateJumpNodeNames));

                newContent = Regex.Replace(newContent, @"(.*\$Callsign:[ \t]*)(.*?)\r\n", new MatchEvaluator(GenerateCallSigns));

                newContent = regexShipNames.Replace(newContent, new MatchEvaluator(GenerateShipNames));

                newContent = ConvertShowSubtitleToShowSubtitleText(newContent);

                newContent = ExtractShowSubtitleTextContentToMessages(newContent);

                newContent = ConvertAltToVariables(newContent);

                newContent = ConvertHardcodedHudTextToVariables(newContent);

                newContent = ConvertSpecialMessageSendersToVariables(newContent);

                if (sourceContent != newContent)
                {
                    Utils.CreateFileWithNewContent(file, ModFolder, DestinationFolder, newContent);
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

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
                newSubsystem = Regex.Replace(newSubsystem, @"(.*\$Alt Subsystem Name:\s*)(.*)\r\n", new MatchEvaluator(ReplaceAltSubsystemName));
            }

            if (!match.Value.Contains("$Alt Damage Popup Subsystem Name:"))
            {
                altDamagePopupNameAlreadyExisting = false;

                // if existing, copy the alt name to damage popup name
                if (altNameAlreadyExisting)
                {
                    newSubsystem = Regex.Replace(newSubsystem, "(\\$Alt Subsystem Name:.*XSTR\\(\"(.*?)\", -1\\)\\r\\n)(.*?)", new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
                else
                {
                    // take 2 lines after subsystem to skip alt subsystem line
                    newSubsystem = Regex.Replace(newSubsystem, @"(\$Subsystem:\s*(.*?),.*?\r\n.*?\r\n)(.*?)", new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
            }
            else if (!Regex.IsMatch(match.Value, @"\$Alt Subsystem Name:\s*XSTR"))
            {
                // [ \t] because \s includes \r and \n
                newSubsystem = Regex.Replace(newSubsystem, @"(.*\$Alt Damage Popup Subsystem Name:[ \t]*)(.*)\r\n", new MatchEvaluator(ReplaceAltDamagePopupSubsystemName));
            }

            // if alt damage popup name already existing but not alt name, then copy it to alt name 
            if (!altNameAlreadyExisting && altDamagePopupNameAlreadyExisting)
            {
                string newName = Regex.Match(newSubsystem, "\\$Alt Damage Popup Subsystem Name:.*XSTR\\(\"(.*?)\", -1\\)").Groups[1].Value;
                newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:.*XSTR\\(\"(.*?)\", -1\\)", $"$Alt Subsystem Name: XSTR(\"{newName}\", -1)");
            }
            // if there is neither alt name nor alt damage popup, then check if this is missile launcher (SBanks key word) to set a custom alt name 
            else if (!altNameAlreadyExisting && !altDamagePopupNameAlreadyExisting && match.Value.Contains("$Default SBanks:"))
            {
                newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)", "$Alt Subsystem Name: XSTR(\"Missile lnchr\", -1)");
                newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)", "$Alt Damage Popup Subsystem Name: XSTR(\"Missile lnchr\", -1)");
            }
            // if there is neither alt name nor alt damage popup, then check if this is gun turret (PBanks key word) to set a custom alt name 
            else if (!altNameAlreadyExisting && !altDamagePopupNameAlreadyExisting && match.Value.Contains("$Default PBanks:"))
            {
                string turretType = "Turret";
                string defaultPBank = Regex.Match(match.Value, "\\$Default PBanks:[ \t]*\\([ \t]*\"(.*?)\"").Groups[1].Value;
                Weapon defaultWeapon = Weapons.FirstOrDefault(w => w.Name == defaultPBank);

                if (defaultWeapon != null)
                {
                    turretType = defaultWeapon.Type;
                }

                newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:[ \t]*XSTR\\(\"([^\r]*?)\", -1\\)", $"$Alt Subsystem Name: XSTR(\"{turretType}\", -1)");
                newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"([^\r]*?)\", -1\\)", $"$Alt Damage Popup Subsystem Name: XSTR(\"{turretType}\", -1)");
            }

            return newSubsystem;
        }

        private string AddAltSubsystemName(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Subsystem Name", match);
        }

        private string ReplaceAltSubsystemName(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string AddAltDamagePopupSubsystemName(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Damage Popup Subsystem Name", match);
        }

        private string ReplaceAltDamagePopupSubsystemName(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateCallSigns(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateCampaignNames(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateCredits(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateHudGauges(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateJumpNodeNames(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateShipLength(Match match)
        {
            if (match.Groups[1].Value.Contains("$thruster:"))
            {
                return match.Value;
            }
            else
            {
                // don't send groups[1] content in the parameters, otherwise the method will check for a ';' in every line
                return match.Groups[1].Value + ReplaceHardcodedValueWithXstr(match.Groups[2].Value + match.Groups[3].Value + match.Groups[4].Value, match.Groups[2].Value, match.Groups[3].Value);
            }
        }

        private string GenerateRanks(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateAltNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Name", match);
        }

        private string GenerateDoorDescriptions(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateTechTitle(Match match)
        {
            return AddXstrLineToHardcodedValue("\t+Tech Title", match);
        }
        private string GenerateTitle(Match match)
        {
            return AddXstrLineToHardcodedValue("\t+Title", match);
        }

        private string GenerateLabels(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
        }

        private string GenerateShipNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Display Name", match);
        }

        /// <summary>
        /// Replaces an hardcoded line with an XSTR variable
        /// </summary>
        /// <param name="originalMatch"></param>
        /// <param name="beginningOfLine"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ReplaceHardcodedValueWithXstr(string originalMatch, string beginningOfLine, string value)
        {
            // if this is a comment or if it's already XSTR, then don't touch it and return the original match
            if (beginningOfLine.Contains(";") || value.Contains("XSTR"))
            {
                return originalMatch;
            }
            else
            {
                string[] values = value.Trim().Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
                string sanatizedValue = values.Length == 0 ? "" : values[0].Replace("\"", "$quote");
                string result = $"{beginningOfLine}XSTR(\"{sanatizedValue}\", -1)";

                if (values.Length > 1)
                {
                    result += $" ;{values[1]}";
                }

                result += Environment.NewLine;

                return result;
            }
        }

        /// <summary>
        /// Adds a new line including an XSTR variable
        /// </summary>
        /// <param name="newMarker">Name of the new marker identifying the XSTR variable</param>
        /// <param name="match">Groups[1]: first original line (including \r\n), Groups[2]: hardcoded value to be translated, Groups[3]: line after the hardcoded value</param>
        /// <returns></returns>
        private string AddXstrLineToHardcodedValue(string newMarker, Match match)
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

        private string ConvertXPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 900;
            double output = 88;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString(CultureInfo.InvariantCulture);
        }

        private string ConvertYPositionFromAbsoluteToRelative(string absolute)
        {
            // values determined testing the mission bp-09 of blue planet
            double input = 500;
            double output = 65;

            return Convert.ToInt32(Math.Round(int.Parse(absolute) / input * output)).ToString(CultureInfo.InvariantCulture);
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

        private string AddVariablesToSexpVariablesSection(string content, List<MissionVariable> variableList)
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

            string endOfVariables = Regex.Match(content, @"\)\r\n\r\n(#Fiction Viewer|#Command Briefing)", RegexOptions.Multiline).Value;

            return content.Replace(endOfVariables, newSexpVariablesSection + endOfVariables);
        }

        private string AddEventToManageTranslations(string content, string newSexp)
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

        private string PrepareNewSexpForAltNames(List<Alt> altList)
        {
            string newSexp = string.Empty;

            foreach (Alt alt in altList)
            {
                newSexp += alt.ModifyVariableXstr() + alt.ShipChangeAltName();
            }

            return newSexp;
        }

        private string PrepareNewSexpForVariables(List<MissionVariable> VariableList)
        {
            string newSexp = string.Empty;

            foreach (MissionVariable variable in VariableList)
            {
                newSexp += variable.ModifyVariableXstr();
            }

            return newSexp;
        }

        /// <summary>
        /// Convert hardcoded texts in hud-set-text sexp to variables to be treated by XSTR
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string ConvertHardcodedHudTextToVariables(string content)
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

        private string ConvertSpecialMessageSendersToVariables(string content)
        {
            MatchCollection messageMatches = Regex.Matches(content, @"\( send-message.*?\r\n[ \t]*\)\r\n", RegexOptions.Singleline);
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

                    // in case of send-message-list, there can be several special senders
                    foreach (Match specialSender in specialSenderMatches)
                    {
                        if (variableList.Any(v => v.DefaultValue == specialSender.Groups[1].Value))
                        {
                            MissionVariable variable = variableList.FirstOrDefault(v => v.DefaultValue == specialSender.Groups[1].Value);

                            string newMessage = message.Value.Replace(specialSender.Value, variable.NewSexp);
                            content = content.Replace(message.Value, newMessage);
                        }
                    }
                }
            }

            return content;
        }
    }
}
