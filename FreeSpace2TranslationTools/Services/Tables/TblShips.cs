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
	internal class TblShips
	{
		public List<EShip> Ships { get; set; }
		public List<EWeapon> ModWeapons { get; set; }
		public List<string> AllTables { get; set; }

		public TblShips(List<EWeapon> modWeapons)
		{
			Ships = new List<EShip>();
			AllTables = new List<string>();
			ModWeapons = modWeapons;
		}

		public void ExtractInternationalizationContent()
		{
			foreach (string table in AllTables)
			{
				Match shipClasses = Regexp.ShipSection.Match(table);

				IEnumerable<Match> entries = Regexp.GenericEntries.Matches(shipClasses.Value);

				foreach (Match entry in entries)
				{
					Match name = Regexp.Names.Match(entry.Value);
					Match altName = Regexp.AltNames.Match(entry.Value);
					Match type = Regexp.Types.Match(entry.Value);
					Match maneuverability = Regexp.Maneuverabilities.Match(entry.Value);
					Match armor = Regexp.Armors.Match(entry.Value);
					Match manufacturer = Regexp.Manufacturers.Match(entry.Value);
					Match description = Regexp.Descriptions.Match(entry.Value);
					Match techDescription = Regexp.TechDescriptions.Match(entry.Value);
					// ignore the length of thrusters
					Match length = Regexp.Lengths.Match(entry.Value.Split("$Thruster")[0]);
					Match flags = Regexp.Flags.Match(entry.Value);
					IEnumerable<Match> subsystems = Regexp.Subsystems.Matches(entry.Value);

					EShip ship;

					if (Ships.Exists(s => s.Name == name.Value.Trim()))
					{
						ship = Ships.First(s => s.Name == name.Value.Trim());
					}
					else
					{
						ship = new EShip()
						{
							Name = name.Value.Trim()
						};

						Ships.Add(ship);
					}

					if (altName.Success && ship.AltName == null)
					{
						ship.AltName = XstrManager.GetValueWithoutXstr(altName.Value);
					}

					if (type.Success && ship.Type == null)
					{
						ship.Type = XstrManager.GetValueWithoutXstr(type.Value);
					}

					if (maneuverability.Success && ship.Maneuverability == null)
					{
						ship.Maneuverability = XstrManager.GetValueWithoutXstr(maneuverability.Value);
					}

					if (armor.Success && ship.Armor == null)
					{
						ship.Armor = XstrManager.GetValueWithoutXstr(armor.Value);
					}

					if (manufacturer.Success && ship.Manufacturer == null)
					{
						ship.Manufacturer = XstrManager.GetValueWithoutXstr(manufacturer.Value);
					}

					if (description.Success && ship.Description == null)
					{
						ship.Description = XstrManager.GetValueWithoutXstr(description.Value);
					}

					if (techDescription.Success && ship.TechDescription == null)
					{
						ship.TechDescription = XstrManager.GetValueWithoutXstr(techDescription.Value);
					}

					if (length.Success && ship.Length == null)
					{
						ship.Length = XstrManager.GetValueWithoutXstr(length.Value);
					}

					if (flags.Success && flags.Value.Contains("player_ship"))
					{
						ship.IsPlayerShip = true;
					}

					foreach (Match subsystem in subsystems)
					{
						// some subsystems are not visible so don't translate them
						if (!subsystem.Value.Contains("untargetable") && !subsystem.Value.Contains("afterburner"))
						{
							Match subsystemName = Regexp.SubsystemNamesAndCoordinates.Match(subsystem.Value);
							Match altsubsystemName = Regexp.AltSubsystemNames.Match(subsystem.Value);
							Match altDamagePopupSubsystemName = Regexp.AltDamagePopupSubsystemNames.Match(subsystem.Value);
							ESubsystem eSubsystem;

							if (ship.Subsystems.Exists(s => s.SubsystemName.ToLower() == subsystemName.Value.Split(',')[0].Trim().ToLower()))
							{
								eSubsystem = ship.Subsystems.First(s => s.SubsystemName.ToLower() == subsystemName.Value.Split(',')[0].Trim().ToLower());
							}
							else
							{
								string[] subsystemParts = subsystemName.Value.Split(',');

								eSubsystem = new ESubsystem()
								{
									SubsystemName = subsystemParts[0].Trim()
								};

								if (subsystemParts.Length > 1)
								{
									eSubsystem.HP = subsystemParts[1].Trim();
								}
								if (subsystemParts.Length > 2)
								{
									eSubsystem.DegreeTurn = subsystemParts[2].Trim();
								}

								ship.Subsystems.Add(eSubsystem);
							}

							if (altsubsystemName.Success && eSubsystem.AltSubsystemName == null)
							{
								eSubsystem.AltSubsystemName = XstrManager.GetValueWithoutXstr(altsubsystemName.Value);
							}

							if (altDamagePopupSubsystemName.Success && eSubsystem.AltDamagePopupSubsystemName == null)
							{
								eSubsystem.AltDamagePopupSubsystemName = XstrManager.GetValueWithoutXstr(altDamagePopupSubsystemName.Value);
							}

							if (subsystem.Value.Contains("$Default PBanks:"))
							{
								string defaultPBank = Regexp.DefaultPBanks.Match(subsystem.Value).Value;
								EWeapon defaultWeapon = ModWeapons.FirstOrDefault(w => w.Name.TrimStart('@') == defaultPBank || w.Name.TrimStart('@').ToUpper() == defaultPBank.ToUpper());

								if (defaultWeapon != null)
								{
									if (defaultWeapon.TurretName != null)
									{
										eSubsystem.TurretNameFromDefaultBank = defaultWeapon.TurretName;
									}
									else if (defaultWeapon.Type != null)
									{
										eSubsystem.TurretTypeFromDefaultBank = defaultWeapon.Type;
									}
									else
									{
										eSubsystem.TurretTypeFromDefaultBank = "Laser turret";
									}
								}
								else
								{
									eSubsystem.TurretTypeFromDefaultBank = "Laser turret";
								}
							}
							else if (subsystem.Value.Contains("$Default SBanks:"))
							{
								eSubsystem.IsMissileLauncher = true;

								string defaultSBank = Regexp.DefaultPBanks.Match(subsystem.Value).Value;
								EWeapon defaultWeapon = ModWeapons.FirstOrDefault(w => w.Name.TrimStart('@') == defaultSBank || w.Name.TrimStart('@').ToUpper() == defaultSBank.ToUpper());

								if (defaultWeapon != null)
								{
									if (defaultWeapon.TurretName != null)
									{
										eSubsystem.TurretNameFromDefaultBank = defaultWeapon.TurretName;
									}
								}
							}

							if (subsystem.Value.Contains("$Turret Reset Delay:") && eSubsystem.TurretTypeFromDefaultBank == null)
							{
								eSubsystem.TurretTypeFromDefaultBank = "Laser turret";
							}

							if (subsystem.Value.Contains("$Default PBanks:") || subsystem.Value.Contains("$Default SBanks:") || subsystem.Value.Contains("$Turret Reset Delay:"))
							{
								eSubsystem.IsTurret = true;
							}
						}
					}
				}
			}
		}

		public string GetContent()
		{
			StringBuilder content = new();

			if (Ships.Count > 0)
			{
				content.Append($"#Ship Classes{Environment.NewLine}");

				foreach (EShip ship in Ships)
				{
					ship.AltName ??= XstrManager.GetValueWithoutXstr(ship.Name);

					content.Append($"{Environment.NewLine}$Name: {ship.Name}{Environment.NewLine}+nocreate{Environment.NewLine}");
					content.Append($"$Alt Name: XSTR(\"{ship.AltName}\", -1){Environment.NewLine}");

					if (ship.Type != null)
					{
						content.Append($"+Type: XSTR(\"{ship.Type}\", -1){Environment.NewLine}");
					}

					if (ship.Maneuverability != null)
					{
						content.Append($"+Maneuverability: XSTR(\"{ship.Maneuverability}\", -1){Environment.NewLine}");
					}

					if (ship.Armor != null)
					{
						content.Append($"+Armor: XSTR(\"{ship.Armor}\", -1){Environment.NewLine}");
					}

					if (ship.Manufacturer != null)
					{
						content.Append($"+Manufacturer: XSTR(\"{ship.Manufacturer}\", -1){Environment.NewLine}");
					}

					if (ship.Description != null)
					{
						content.Append($"+Description: XSTR(\"{ship.Description}\", -1){Environment.NewLine}$end_multi_text{Environment.NewLine}");
					}

					if (ship.TechDescription != null)
					{
						content.Append($"+Tech Description: XSTR(\"{ship.TechDescription}\", -1){Environment.NewLine}$end_multi_text{Environment.NewLine}");
					}

					if (ship.Length != null)
					{
						content.Append($"+Length: XSTR(\"{ship.Length}\", -1){Environment.NewLine}");
					}

					foreach (ESubsystem subsystem in ship.Subsystems)
					{
						StringBuilder stringBuilderSubsystem = new();

						if (subsystem.AltSubsystemName != null)
						{
							stringBuilderSubsystem.Append($"$Alt Subsystem Name: XSTR(\"{subsystem.AltSubsystemName}\", -1){Environment.NewLine}");
						}
						else if (!subsystem.IsTurret)
						{
							stringBuilderSubsystem.Append($"$Alt Subsystem Name: XSTR(\"{subsystem.SubsystemName.Split(',')[0]}\", -1){Environment.NewLine}");
						}

						if (ship.IsPlayerShip)
						{
							if (subsystem.AltDamagePopupSubsystemName != null)
							{
								stringBuilderSubsystem.Append($"$Alt Damage Popup Subsystem Name: XSTR(\"{subsystem.AltDamagePopupSubsystemName}\", -1){Environment.NewLine}");
							}
							// if there is a turret name but no damage popup name, then copy the turret name to damage popup name
							else if (subsystem.TurretNameFromDefaultBank != null)
							{
								stringBuilderSubsystem.Append($"$Alt Damage Popup Subsystem Name: XSTR(\"{subsystem.TurretNameFromDefaultBank}\", -1){Environment.NewLine}");
							}
							// if there is neither alt name nor alt damage popup, then check if this is missile launcher (SBanks key word) to set a custom alt name
							else if (subsystem.IsMissileLauncher)
							{
								stringBuilderSubsystem.Append($"$Alt Damage Popup Subsystem Name: XSTR(\"Missile lnchr\", -1){Environment.NewLine}");
							}
							// if there is neither alt name nor alt damage popup, then check if this is a gun turret ("PBanks" or "$Turret Reset Delay" key words) to set a custom alt name
							else if (subsystem.TurretTypeFromDefaultBank != null)
							{
								stringBuilderSubsystem.Append($"$Alt Damage Popup Subsystem Name: XSTR(\"{subsystem.TurretTypeFromDefaultBank}\", -1){Environment.NewLine}");
							}
							else
							{
								stringBuilderSubsystem.Append($"$Alt Damage Popup Subsystem Name: XSTR(\"{subsystem.SubsystemName.Split(',')[0]}\", -1){Environment.NewLine}");
							}
						}

						// only add the subsystem if there is something inside
						if (stringBuilderSubsystem.Length > 0)
						{
							content.Append($"$Subsystem: {subsystem.SubsystemName}");

							if (subsystem.HP != null)
							{
								content.Append($", {subsystem.HP}");
							}
							if (subsystem.DegreeTurn != null)
							{
								content.Append($", {subsystem.DegreeTurn}");
							}

							content.Append($"{Environment.NewLine}");
							content.Append(stringBuilderSubsystem);
						}
					}
				}

				content.Append($"{Environment.NewLine}#End");
			}

			return content.ToString();
		}
	}
}
