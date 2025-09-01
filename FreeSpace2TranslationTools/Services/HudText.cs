namespace FreeSpace2TranslationTools.Services
{
    public class HudText(string name, string beginningOfSexp, string defaultValue, string endOfSexp) : MissionVariable(name, defaultValue)
    {
		public string BeginningOfSexp { get; set; } = beginningOfSexp;
		public string EndOfSexp { get; set; } = endOfSexp;
		public new string NewSexp => "@" + Name + "[" + DefaultValue + "]";
	}
}
