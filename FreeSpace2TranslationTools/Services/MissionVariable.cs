using System;

namespace FreeSpace2TranslationTools.Services
{
    public class MissionVariable(string name, string defaultValue, bool original = false)
	{
		public string Name { get; set; } = name;
		public string DefaultValue { get; set; } = defaultValue;
		public string NewSexp => "\"@" + Name + "[" + DefaultValue + "]\"";
		public bool Original { get; set; } = original;

		/// <summary>
		/// returns the modify-variable-xstr sexp
		/// </summary>
		/// <returns></returns>
		public string ModifyVariableXstr()
        {
            string result = $"   ( modify-variable-xstr {Environment.NewLine}"
                + $"      \"@{Name}[{DefaultValue}]\" {Environment.NewLine}"
                + $"      \"{DefaultValue}\" {Environment.NewLine}"
                + $"      -1 {Environment.NewLine}"
                + $"   ){Environment.NewLine}";

            return result;
        }
    }
}
