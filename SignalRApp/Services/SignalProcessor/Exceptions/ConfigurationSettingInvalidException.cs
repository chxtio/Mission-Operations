using System;
using System.Diagnostics.CodeAnalysis;

namespace SignalRApp
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public sealed class ConfigurationSettingInvalidException : SignalProcessorTechnicalBaseException
    {
        public override string Reason => "Configuration Setting is Invalid";
        public ConfigurationSettingInvalidException() { }
        public ConfigurationSettingInvalidException(string message) : base(message) { }
        public ConfigurationSettingInvalidException(string message, Exception inner) : base(message, inner) { }
        private ConfigurationSettingInvalidException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
