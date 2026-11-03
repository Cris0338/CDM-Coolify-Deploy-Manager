// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// Coolify GET /api/v1/deployments/{uuid} includes "logs" as a JSON-encoded
// array (or, on older servers, plain text). The status poll already fetches it.
// This class is pure: no network, no terminal, no SSH and no personal settings.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoolifyDeployManager;

internal sealed class DeploymentLogFollower
{
    private List<string> lastVisibleLines = new();

    internal IReadOnlyList<string> NewLines(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        var current = Parse(raw);
        if (current.Count == 0) return Array.Empty<string>();

        int same = 0;
        while (same < current.Count && same < lastVisibleLines.Count &&
               string.Equals(current[same], lastVisibleLines[same], StringComparison.Ordinal))
            same++;

        // Coolify typically appends log entries. If its newest entry grows,
        // the previously displayed complete lines remain the common prefix.
        // If an old log is rewritten or rotated, don't repeat the whole prior
        // output: emit only the changed tail.
        int start = same;
        if (same < lastVisibleLines.Count && same < current.Count &&
            current.Count <= lastVisibleLines.Count &&
            current[same].StartsWith(lastVisibleLines[same], StringComparison.Ordinal))
        {
            var growth = current[same][lastVisibleLines[same].Length..];
            var result = new List<string>();
            if (growth.Length > 0) result.Add(growth.TrimStart('\n'));
            for (int i = same + 1; i < current.Count; i++) result.Add(current[i]);
            lastVisibleLines = current;
            return result.Where(x => x.Length > 0).ToArray();
        }

        lastVisibleLines = current;
        if (same < lastVisibleLines.Count && same < current.Count &&
            same == 0 && lastVisibleLines.Count <= 1)
            return current;

        return current.Skip(start).ToArray();
    }

    internal static List<string> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return CleanLines(raw);

            var entries = new List<(int Batch, int Order, int Index, string Output, string Command)>();
            int index = 0;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) { index++; continue; }
                if (item.TryGetProperty("hidden", out var hidden) &&
                    hidden.ValueKind == JsonValueKind.True) { index++; continue; }
                var output = item.Str("output");
                var command = item.Str("command");
                int batch = TryNumber(item, "batch", 0);
                int order = TryNumber(item, "order", index);
                entries.Add((batch, order, index, output, command));
                index++;
            }

            var visible = new List<string>();
            foreach (var entry in entries.OrderBy(e => e.Batch)
                         .ThenBy(e => e.Order).ThenBy(e => e.Index))
            {
                if (!string.IsNullOrWhiteSpace(entry.Command))
                    visible.AddRange(CleanLines("$ " + entry.Command));
                visible.AddRange(CleanLines(entry.Output));
            }
            return visible;
        }
        catch (JsonException)
        {
            // Some older Coolify instances supply plain text rather than JSON.
            return CleanLines(raw);
        }
    }

    private static int TryNumber(JsonElement item, string key, int fallback)
    {
        if (!item.TryGetProperty(key, out var value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n))
            return n;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out n))
            return n;
        return fallback;
    }

    private static List<string> CleanLines(string raw)
    {
        // Remove ANSI terminal escape sequences before writing to the WPF log.
        // Never display Coolify's "hidden" entries; they can include internal
        // deployment commands the server UI hides by default.
        var text = Regex.Replace(raw, "\u001b\\[[0-?]*[ -/]*[@-~]", "");
        return text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None)
            .Where(line => line.Length > 0).ToList();
    }

    internal static void VerifyForSmoke()
    {
        var reader = new DeploymentLogFollower();
        var first = """[{"output":"Preparing build","type":"stdout","hidden":false,"batch":1,"order":1},{"output":"SECRET INTERNAL COMMAND","hidden":true,"batch":1,"order":2},{"output":"Pulling source","type":"stdout","hidden":false,"batch":1,"order":3}]""";
        var second = """[{"output":"Preparing build","hidden":false,"batch":1,"order":1},{"output":"SECRET INTERNAL COMMAND","hidden":true,"batch":1,"order":2},{"output":"Pulling source","hidden":false,"batch":1,"order":3},{"output":"Building image\nStep 1/3","hidden":false,"batch":2,"order":1}]""";
        var third = """[{"output":"Preparing build","hidden":false,"batch":1,"order":1},{"output":"SECRET INTERNAL COMMAND","hidden":true,"batch":1,"order":2},{"output":"Pulling source","hidden":false,"batch":1,"order":3},{"output":"Building image\nStep 1/3\nStep 2/3","hidden":false,"batch":2,"order":1}]""";
        var start = reader.NewLines(first);
        if (start.Count != 2 || start[0] != "Preparing build" ||
            start[1] != "Pulling source" || reader.NewLines(first).Count != 0)
            throw new InvalidOperationException("Initial or repeated deployment logs were duplicated.");
        var next = reader.NewLines(second);
        if (next.Count != 2 || next[0] != "Building image" ||
            next[1] != "Step 1/3" || reader.NewLines(second).Count != 0)
            throw new InvalidOperationException("New deployment lines were not incremental.");
        var growth = reader.NewLines(third);
        if (growth.Count != 1 || growth[0] != "Step 2/3")
            throw new InvalidOperationException("Growing Coolify log entry was repeated.");
        if (reader.NewLines(third).Count != 0)
            throw new InvalidOperationException("Identical Coolify response repeated log output.");
        var plain = new DeploymentLogFollower();
        var fallback = plain.NewLines("Build started\nBuild finished");
        if (fallback.Count != 2 || plain.NewLines("Build started\nBuild finished").Count != 0)
            throw new InvalidOperationException("Legacy plain-text logs are not incremental.");
    }
}
