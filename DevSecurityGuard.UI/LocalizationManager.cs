using System.Globalization;
using System.Windows;

namespace DevSecurityGuard.UI;

/// <summary>
/// Localization manager for multi-language support
/// </summary>
public static class LocalizationManager
{
    private static ResourceDictionary? _currentLanguage;
    
    public static string CurrentLanguageCode { get; private set; } = "en";

    public static void SetLanguage(string languageCode)
    {
        CurrentLanguageCode = languageCode;
        
        var dict = new ResourceDictionary();
        
        switch (languageCode)
        {
            case "es":
                dict.Source = new Uri("Resources/Strings.es.xaml", UriKind.Relative);
                break;
            case "en":
            default:
                dict.Source = new Uri("Resources/Strings.en.xaml", UriKind.Relative);
                break;
        }

        // Remove old language dictionary
        if (_currentLanguage != null)
        {
            System.Windows.Application.Current.Resources.MergedDictionaries.Remove(_currentLanguage);
        }

        // Add new language dictionary
        System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
        _currentLanguage = dict;

        // Set culture
        Thread.CurrentThread.CurrentCulture = new CultureInfo(languageCode);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode);
    }

    public static string GetString(string key)
    {
        if (System.Windows.Application.Current.Resources.Contains(key))
        {
            return System.Windows.Application.Current.Resources[key]?.ToString() ?? key;
        }
        return key;
    }
}
