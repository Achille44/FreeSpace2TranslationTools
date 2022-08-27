using FreeSpace2TranslationTools.Exceptions;
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
        private MainWindow Parent { get; set; }
        private object Sender { get; set; }
        private List<GameFile> Files { get; set; }
        private int CurrentProgress { get; set; }
        private List<Weapon> Weapons { get; set; }
        private List<Ship> Ships { get; set; }

        public XstrManager(MainWindow parent, object sender, List<GameFile> files)
        {
            Parent = parent;
            Sender = sender;
            CurrentProgress = 0;
            Weapons = new List<Weapon>();
            Ships = new List<Ship>();
            Files = files;

            Parent.InitializeProgress(Sender);
            Parent.SetMaxProgress(Files.Count);
        }

        public void LaunchXstrProcess()
        {
            ProcessCampaignFiles();
            ProcessCreditFiles();
            ProcessCutscenesFile();
            ProcessHudGaugeFiles();
            ProcessMainHallFiles();
            ProcessMedalsFile();
            ProcessRankFile();
            // Weapons must be treated before ships because of the way ship turrets are treated!
            ProcessWeaponFiles();
            ProcessShipFiles();
            ProcessMissionFiles();
            ProcessVisualNovelFiles();
        }

        internal static string GenerateAltNames(Match match)
        {
            return AddXstrLineToHardcodedValue("$Alt Name", match);
        }

        internal static string InternationalizeHardcodedValue(Match match)
        {
            return ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
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

        private void ProcessCampaignFiles()
        {
            foreach (GameFile file in Files.Where(x => x.Type == FileType.Campaign))
            {
                ProcessFile(file, new Campaign(file.Content));
            }
        }

        private void ProcessCreditFiles()
        {
            foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-crd.tbm") || x.Name.EndsWith("credits.tbl")))
            {
                ProcessFile(file, new Credits(file.Content));
            }
        }

        private void ProcessCutscenesFile()
        {
            GameFile cutscenes = Files.FirstOrDefault(x => x.Name.EndsWith("cutscenes.tbl"));

            if (cutscenes != null)
            {
                ProcessFile(cutscenes, new Cutscenes(cutscenes.Content));
            }
        }

        private void ProcessHudGaugeFiles()
        {
            foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-hdg.tbm") || x.Name.EndsWith("hud_gauges.tbl")))
            {
                ProcessFile(file, new HudGauges(file.Content));
            }
        }

        private void ProcessMedalsFile()
        {
            GameFile medals = Files.FirstOrDefault(x => x.Name.EndsWith("medals.tbl"));

            if (medals != null)
            {
                ProcessFile(medals, new Medals(medals.Content));
            }
        }

        private void ProcessMainHallFiles()
        {
            foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-hall.tbm") || x.Name.EndsWith("mainhall.tbl")))
            {
                ProcessFile(file, new Mainhall(file.Content));
            }
        }

        private void ProcessRankFile()
        {
            GameFile rankFile = Files.FirstOrDefault(x => x.Name.Contains("rank.tbl"));

            if (rankFile != null)
            {
                ProcessFile(rankFile, new Rank(rankFile.Content));
            }
        }

        private void ProcessShipFiles()
        {
            // Start with ships.tbl
            List<GameFile> shipFiles = Files.Where(x => x.Name.Contains("ships.tbl")).ToList();
            shipFiles.AddRange(Files.Where(x => x.Name.Contains("-shp.tbm")).ToList());

            foreach (GameFile file in shipFiles)
            {
                if (file.Content.Trim() != "")
                {
                    string shipSection = Regex.Match(file.Content, @"#Ship Classes.*?#end", RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;
                    string newContent = shipSection;
                    MatchCollection shipEntries = Regex.Matches(shipSection, @"\n\$Name:.*?(?=\n\$Name|#end)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                    foreach (Match shipEntry in shipEntries.AsEnumerable())
                    {
                        string newEntry = shipEntry.Value;
                        string shipName = SanitizeName(Regex.Match(shipEntry.Value, @"\$Name:(.*)$", RegexOptions.Multiline).Groups[1].Value);

                        if (!Ships.Any(s => s.Name == shipName))
                        {
                            newEntry = Regexp.NoAltNames.Replace(newEntry, new MatchEvaluator(GenerateAltNames));
                            Ships.Add(new Ship { Name = shipName });
                        }

                        Ship ship = Ships.FirstOrDefault(s => s.Name == shipName);

                        MatchCollection subsystems = Regex.Matches(newEntry, @"(\$Subsystem:[ \t]*([^\r\n]*?),[^\r\n]*?\r?\n)(.*?)(?=\$Subsystem:|$)", RegexOptions.Singleline);

                        foreach (Match subsystem in subsystems)
                        {
                            // some subsystems are not visible so don't translate them
                            if (!subsystem.Value.Contains("untargetable") && !subsystem.Value.Contains("afterburner"))
                            {
                                string subsystemName = subsystem.Groups[2].Value.Trim().ToLower();

                                if (!ship.Subsystems.Any(s => s.Name == subsystemName))
                                {
                                    ship.Subsystems.Add(new Subsystem { Name = subsystemName });
                                    newEntry = newEntry.Replace(subsystem.Value, GenerateSubsystems(subsystem));
                                }
                                // in this case the subsystem has already been treated in another file, so just replace hardcoded values with XSTR
                                else
                                {
                                    newEntry = newEntry.Replace(subsystem.Value, GenerateSubsystems(subsystem, true));
                                }
                            }
                        }

                        if (newEntry != shipEntry.Value)
                        {
                            newContent = newContent.Replace(shipEntry.Value, newEntry);
                        }
                    }

                    newContent = Regex.Replace(newContent, @"(\+Tech Description:[ \t]*)(.*?)\r\n", new MatchEvaluator(GenerateShipDescription));

                    // the main problem is that there are two different +Length properties, and only one of them should be translated (the one before $thruster property)
                    newContent = Regex.Replace(newContent, @"(\$Name:(?:(?!\$Name:|\$Thruster).)*?\r\n)([ \t]*\+Length:[ \t]*)([^\r]*?)(\r\n)", new MatchEvaluator(GenerateShipLength), RegexOptions.Singleline);

                    newContent = file.Content.Replace(shipSection, newContent);

                    file.SaveContent(newContent); 
                }

                Parent.IncreaseProgress(Sender, CurrentProgress++);
            }
        }

        private void ProcessWeaponFiles()
        {
            foreach (GameFile file in Files.Where(x => x.Name.EndsWith("-wep.tbm") || x.Name.EndsWith("weapons.tbl")))
            {
                ProcessWeaponFile(file, new WeaponsFile(file.Content));
            }
        }

        private void ProcessMissionFiles()
        {
            foreach (GameFile file in Files.Where(x => x.Type == FileType.Mission).ToList())
            {
                ProcessFile(file, new Mission(file.Content));
            }
        }

        private void ProcessVisualNovelFiles()
        {
            GameFile[] visualNovels = Files.Where(file => file.Type == FileType.Fiction).ToArray();

            foreach (GameFile file in visualNovels)
            {
                try
                {
                    ProcessFile(file, new VisualNovel(file.Content));
                }
                catch (WrongFileFormatException)
                {
                    continue;
                }
            }
        }

        private void ProcessFile(GameFile gameFile, IFile file)
        {
            gameFile.SaveContent(file.GetInternationalizedContent());

            Parent.IncreaseProgress(Sender, CurrentProgress++);
        }

        private void ProcessWeaponFile(GameFile gameFile, IFile file)
        {
            gameFile.SaveContent(file.GetInternationalizedContent(Weapons));

            Parent.IncreaseProgress(Sender, CurrentProgress++);
        }

        private string GenerateSubsystems(Match match, bool replaceOnly = false)
        {
            string newSubsystem = match.Value;
            bool altNameAlreadyExisting = true;
            bool altDamagePopupNameAlreadyExisting = true;

            if (!replaceOnly && !match.Value.Contains("$Alt Subsystem Name:") && !match.Value.Contains("$Alt Subsystem name:"))
            {
                altNameAlreadyExisting = false;
                newSubsystem = Regex.Replace(newSubsystem, @"(\$Subsystem:[ \t]*(.*?),.*?\n)(.*?)", new MatchEvaluator(AddAltSubsystemName));
            }
            else if (!Regex.IsMatch(match.Value, @"\$Alt Subsystem Name:[ \t]*XSTR", RegexOptions.IgnoreCase))
            {
                newSubsystem = Regex.Replace(newSubsystem, @"(.*\$Alt Subsystem Name:[ \t]*)(.*)\r?\n", new MatchEvaluator(ReplaceAltSubsystemName), RegexOptions.IgnoreCase);
            }

            if (!replaceOnly && !match.Value.Contains("$Alt Damage Popup Subsystem Name:"))
            {
                altDamagePopupNameAlreadyExisting = false;

                // if existing, copy the alt name to damage popup name
                if (altNameAlreadyExisting)
                {
                    newSubsystem = Regex.Replace(newSubsystem, "(\\$Alt Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)\\r?\\n)(.*?)", new MatchEvaluator(AddAltDamagePopupSubsystemName), RegexOptions.IgnoreCase);
                }
                else
                {
                    // take 2 lines after subsystem to skip alt subsystem line
                    newSubsystem = Regex.Replace(newSubsystem, @"(\$Subsystem:[ \t]*(.*?),.*?\n.*?\n)(.*?)", new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
            }
            else if (!Regex.IsMatch(match.Value, @"\$Alt Subsystem Name:[ \t]*XSTR", RegexOptions.IgnoreCase))
            {
                // [ \t] because \s includes \r and \n
                newSubsystem = Regex.Replace(newSubsystem, @"(.*\$Alt Damage Popup Subsystem Name:[ \t]*)(.*)\r?\n", new MatchEvaluator(ReplaceAltDamagePopupSubsystemName));
            }

            if (!replaceOnly)
            {
                // if alt damage popup name already existing but not alt name, then copy it to alt name 
                if (!altNameAlreadyExisting && altDamagePopupNameAlreadyExisting)
                {
                    string newName = Regex.Match(newSubsystem, "\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)").Groups[1].Value;
                    newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)", $"$Alt Subsystem Name: XSTR(\"{newName}\", -1)", RegexOptions.IgnoreCase);
                }
                // if there is neither alt name nor alt damage popup, then check if this is missile launcher (SBanks key word) to set a custom alt name 
                else if (!altNameAlreadyExisting && !altDamagePopupNameAlreadyExisting && match.Value.Contains("$Default SBanks:"))
                {
                    newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)", "$Alt Subsystem Name: XSTR(\"Missile lnchr\", -1)", RegexOptions.IgnoreCase);
                    newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"(.*?)\", -1\\)", "$Alt Damage Popup Subsystem Name: XSTR(\"Missile lnchr\", -1)");
                }
                // if there is neither alt name nor alt damage popup, then check if this is gun turret ("PBanks" or "$Turret Reset Delay" key words) to set a custom alt name 
                else if (!altNameAlreadyExisting && !altDamagePopupNameAlreadyExisting)
                {
                    if (match.Value.Contains("$Default PBanks:"))
                    {
                        string turretType = "Turret";
                        string defaultPBank = Regex.Match(match.Value, "\\$Default PBanks:[ \t]*\\([ \t]*\"(.*?)\"").Groups[1].Value;
                        Weapon defaultWeapon = Weapons.FirstOrDefault(w => w.Name == defaultPBank || w.Name.ToUpper() == defaultPBank.ToUpper());

                        if (defaultWeapon != null)
                        {
                            turretType = defaultWeapon.Type;
                        }

                        newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", $"$Alt Subsystem Name: XSTR(\"{turretType}\", -1)", RegexOptions.IgnoreCase);
                        newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", $"$Alt Damage Popup Subsystem Name: XSTR(\"{turretType}\", -1)");
                    }
                    else if (match.Value.Contains("$Turret Reset Delay"))
                    {
                        newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", $"$Alt Subsystem Name: XSTR(\"Turret\", -1)", RegexOptions.IgnoreCase);
                        newSubsystem = Regex.Replace(newSubsystem, "\\$Alt Damage Popup Subsystem Name:[ \t]*XSTR\\(\"([^\r\n]*?)\", -1\\)", $"$Alt Damage Popup Subsystem Name: XSTR(\"Turret\", -1)");
                    }
                }
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

        private string GenerateShipDescription(Match match)
        {
            string result = ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);

            // if $end_multi_text is part of the original line, we have to exlude it from XSTR and put it on a new line
            if (result.Contains("$end_multi_text"))
            {
                result = result.Replace("$end_multi_text", "");
                result += "$end_multi_text\r\n";
            }

            return result;
        }

        /// <summary>
        /// Adds a new line including an XSTR variable
        /// </summary>
        /// <param name="newMarker">Name of the new marker identifying the XSTR variable</param>
        /// <param name="match">Groups[1]: first original line (including \r\n), Groups[2]: hardcoded value to be translated, Groups[3]: line after the hardcoded value</param>
        /// <returns></returns>
        internal static string AddXstrLineToHardcodedValue(string newMarker, Match match)
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
        /// Removes comments, alias and spaces from a name
        /// </summary>
        internal static string SanitizeName(string rawName, bool fullSanatizing = false)
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
    }
}
