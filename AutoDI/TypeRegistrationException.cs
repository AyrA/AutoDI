using System;

namespace AyrA.AutoDI
{
    [Serializable]
    internal class TypeRegistrationException : Exception
    {
        public TypeRegistrationException() : this("Unknown type registration exception")
        {
        }

        public TypeRegistrationException(string? message) : base(message)
        {
        }

        public TypeRegistrationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}