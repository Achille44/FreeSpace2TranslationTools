using System;
using System.Runtime.Serialization;

namespace FreeSpace2TranslationTools.Exceptions
{
    internal class UserFriendlyException : Exception
    {
        public UserFriendlyException()
        {
        }

        public UserFriendlyException(string message) : base(message)
        {
        }

        public UserFriendlyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}