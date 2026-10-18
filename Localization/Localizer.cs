using System.Globalization;
using System.Text.Json;

namespace OBLSequenceStudio.Localization;

public static class Localizer
{
    public sealed record Language(string Code, string Name)
    {
        public override string ToString() => Name;
    }

    public static IReadOnlyList<Language> Languages { get; } = new[]
    {
        new Language("it", "Italiano"), new Language("en", "English"), new Language("es", "Español"),
        new Language("de", "Deutsch"), new Language("fr", "Français")
    };

    private static readonly Dictionary<string, string[]> Strings = LoadStrings();
    public static string Code { get; private set; } = "it";
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("it-IT");
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

    public static string Text(string key, params object[] args)
    {
        int index = Languages.ToList().FindIndex(language => language.Code == Code);
        return string.Format(Culture, Strings[key][index], args);
    }

    public static void Initialize()
    {
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        try { if (File.Exists(PreferencePath)) code = File.ReadAllText(PreferencePath).Trim(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { System.Diagnostics.Debug.WriteLine(ex); }
        SetLanguage(Languages.Any(l => l.Code == code) ? code : "en");
    }

    public static void SetLanguage(string code)
    {
        if (!Languages.Any(l => l.Code == code)) throw new ArgumentOutOfRangeException(nameof(code));
        Code = code;
        Culture = CultureInfo.GetCultureInfo(code switch { "it" => "it-IT", "en" => "en-GB", "es" => "es-ES", "de" => "de-DE", _ => "fr-FR" });
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }

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
