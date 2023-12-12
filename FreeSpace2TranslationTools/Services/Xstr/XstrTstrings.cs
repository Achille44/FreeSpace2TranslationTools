using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
	internal class XstrTstrings : IXstr
	{
		public int Id { get; set; }
		public string Text { get; set; }
		public string FileName { get; set; }
		public string FilePath { get; set; }
		public string FullLine { get; set; }
		public bool Treated { get; set; } = false;
		public bool Replaceable { get; set; } = true;
		public bool UniqueId { get; set; } = false;

		public XstrTstrings(int id, string text, FileInfo file, string fullLine)
		{
			Id = id;
			Text = text;
			FileName = file.Name;
			FilePath = file.FullName;
			FullLine = fullLine;
		}

		public string ReplaceContentWithNewXstrId(string content)
		{
			return content;
		}
	}
}
