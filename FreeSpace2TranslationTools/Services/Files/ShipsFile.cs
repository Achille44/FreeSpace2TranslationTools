using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace FreeSpace2TranslationTools.Services
{
    internal class ShipsFile : IFile
    {
        private string Content;
        private readonly List<Weapon> ModWeapons;

        public ShipsFile(string content, List<Weapon> modWeapons)
        {
            Content = content;
            ModWeapons = modWeapons;
        }

        public string GetInternationalizedContent()
        {
            throw new NotImplementedException();
        }

        string IFile.GetInternationalizedContent(List<Weapon> modWeapons)
        {
            throw new NotImplementedException();
        }

        public string GetInternationalizedContent(List<Ship> modShips)
        {
            if (Content.Trim() != "")
            {
                string shipSection = Regexp.ShipSection.Match(Content).Value;
                string newContent = shipSection;
                IEnumerable<Match> shipEntries = Regexp.ShipEntries.Matches(shipSection);

                foreach (Match shipEntry in shipEntries)
                {
                    string newEntry = shipEntry.Value;
                    string shipName = XstrManager.SanitizeName(Regexp.ShipNames.Match(shipEntry.Value).Groups[1].Value);

                    if (!modShips.Any(s => s.Name == shipName))
                    {
                        newEntry = Regexp.NoAltNames.Replace(newEntry, new MatchEvaluator(XstrManager.GenerateAltNames));
                        modShips.Add(new Ship { Name = shipName });
                    }

                    Ship ship = modShips.FirstOrDefault(s => s.Name == shipName);

                    IEnumerable<Match> subsystems = Regexp.Subsystems.Matches(newEntry);

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

                newContent = Regexp.TechDescriptions.Replace(newContent, new MatchEvaluator(GenerateShipDescription));

                newContent = Regexp.ShipLength.Replace(newContent, new MatchEvaluator(GenerateShipLength));

                Content = Content.Replace(shipSection, newContent);
            }

            return Content;
        }

        private string GenerateSubsystems(Match match, bool replaceOnly = false)
        {
            string newSubsystem = match.Value;
            bool altNameAlreadyExisting = true;
            bool altDamagePopupNameAlreadyExisting = true;

            if (!replaceOnly && !match.Value.Contains("$Alt Subsystem Name:") && !match.Value.Contains("$Alt Subsystem name:"))
            {
                altNameAlreadyExisting = false;
                newSubsystem = Regexp.SubsystemNames.Replace(newSubsystem, new MatchEvaluator(AddAltSubsystemName));
            }
            else if (!Regex.IsMatch(match.Value, @"\$Alt Subsystem Name:[ \t]*XSTR", RegexOptions.IgnoreCase))
            {
                newSubsystem = Regexp.AltSubsystemNames.Replace(newSubsystem, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));
            }

            if (!replaceOnly && !match.Value.Contains("$Alt Damage Popup Subsystem Name:"))
            {
                altDamagePopupNameAlreadyExisting = false;

                // if existing, copy the alt name to damage popup name
                if (altNameAlreadyExisting)
                {
                    newSubsystem = Regexp.InternationalizedAltSubsystemNamesWithFollowingLine.Replace(newSubsystem, new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
                else
                {
                    newSubsystem = Regexp.SubsystemsWithAltSubsystems.Replace(newSubsystem, new MatchEvaluator(AddAltDamagePopupSubsystemName));
                }
            }
            else if (!Regexp.InternationalizedSubsystemNames.IsMatch(match.Value))
            {
                newSubsystem = Regexp.AltDamagePopupSubsystemNames.Replace(newSubsystem, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));
            }

            if (!replaceOnly)
            {
                // if alt damage popup name already existing but not alt name, then copy it to alt name 
                if (!altNameAlreadyExisting && altDamagePopupNameAlreadyExisting)
                {
                    string newName = Regexp.InternationalizedAltDamagePopupSubsystemNames.Match(newSubsystem).Groups[1].Value;
                    newSubsystem = Regexp.InternationalizedAltSubsystemNames.Replace(newSubsystem, $"$Alt Subsystem Name: XSTR(\"{newName}\", -1)");
                }
                // if there is neither alt name nor alt damage popup, then check if this is missile launcher (SBanks key word) to set a custom alt name 
                else if (!altNameAlreadyExisting && !altDamagePopupNameAlreadyExisting && match.Value.Contains("$Default SBanks:"))
                {
                    newSubsystem = Regexp.InternationalizedAltSubsystemNames.Replace(newSubsystem, "$Alt Subsystem Name: XSTR(\"Missile lnchr\", -1)");
                    newSubsystem = Regexp.InternationalizedAltDamagePopupSubsystemNames.Replace(newSubsystem, "$Alt Damage Popup Subsystem Name: XSTR(\"Missile lnchr\", -1)");
                }
                // if there is neither alt name nor alt damage popup, then check if this is gun turret ("PBanks" or "$Turret Reset Delay" key words) to set a custom alt name 
                else if (!altNameAlreadyExisting && !altDamagePopupNameAlreadyExisting)
                {
                    if (match.Value.Contains("$Default PBanks:"))
                    {
                        string turretType = "Turret";
                        string defaultPBank = Regexp.DefaultPBanks.Match(match.Value).Groups[1].Value;
                        Weapon defaultWeapon = ModWeapons.FirstOrDefault(w => w.Name == defaultPBank || w.Name.ToUpper() == defaultPBank.ToUpper());

                        if (defaultWeapon != null)
                        {
                            turretType = defaultWeapon.Type;
                        }

                        newSubsystem = Regexp.InternationalizedAltSubsystemNames.Replace(newSubsystem, $"$Alt Subsystem Name: XSTR(\"{turretType}\", -1)");
                        newSubsystem = Regexp.InternationalizedAltDamagePopupSubsystemNames.Replace(newSubsystem, $"$Alt Damage Popup Subsystem Name: XSTR(\"{turretType}\", -1)");
                    }
                    else if (match.Value.Contains("$Turret Reset Delay"))
                    {
                        newSubsystem = Regexp.InternationalizedAltSubsystemNames.Replace(newSubsystem, $"$Alt Subsystem Name: XSTR(\"Turret\", -1)");
                        newSubsystem = Regexp.InternationalizedAltDamagePopupSubsystemNames.Replace(newSubsystem, $"$Alt Damage Popup Subsystem Name: XSTR(\"Turret\", -1)");
                    }
                }
            }

            return newSubsystem;
        }

        private string GenerateShipDescription(Match match)
        {
            string result = XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);

            // if $end_multi_text is part of the original line, we have to exlude it from XSTR and put it on a new line
            if (result.Contains("$end_multi_text"))
            {
                result = result.Replace("$end_multi_text", "");
                result += "$end_multi_text\r\n";
            }

            return result;
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
                return match.Groups[1].Value + XstrManager.ReplaceHardcodedValueWithXstr(match.Groups[2].Value + match.Groups[3].Value + match.Groups[4].Value, match.Groups[2].Value, match.Groups[3].Value);
            }
        }

        private string AddAltSubsystemName(Match match)
        {
            return XstrManager.AddXstrLineToHardcodedValue("$Alt Subsystem Name", match);
        }

        private string AddAltDamagePopupSubsystemName(Match match)
        {
            return XstrManager.AddXstrLineToHardcodedValue("$Alt Damage Popup Subsystem Name", match);
        }
    }
}
