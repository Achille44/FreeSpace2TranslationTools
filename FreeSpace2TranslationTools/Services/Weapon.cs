namespace FreeSpace2TranslationTools.Services
{
    public class Weapon
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool HasTurretName { get; set; }
        public string TurretName { get; set; }

        public Weapon(string name, string type, bool hasTurretName = false, string turretName = null)
        {
            Name = name;
            Type = type;
            HasTurretName = hasTurretName;
            TurretName = turretName;
        }
    }
}
