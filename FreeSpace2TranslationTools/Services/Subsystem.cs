namespace FreeSpace2TranslationTools.Services
{
    public class Subsystem
    {
        public string Name { get; set; }
        public string AltSubsystemName { get; set; }
        public string AltDamagePopupSubsystemName { get; set; }

        public Subsystem()
        {
            Name = "";
            AltSubsystemName = "";
            AltDamagePopupSubsystemName = "";
        }
    }
}
