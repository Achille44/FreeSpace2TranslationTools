namespace FreeSpace2TranslationTools.Services
{
    internal interface IXstr
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FullLine { get; set; }
        public bool Treated { get; set; }
		public bool Replaceable { get; set; }
        public bool UniqueId { get; set; }
        public string Comments { get; set; }

        public string ReplaceContentWithNewXstrId(string content);
    }
}
