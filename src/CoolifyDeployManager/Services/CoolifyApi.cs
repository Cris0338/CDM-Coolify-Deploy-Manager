// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CoolifyDeployManager;

internal sealed class CoolifyApi : IDisposable
{
    private readonly HttpClient http;
    private readonly string readToken;
    private readonly string deployToken;

    public CoolifyApi(UserSettings settings)
    {
        var url = NormalizeUrl(settings.ServerUrl);
        readToken = settings.ReadToken.Trim();
        deployToken = settings.DeployToken.Trim();
        if (string.IsNullOrEmpty(readToken) || string.IsNullOrEmpty(deployToken))
            throw new ArgumentException("Configure both API tokens.");

        http = new HttpClient
        {
            BaseAddress = new Uri(url + "/api/v1/"),
            Timeout = TimeSpan.FromSeconds(35)
        };
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Direct HTTP is valid without a domain on loopback, LAN or a private overlay
    /// network (e.g. a VPN). Remote public HTTP requires the managed SSH tunnel.
    /// </summary>
    public static bool IsLocalHttpUri(Uri uri)
    {
        if (uri.IsLoopback) return true;
        var host = uri.DnsSafeHost;
        if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".home.arpa", StringComparison.OrdinalIgnoreCase) ||
            (!host.Contains('.') && !host.Contains(':')))
            return true;
        if (!IPAddress.TryParse(host, out var address)) return false;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            return bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168) ||
                (bytes[0] == 169 && bytes[1] == 254) ||
                (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127);
        }
        return address.IsIPv6LinkLocal || (bytes.Length == 16 && (bytes[0] & 0xFE) == 0xFC);
    }

    public static string NormalizeUrl(string raw)
    {
        raw = raw.Trim().TrimEnd('/');
        if (raw.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            raw = raw[..^7].TrimEnd('/');
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || !(uri.Scheme == Uri.UriSchemeHttps ||
                 (uri.Scheme == Uri.UriSchemeHttp && IsLocalHttpUri(uri))))
            throw new ArgumentException(
                "Enter a Coolify HTTPS URL or a private LAN/VPN HTTP address. " +
                "For remote public HTTP, select the managed SSH tunnel.");
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    public async Task<CoolifyInventory> GetInventoryAsync(CancellationToken ct)
    {
        // The /applications endpoint lists applications (not databases or infrastructure services).
        // /projects/{uuid}/environments maps IDs to projects and environments.
        using var projectsDoc = await GetJsonAsync("projects", readToken, ct);
        using var appsDoc = await GetJsonAsync("applications", readToken, ct);

        var projects = new List<CoolifyProject>();
        foreach (var p in RequireArray(projectsDoc.RootElement, "projects"))
        {
            var id = p.Str("uuid");
            if (id.Length > 0) projects.Add(new CoolifyProject(id, p.Str("name", "(senza nome)")));
        }

        using var envLimit = new SemaphoreSlim(4); // at most four concurrent requests
        var envTasks = projects.Select(async p =>
        {
            await envLimit.WaitAsync(ct);
            try
            {
                using var doc = await GetJsonAsync($"projects/{Uri.EscapeDataString(p.Uuid)}/environments", readToken, ct);
                var found = new List<(long id, string project, string projectUuid, string environment)>();
                foreach (var env in RequireArray(doc.RootElement, "environments"))
                {
                    var id = env.Int("id");
                    if (id != 0) found.Add((id, p.Name, p.Uuid, env.Str("name")));
                }
                return found;
            }
            finally
            {
                envLimit.Release();
            }
        });
        var envLists = await Task.WhenAll(envTasks);
        var environmentMap = envLists.SelectMany(x => x)
            .GroupBy(x => x.id)
            .ToDictionary(g => g.Key, g => (g.First().project, g.First().projectUuid, g.First().environment));

        var apps = new List<CoolifyApplication>();
        foreach (var a in RequireArray(appsDoc.RootElement, "applications"))
        {
            var uuid = a.Str("uuid");
            if (uuid.Length == 0) continue;
            environmentMap.TryGetValue(a.Int("environment_id"), out var env);
            apps.Add(new CoolifyApplication(
                uuid,
                a.Str("name", "(senza nome)"),
                a.Str("status", "non disponibile"),
                a.Str("git_branch", ""),
                env.project ?? "(progetto sconosciuto)",
                env.projectUuid ?? "",
                env.environment ?? "(unknown environment)",
                a.Str("fqdn", "")));
        }
        return new CoolifyInventory(
            projects.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            apps.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToArray());
    }

    /// <summary>
    /// Fetch the latest deployment for each application with limited concurrency.
    /// Coolify normally returns { count, deployments }, but some versions
    /// return an array directly. Support both formats.
    /// A failure retrieving one application's history must not block card loading.
    /// </summary>
    public async Task<(Dictionary<string, LatestDeploy> Values, int Unavailable)> GetLatestDeploysAsync(
        IReadOnlyList<CoolifyApplication> apps, CancellationToken ct)
    {
        using var gate = new SemaphoreSlim(4);
        var values = new System.Collections.Concurrent.ConcurrentDictionary<string, LatestDeploy>(
            StringComparer.OrdinalIgnoreCase);
        var unavailable = 0;
        var tasks = apps.Select(async app =>
        {
            await gate.WaitAsync(ct);
            try
            {
                using var doc = await GetJsonAsync(
                    $"deployments/applications/{Uri.EscapeDataString(app.Uuid)}?skip=0&take=1",
                    readToken, ct);
                var root = doc.RootElement;
                var deployments = root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.ValueKind == JsonValueKind.Object &&
                      root.TryGetProperty("deployments", out var list) && list.ValueKind == JsonValueKind.Array
                        ? list : default;
                if (deployments.ValueKind != JsonValueKind.Array)
                {
                    Interlocked.Increment(ref unavailable);
                    return;
                }
                var entries = deployments.EnumerateArray().ToArray();
                if (entries.Length == 0)
                {
                    values[app.Uuid] = new LatestDeploy(null, "", "", false);
                    return;
                }
                var newest = entries.OrderByDescending(x => x.Date("created_at") ?? x.Date("updated_at"))
                                    .First();
                var status = newest.Str("status", "sconosciuto");
                var terminal = status.Equals("finished", StringComparison.OrdinalIgnoreCase) ||
                               status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                               status.Equals("cancelled", StringComparison.OrdinalIgnoreCase);
                var when = (terminal ? newest.Date("updated_at") : null) ??
                           newest.Date("created_at") ?? newest.Date("updated_at");
                values[app.Uuid] = new LatestDeploy(when, status, newest.Str("commit"), true);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpRequestException) { Interlocked.Increment(ref unavailable); }
            catch (JsonException) { Interlocked.Increment(ref unavailable); }
            catch (InvalidOperationException) { Interlocked.Increment(ref unavailable); }
            finally { gate.Release(); }
        });
        await Task.WhenAll(tasks);
        return (new Dictionary<string, LatestDeploy>(values, StringComparer.OrdinalIgnoreCase), unavailable);
    }

    public async Task<string> StartDeployAsync(string resourceUuid, CancellationToken ct)
    {
        // The Coolify API accepts POST /deploy?uuid=...&force=false.
        var query = $"deploy?uuid={Uri.EscapeDataString(resourceUuid)}&force=false";
        using var doc = await SendJsonAsync(HttpMethod.Post, query, deployToken, ct);
        var root = doc.RootElement;
        if (!root.TryGetProperty("deployments", out var deployments)
            || deployments.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Coolify did not return a deployments list. Response: " +
                                                root.Str("message", "no details"));
        foreach (var item in deployments.EnumerateArray())
        {
            if (item.Str("resource_uuid").Equals(resourceUuid, StringComparison.OrdinalIgnoreCase))
            {
                var deploymentUuid = item.Str("deployment_uuid");
                if (deploymentUuid.Length > 0) return deploymentUuid;
                throw new InvalidOperationException("Deployment was not queued: " + item.Str("message", "UUID missing"));
            }
        }
        throw new InvalidOperationException("No deployment was queued for this application.");
    }

    /// <summary>
    /// A single read-only application status snapshot. Deployment jobs can be
    /// "finished" while container health checks are still settling.
    /// </summary>
    public async Task<string> GetApplicationStatusAsync(string appUuid, CancellationToken ct)
    {
        using var doc = await GetJsonAsync(
            $"applications/{Uri.EscapeDataString(appUuid)}", readToken, ct);
        var status = doc.RootElement.Str("status");
        if (string.IsNullOrWhiteSpace(status))
            throw new InvalidDataException("Coolify returned no runtime status for the application.");
        return status;
    }

    public async Task<DeploymentInfo> GetDeploymentAsync(string deploymentUuid, CancellationToken ct)
    {
        using var doc = await GetJsonAsync(
            $"deployments/{Uri.EscapeDataString(deploymentUuid)}", readToken, ct);
        var root = doc.RootElement;
        return new DeploymentInfo(root.Str("deployment_uuid", deploymentUuid),
                                  root.Str("status", "sconosciuto"),
                                  root.Str("commit", ""));
    }

    private Task<JsonDocument> GetJsonAsync(string path, string token, CancellationToken ct) =>
        SendJsonAsync(HttpMethod.Get, path, token, ct);

    private async Task<JsonDocument> SendJsonAsync(HttpMethod method, string path, string token, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            var hint = res.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Token scaduto o non valido.",
                HttpStatusCode.Forbidden => "Permessi API insufficienti, team errato o IP non autorizzato.",
                HttpStatusCode.TooManyRequests => "Limite richieste API raggiunto.",
                _ => "Controllare Coolify e l'endpoint richiesto."
            };
            throw new HttpRequestException($"Coolify HTTP {(int)res.StatusCode} ({res.ReasonPhrase}). {hint}", null, res.StatusCode);
        }
        try { return JsonDocument.Parse(body); }
        catch (JsonException e) { throw new InvalidOperationException("The API returned invalid JSON.", e); }
    }

    private static JsonElement.ArrayEnumerator RequireArray(JsonElement root, string endpoint)
    {
        if (root.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Unexpected response from /{endpoint}: expected a JSON array.");
        return root.EnumerateArray();
    }

    public void Dispose() => http.Dispose();
}

internal static class JsonExtensions
{
    public static string Str(this JsonElement obj, string key, string fallback = "")
    {
        if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(key, out var value))
            return fallback;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.GetRawText(),
            _ => fallback
        };
    }

    public static DateTimeOffset? Date(this JsonElement obj, string key)
    {
        var raw = obj.Str(key);
        return DateTimeOffset.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var value) ? value : null;
    }

    public static long Int(this JsonElement obj, string key)
    {
        if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(key, out var value))
            return 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var n) => n,
            JsonValueKind.String when long.TryParse(value.GetString(), out var n) => n,
            _ => 0
        };
    }
}
