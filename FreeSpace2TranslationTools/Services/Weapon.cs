namespace FreeSpace2TranslationTools.Services
{
    public class Weapon
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool HasTurretName { get; set; }

        public Weapon(string name, string type, bool hasTurretName = false)
        {
            Name = name;
            Type = type;
            HasTurretName = hasTurretName;
        }
    }
}
