using System;
using System.Runtime.Serialization;

namespace AyrA.AutoDI
{
    [Serializable]
    internal class TypeRegistrationException : Exception
    {
        public TypeRegistrationException()
        {
        }

        public TypeRegistrationException(string? message) : base(message)
        {
        }

        public TypeRegistrationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected TypeRegistrationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}