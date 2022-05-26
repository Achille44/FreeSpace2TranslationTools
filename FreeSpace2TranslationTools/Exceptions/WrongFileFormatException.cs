using System;
using System.Runtime.Serialization;

namespace FreeSpace2TranslationTools.Exceptions
{
    [Serializable]
    internal class WrongFileFormatException : Exception
    {
        public WrongFileFormatException()
        {
        }

        public WrongFileFormatException(string message) : base(message)
        {
        }

        public WrongFileFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongFileFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}