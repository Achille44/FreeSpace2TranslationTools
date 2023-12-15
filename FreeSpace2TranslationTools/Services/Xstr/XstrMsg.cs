using System.IO;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class XstrMsg : IXstr
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

		public XstrMsg(int id, string text, FileInfo file, string fullLine)
        {
            Id = id;
            Text = text;
            FileName = file.Name;
            FilePath = file.FullName;
            FullLine = fullLine;
        }

        public string ReplaceContentWithNewXstrId(string content)
        {
            string newLine = Regexp.StringAndId.Replace(FullLine, match => $"{match.Groups[1].Value}{Id}");

            return content.Replace(FullLine, newLine);
        }
    }
}
