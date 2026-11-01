// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// The spinner represents server-confirmed deployment activity, not elapsed time.
// Only explicit active states are animated; unknown/error/terminal states are not.
namespace CoolifyDeployManager;

internal static class DeploymentActivity
{
    internal static bool IsActive(string? status) =>
        status?.Trim().ToLowerInvariant() is
            "queued" or "pending" or "waiting" or "starting" or "in_progress";
}
