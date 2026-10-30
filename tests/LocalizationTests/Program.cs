// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Text.Json;
using System.Text.RegularExpressions;

string directory = Path.Combine(AppContext.BaseDirectory, "Localization");
if (!Directory.Exists(directory)) throw new Exception("Missing localization output directory.");

var files = Directory.GetFiles(directory, "*.json");
var expected = new HashSet<string>(StringComparer.Ordinal)
{
    "en", "fr", "it", "es", "de", "pt-BR", "nl", "pl", "tr", "ru", "uk", "ja", "ko", "zh-CN"
};
var actual = files.Select(path => Path.GetFileNameWithoutExtension(path)).ToHashSet(StringComparer.Ordinal);
if (!expected.SetEquals(actual))
    throw new Exception("Locale mismatch. Missing: " + string.Join(", ", expected.Except(actual)) +
                        "; unexpected: " + string.Join(", ", actual.Except(expected)));

static Dictionary<string, string> Read(string path)
{
    var text = File.ReadAllText(path);
    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(text)
        ?? throw new Exception("Empty catalog: " + path);
    if (map.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value)))
        throw new Exception("Blank key/value in: " + path);
    return map;
}

static string[] Placeholders(string text) =>
    Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).OrderBy(x => x).ToArray();

var english = Read(Path.Combine(directory, "en.json"));
if (english.Count < 130) throw new Exception("Unexpectedly small English source catalog.");
int errors = 0;
foreach (var file in files.OrderBy(Path.GetFileName))
{
    var lang = Path.GetFileNameWithoutExtension(file);
    var catalog = Read(file);
    foreach (var (key, value) in catalog)
    {
        if (!english.TryGetValue(key, out var source))
        {
            Console.Error.WriteLine($"{lang}: unknown translation key '{key}'");
            errors++;
            continue;
        }
        if (!Placeholders(source).SequenceEqual(Placeholders(value)))
        {
            Console.Error.WriteLine($"{lang}: message placeholders mismatch for '{key}'");
            errors++;
        }
    }
    if (lang is "en" or "fr" or "it")
    {
        foreach (var key in english.Keys.Except(catalog.Keys))
        {
            Console.Error.WriteLine($"{lang}: missing required key '{key}'");
            errors++;
        }
    }
    Console.WriteLine($"{lang}: {catalog.Count}/{english.Count} keys ({(catalog.Count == english.Count ? "complete" : "preview / English fallback")})");
}
if (errors > 0) throw new Exception($"Localization validation failed with {errors} error(s).");
Console.WriteLine("PASS: all localization catalogs, known keys and format placeholders validated.");
