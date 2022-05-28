namespace FreeSpace2TranslationTools.Services
{
    public class Weapon
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public Weapon(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
