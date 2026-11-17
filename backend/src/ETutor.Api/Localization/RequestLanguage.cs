using System.Globalization;
using ETutor.Application.Abstractions;
using ETutor.Domain.Common;

namespace ETutor.Api.Localization;

/// <summary>Reads the language chosen by the request-localization middleware (Accept-Language).</summary>
internal sealed class RequestLanguage : IRequestLanguage
{
    public string Code
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return LanguageCodes.IsSupported(code) ? code : LanguageCodes.Default;
        }
    }
}
