﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace LaunchVehicle
{
    [ExcludeFromCodeCoverage]

    public sealed class MessageDeserializationFailedException : Exception
    {
        public MessageDeserializationFailedException() { }
        public MessageDeserializationFailedException(string message) : base(message) { }
        public MessageDeserializationFailedException(string message, Exception inner) : base(message, inner) { }
    }
}
