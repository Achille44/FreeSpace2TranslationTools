using System.IO;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class XstrModifyVariable : IXstr
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FullLine { get; set; }
        public bool Treated { get; set; } = false;
		public bool Replaceable { get; set; } = false;
		public bool UniqueId { get; set; } = false;
        public string Comments { get; set; } = "";

        public XstrModifyVariable(int id, string text, FileInfo file, string fullLine)
        {
            Id = id;
            Text = text;
            FileName = file.Name;
            FilePath = file.FullName;
            FullLine = fullLine;
		}

		public string ReplaceContentWithNewXstrId(string content)
        {
            string newLine = Regexp.ModifyVariableXstr.Replace(FullLine, match => $"{match.Groups[1].Value}{Id}{match.Groups[4].Value}");

            return content.Replace(FullLine, newLine);
        }
    }
}
