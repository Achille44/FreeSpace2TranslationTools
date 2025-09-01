namespace FreeSpace2TranslationTools.Services
{
    public class Weapon(string name, string type, bool hasTurretName = false, string turretName = null)
	{
		public string Name { get; set; } = name;
		public string Type { get; set; } = type;
		public bool HasTurretName { get; set; } = hasTurretName;
		public string TurretName { get; set; } = turretName;
	}
}
