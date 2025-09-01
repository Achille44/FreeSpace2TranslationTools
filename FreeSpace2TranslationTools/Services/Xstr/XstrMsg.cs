using System.IO;

namespace FreeSpace2TranslationTools.Services.Xstr
{
    internal class XstrMsg(int id, string text, FileInfo file, string fullLine) : IXstr
    {
		public int Id { get; set; } = id;
		public string Text { get; set; } = text;
		public string FileName { get; set; } = file.Name;
		public string FilePath { get; set; } = file.FullName;
		public string FullLine { get; set; } = fullLine;
		public bool Treated { get; set; } = false;
		public bool Replaceable { get; set; } = false;
		public bool UniqueId { get; set; } = false;
		public string Comments { get; set; } = "";

		public string ReplaceContentWithNewXstrId(string content)
        {
            string newLine = Regexp.StringAndId.Replace(FullLine, match => $"{match.Groups[1].Value}{Id}");

            return content.Replace(FullLine, newLine);
        }
    }
}
