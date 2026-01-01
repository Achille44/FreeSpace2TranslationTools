using System;
using System.IO;

namespace FreeSpace2TranslationTools.Services.Xstr
{
	internal class XstrTstrings: IXstr
	{
		public int Id { get; set; }
		public string Text { get; set; }
		public string FileName { get; set; } = "";
		public string FilePath { get; set; } = "";
		public string FullLine { get; set; }
		public bool Treated { get; set; } = false;
		public bool Replaceable { get; set; } = true;
		public bool UniqueId { get; set; } = false;
		public string Comments { get; set; } = "";

		public XstrTstrings(int id, string text, FileInfo file, string fullLine, string endOfLine)
		{
			Id = id;
			Text = text;
			FileName = file.Name;
			FilePath = file.FullName;
			FullLine = fullLine;
			SetComments(endOfLine);
		}

		public string ReplaceContentWithNewXstrId(string content)
		{
			return content;
		}

		private void SetComments(string text)
		{
			string[] comments = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (comments.Length > 0)
			{
				Comments = " ;" + comments[0];

				if (comments[0].Contains(Constants.UNIQUE_ID))
				{
					UniqueId = true;
				}
			}
		}
	}
}
