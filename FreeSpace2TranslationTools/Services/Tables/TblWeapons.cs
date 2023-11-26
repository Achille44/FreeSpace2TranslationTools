using FreeSpace2TranslationTools.Services.Entries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Tables
{
	internal class TblWeapons
	{
        public List<EWeapon> Primaries { get; set; }
        public List<EWeapon> Secondaries { get; set; }
		public List<string> AllTables { get; set; }

		public TblWeapons() 
		{
			Primaries = new List<EWeapon>();
			Secondaries = new List<EWeapon>();
			AllTables = new List<string>();
		}

		public List<EWeapon> ExtractInternationalizationContent()
		{
			foreach (string table in AllTables)
			{
				Match primaries = Regexp.Primaries.Match(table);

				if (primaries.Success)
				{
					IEnumerable<Match> entries = Regexp.GenericEntries.Matches(primaries.Value);
					FetchWeapons(entries, Primaries);
				}

				Match secondaries = Regexp.Secondaries.Match(table);

				if (secondaries.Success)
				{
					IEnumerable<Match> entries = Regexp.GenericEntries.Matches(secondaries.Value);
					FetchWeapons(entries, Secondaries);
				}
			}

			List<EWeapon> allWeapons = new();
			allWeapons.AddRange(Primaries);
			allWeapons.AddRange(Secondaries);

			return allWeapons;
		}

		public string GetContent()
		{
			StringBuilder content = new();

			if (Primaries.Count > 0)
			{
				content.Append($"#Primary Weapons{Environment.NewLine}");

				foreach (EWeapon primary in Primaries)
				{
					FillWeaponContent(content, primary);
				}

				content.Append($"{Environment.NewLine}#End{Environment.NewLine}");
			}

			if (Secondaries.Count > 0)
			{
				content.Append($"{Environment.NewLine}#Secondary Weapons{Environment.NewLine}");

				foreach (EWeapon secondary in Secondaries)
				{
					FillWeaponContent(content, secondary);
				}

				content.Append($"{Environment.NewLine}#End");
			}

			return content.ToString();
		}

		private static void FetchWeapons(IEnumerable<Match> entries, List<EWeapon> weapons)
		{
			foreach (Match entry in entries)
			{
				Match name = Regexp.Names.Match(entry.Value);
				Match altName = Regexp.AltNames.Match(entry.Value);
				Match turretName = Regexp.TurretNames.Match(entry.Value);
				Match title = Regexp.Titles.Match(entry.Value);
				Match description = Regexp.Descriptions.Match(entry.Value);
				Match techTitle = Regexp.TechTitles.Match(entry.Value);
				Match techDescription = Regexp.TechDescriptions.Match(entry.Value);
				Match flags = Regexp.Flags.Match(entry.Value);
				EWeapon weapon;

				if (weapons.Exists(p => p.Name == name.Value.Trim()))
				{
					weapon = weapons.First(p => p.Name == name.Value.Trim());
				}
				else
				{
					weapon = new EWeapon()
					{
						Name = name.Value.Trim()
					};

					weapons.Add(weapon);
				}

				if (altName.Success && weapon.AltName == null)
				{
					weapon.AltName = XstrManager.GetValueWithoutXstr(altName.Value);
				}

				if (turretName.Success && weapon.TurretName == null)
				{
					weapon.TurretName = XstrManager.GetValueWithoutXstr(turretName.Value);
				}

				if (title.Success && weapon.Title == null)
				{
					weapon.Title = XstrManager.GetValueWithoutXstr(title.Value);
				}

				if (description.Success && weapon.Description == null)
				{
					weapon.Description = XstrManager.GetValueWithoutXstr(description.Value);
				}

				if (techTitle.Success && weapon.TechTitle == null)
				{
					weapon.TechTitle = XstrManager.GetValueWithoutXstr(techTitle.Value);
				}

				if (techDescription.Success && weapon.TechDescription == null)
				{
					weapon.TechDescription = XstrManager.GetValueWithoutXstr(techDescription.Value);
				}

				if (flags.Success && weapon.Type == null)
				{
					if (flags.Value.Contains("beam"))
					{
						weapon.Type = "Beam turret";
					}
					else if (flags.Value.Contains("Flak"))
					{
						weapon.Type = "Flak turret";
					}
					else if (flags.Value.Contains("Bomb"))
					{
						weapon.Type = "Missile lnchr";
					}
					else
					{
						weapon.Type = "Turret";
					}
				}
			}
		}

		private static void FillWeaponContent(StringBuilder content, EWeapon weapon)
		{
			weapon.AltName ??= XstrManager.GetValueWithoutXstr(weapon.Name);

			if (weapon.Title == null && weapon.Description != null) 
			{
				weapon.Title = XstrManager.GetValueWithoutXstr(weapon.Name);
			}

			if (weapon.TechTitle == null && weapon.TechDescription != null)
			{
				weapon.TechTitle = XstrManager.GetValueWithoutXstr(weapon.Name);
			}

			content.Append($"{Environment.NewLine}$Name: {weapon.Name}{Environment.NewLine}+nocreate{Environment.NewLine}");
			content.Append($"$Alt Name: XSTR(\"{weapon.AltName}\", -1){Environment.NewLine}");

			if (weapon.Title != null)
			{
				content.Append($"+Title: XSTR(\"{weapon.Title}\", -1){Environment.NewLine}");
			}

			if (weapon.Description != null)
			{
				content.Append($"+Description: XSTR(\"{weapon.Description}\", -1){Environment.NewLine}$end_multi_text{Environment.NewLine}");
			}

			if (weapon.TechTitle != null)
			{
				content.Append($"+Tech Title: XSTR(\"{weapon.TechTitle}\", -1){Environment.NewLine}");
			}

			if (weapon.TechDescription != null)
			{
				content.Append($"+Tech Description: XSTR(\"{weapon.TechDescription}\", -1){Environment.NewLine}$end_multi_text{Environment.NewLine}");
			}

			if (weapon.TurretName != null)
			{
				content.Append($"$Turret Name: XSTR(\"{weapon.TurretName}\", -1){Environment.NewLine}");
			}
		}
	}
}
