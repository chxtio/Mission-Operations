﻿using System.Text;

namespace SignalRApp
{
    internal static class StringBuilderExtension
    {
        public static void AppendLineIfNotNull(this StringBuilder stringBuilder, string value)
        {
            if (value != null)
            {
                stringBuilder.AppendLine(value);
            }
        }
    }
}
