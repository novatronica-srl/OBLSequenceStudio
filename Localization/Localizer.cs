using System.Globalization;
using System.Text.Json;

namespace OBLSequenceStudio.Localization;

/// <summary>Loads embedded translations and manages the selected language, formatting culture and persisted preference.</summary>
public static class Localizer
{
    /// <summary>Language option displayed by its native name in the selector.</summary>
    /// <param name="Code">Supported language code used to resolve translations.</param>
    /// <param name="Name">Native display name.</param>
    public sealed record Language(string Code, string Name)
    {
        /// <summary>Returns the native display name used by selection controls.</summary>
        public override string ToString() => Name;
    }

    /// <summary>Supported languages in the same order as translation arrays in Strings.json.</summary>
    public static IReadOnlyList<Language> Languages { get; } = new[]
    {
        new Language("it", "Italiano"), new Language("en", "English"), new Language("es", "Español"),
        new Language("de", "Deutsch"), new Language("fr", "Français")
    };

    private static readonly Dictionary<string, string[]> Strings = LoadStrings();
    /// <summary>Currently selected supported language code.</summary>
    public static string Code { get; private set; } = "it";
    /// <summary>Culture used for localized formatting in the editor.</summary>
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("it-IT");
    /// <summary>Resource keys available in the embedded translation dictionary.</summary>
    public static IEnumerable<string> Keys => Strings.Keys;
    private static string PreferencePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OBLSequenceStudio", "language.txt");

    private static Dictionary<string, string[]> LoadStrings()
    {
        var assembly = typeof(Localizer).Assembly;
        const string resourceName = "OBLSequenceStudio.Localization.Strings.json";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found. Available: {available}");
        }
        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream)
               ?? throw new JsonException("Failed to deserialize localization strings.");
    }

    /// <summary>Formats the selected translation with the current formatting culture.</summary>
    /// <param name="key">Existing key in Strings.json.</param>
    /// <param name="args">Arguments matching the translation placeholders.</param>
    /// <returns>Formatted text in the selected language.</returns>
    public static string Text(string key, params object[] args)
    {
        int index = Languages.ToList().FindIndex(language => language.Code == Code);
        return string.Format(Culture, Strings[key][index], args);
    }

    /// <summary>Loads the saved language or operating-system preference, falling back to English when unsupported.</summary>
    public static void Initialize()
    {
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        try { if (File.Exists(PreferencePath)) code = File.ReadAllText(PreferencePath).Trim(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { System.Diagnostics.Debug.WriteLine(ex); }
        SetLanguage(Languages.Any(l => l.Code == code) ? code : "en");
    }

    /// <summary>Selects a supported language and updates the current thread formatting and UI cultures.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The language code is unsupported.</exception>
    public static void SetLanguage(string code)
    {
        if (!Languages.Any(l => l.Code == code)) throw new ArgumentOutOfRangeException(nameof(code));
        Code = code;
        Culture = CultureInfo.GetCultureInfo(code switch { "it" => "it-IT", "en" => "en-GB", "es" => "es-ES", "de" => "de-DE", _ => "fr-FR" });
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }

    /// <summary>Persists the selected language under the current user local application data directory.</summary>
    /// <returns>False when I/O or access permissions prevent saving; otherwise true.</returns>
    public static bool SavePreference()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
            File.WriteAllText(PreferencePath, Code);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return false;
        }
    }

    /// <summary>Logs an exception to Debug and maps it to an application-facing localized file or validation message.</summary>
    public static string Error(Exception exception, string filename = "")
    {
        System.Diagnostics.Debug.WriteLine(exception);
        return exception switch
        {
            JsonException => Text("InvalidJson"),
            System.Xml.XmlException => Text("InvalidProgrammerConfig"),
            InvalidDataException or InvalidOperationException => exception.Message,
            IOException or UnauthorizedAccessException => Text("FileError", filename),
            _ => Text("UnexpectedError")
        };
    }
}
