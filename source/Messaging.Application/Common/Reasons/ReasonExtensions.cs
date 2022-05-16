using System;

namespace Messaging.Application.Common.Reasons;

public static class ReasonExtensions
{
    public static string EnumToString(this ReasonLanguage lang)
    {
        return Enum.GetName(typeof(ReasonLanguage), lang) ?? string.Empty;
    }
}
