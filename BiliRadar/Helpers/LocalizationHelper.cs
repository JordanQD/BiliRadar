using System;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;

namespace BiliRadar.Helpers;

public static class LocalizationHelper
{
    public static string GetString(string key)
    {
        var value = ResourceLoader.GetForViewIndependentUse("Resources").GetString(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static string GetString(string key, string fallback)
    {
        var value = ResourceLoader.GetForViewIndependentUse("Resources").GetString(key);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    public static string Format(string key, params object[] args)
    {
        var format = ResourceLoader.GetForViewIndependentUse("Resources").GetString(key);
        return string.IsNullOrEmpty(format) ? key : string.Format(format, args);
    }

    public static void SetLanguage(string language)
    {
        if (string.Equals(language, Services.AppSettings.SystemLanguage, StringComparison.OrdinalIgnoreCase))
        {
            ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
            return;
        }

        var culture = CultureInfo.GetCultureInfo(language);
        ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }
}
