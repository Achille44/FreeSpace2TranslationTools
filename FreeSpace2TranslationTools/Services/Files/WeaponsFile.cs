using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services.Files
{
	internal class WeaponsFile(string content) : IFile
	{
		internal string Content { get; set; } = content;

		public string GetInternationalizedContent(bool completeInternationalization = true)
		{
			throw new NotImplementedException();
		}

		public string GetInternationalizedContent(List<Weapon> modWeapons)
		{
			Content = Regexp.HardCodedAltNames.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

			Content = Regexp.HardCodedTurretNames.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

			Content = Regexp.TitlesFullLine.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

			Content = Regexp.DescriptionsFullLine.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

			IEnumerable<Match> weapons = Regexp.GenericEntries.Matches(Content);

			foreach (Match weapon in weapons)
			{
				string newEntry = weapon.Value;

				if (!weapon.Value.Contains("$Alt Name"))
				{
					if (Regexp.Remove.IsMatch(newEntry))
					{
						newEntry = Regexp.Remove.Replace(newEntry, new MatchEvaluator(XstrManager.GenerateAltNames));
					}
					else if (Regexp.Nocreate.IsMatch(newEntry))
					{
						newEntry = Regexp.Nocreate.Replace(newEntry, new MatchEvaluator(XstrManager.GenerateAltNames));
					}
					else
					{
						newEntry = Regexp.NoAltNames.Replace(newEntry, new MatchEvaluator(XstrManager.GenerateAltNames));
					}
				}

				if (!weapon.Value.Contains("+nocreate"))
				{
					if (!weapon.Value.Contains("+Tech Title:") && weapon.Value.Contains("+Tech Description:"))
					{
						newEntry = Regexp.NoTechTitles.Replace(newEntry, new MatchEvaluator(GenerateTechTitle));
					}

					if (!weapon.Value.Contains("+Title:") && weapon.Value.Contains("+Description:"))
					{
						newEntry = Regexp.NoTitles.Replace(newEntry, new MatchEvaluator(GenerateTitle));
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

						if (weapon.Value.Contains("$Turret Name:"))
						{
							string turretName = Regexp.TurretNamesInsideXstr.Match(weapon.Value).Groups[1].Value;
							modWeapons.Add(new Weapon(name, type, true, turretName));
						}
						else
						{
							modWeapons.Add(new Weapon(name, type));
						}
					}
				}

				if (newEntry != weapon.Value)
				{
					Content = Content.Replace(weapon.Value, newEntry);
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
