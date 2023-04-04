using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    internal class WeaponsFile : IFile
    {
        internal string Content { get; set; }

        public WeaponsFile(string content)
        {
            Content = content;
        }

        public string GetInternationalizedContent()
        {
            throw new NotImplementedException();
        }

        public string GetInternationalizedContent(List<Weapon> modWeapons)
        {
            Content = Regexp.HardCodedAltNames.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            Content = Regexp.HardCodedTurretNames.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            Content = Regexp.NoAltNames.Replace(Content, new MatchEvaluator(XstrManager.GenerateAltNames));

            Content = Regexp.Titles.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            Content = Regexp.Descriptions.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            IEnumerable<Match> weapons = Regexp.Weapons.Matches(Content);

            foreach (Match weapon in weapons)
            {
                if (!weapon.Value.Contains("+nocreate"))
                {
                    if (!weapon.Value.Contains("+Tech Title:") && weapon.Value.Contains("+Tech Description:"))
                    {
                        string newEntry = Regexp.NoTechTitles.Replace(weapon.Value, new MatchEvaluator(GenerateTechTitle));

                        Content = Content.Replace(weapon.Value, newEntry);
                    }

                    if (!weapon.Value.Contains("+Title:") && weapon.Value.Contains("+Description:"))
                    {
                        string newEntry = Regexp.NoTitles.Replace(weapon.Value, new MatchEvaluator(GenerateTitle));

                        Content = Content.Replace(weapon.Value, newEntry);
                    }
                }

                // Here we save weapons to use them in ships files for subsystems
                if (weapon.Value.Contains("$Flags:"))
                {
                    string name = Regexp.WeaponNames.Match(weapon.Value).Groups[1].Value.Trim();

                    if (!modWeapons.Any(w => w.Name == name))
                    {
                        string type = "Laser turret";

                        string flags = Regexp.Flags.Match(weapon.Value).Value;

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

                        bool hasTurretName = weapon.Value.Contains("$Turret Name:");

                        modWeapons.Add(new Weapon(name, type, hasTurretName));
                    }
                }
            }

            return Content;
        }

        public string GetInternationalizedContent(List<Ship> modShips)
        {
            throw new NotImplementedException();
        }

        private string GenerateTechTitle(Match match)
        {
            return XstrManager.AddXstrLineToHardcodedValue("\t+Tech Title", match);
        }

        private string GenerateTitle(Match match)
        {
            return XstrManager.AddXstrLineToHardcodedValue("\t+Title", match);
        }
    }
}
