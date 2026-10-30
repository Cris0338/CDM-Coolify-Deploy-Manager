// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
// UI translations are embedded JSON resources. English is the canonical fallback.
using System.Globalization;
using System.Text.Json;
using System.Windows;

namespace CoolifyDeployManager;

internal static class Loc
{
    public static readonly IReadOnlyList<(string Code, string NativeName)> Languages =
        new (string, string)[]
        {
            ("system", "System (automatic)"),
            ("en", "English"),
            ("fr", "Français"),
            ("it", "Italiano"),
            ("es", "Español · preview"),
            ("de", "Deutsch · preview"),
            ("pt-BR", "Português (Brasil) · preview"),
            ("nl", "Nederlands · preview"),
            ("pl", "Polski · preview"),
            ("tr", "Türkçe · preview"),
            ("ru", "Русский · preview"),
            ("uk", "Українська · preview"),
            ("ja", "日本語 · preview"),
            ("ko", "한국어 · preview"),
            ("zh-CN", "简体中文 · preview"),
        };

    private static readonly Dictionary<string, string> English = LoadJson("en");
    private static readonly Dictionary<string, Dictionary<string, string>> Cache =
        new(StringComparer.OrdinalIgnoreCase) { ["en"] = English };

    public static string Language { get; private set; } = "en";

    private static Dictionary<string, string> LoadJson(string code)
    {
        var resourceName = "CoolifyDeployManager.Localization." + code + ".json";
        using var stream = typeof(Loc).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Missing embedded locale: " + resourceName);
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException("Empty locale: " + code);
        return new Dictionary<string, string>(values, StringComparer.Ordinal);
    }

    public static string Resolve(string? preference)
    {
        var input = string.IsNullOrWhiteSpace(preference) || string.Equals(preference, "system", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.CurrentUICulture.Name : preference.Trim();
        var lang = input.Replace('_', '-').ToLowerInvariant();
        if (lang.StartsWith("pt")) return "pt-BR";
        if (lang.StartsWith("zh")) return "zh-CN";
        foreach (var (code, _) in Languages)
            if (code != "system" && (lang == code.ToLowerInvariant() || lang.StartsWith(code.ToLowerInvariant() + "-")))
                return code;
        var prefix = lang.Split('-')[0];
        foreach (var (code, _) in Languages)
            if (code == prefix) return code;
        return "en";
    }

    public static void Apply(string? preference)
    {
        Language = Resolve(preference);
        if (!Cache.TryGetValue(Language, out var selected))
        {
            selected = LoadJson(Language);
            Cache[Language] = selected;
        }
        if (Application.Current is not { } app) return;
        foreach (var (key, english) in English)
            app.Resources["L." + key] = selected.TryGetValue(key, out var localized) && !string.IsNullOrWhiteSpace(localized)
                ? localized : english;
    }

    public static string T(string key)
    {
        if (Application.Current?.Resources["L." + key] is string localized) return localized;
        return English.TryGetValue(key, out var english) ? english : key;
    }

    public static string F(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);
}
