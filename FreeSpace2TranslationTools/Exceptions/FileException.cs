using System;
using System.Runtime.Serialization;

namespace FreeSpace2TranslationTools.Exceptions
{
    internal class FileException : Exception
    {
        public string File { get; set; }

        public FileException()
        {
        }

        public FileException(string message) : base(message)
        {
        }

        public FileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public FileException(Exception innerException, string file) : this(innerException.Message, innerException)
        {
            File = file;
        }
    }
}
