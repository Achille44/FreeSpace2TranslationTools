using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Exceptions
{
    [Serializable]
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

        protected FileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public FileException(Exception innerException, string file) : this(innerException.Message, innerException)
        {
            File = file;
        }
    }
}
