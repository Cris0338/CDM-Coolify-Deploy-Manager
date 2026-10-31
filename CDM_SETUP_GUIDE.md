# CDM 2.2 — connection and setup guide

This guide describes the **current, embedded-profile build**, not earlier file-based configuration methods. **No separate `settings.json`, password vault, external profile or persistent AppData configuration is created or read.**

## 1. Start from an empty `CDM.exe`

The generic executable always starts the setup wizard without a server/token. Set a server nickname, preferred language and connection mode. Finishing a verified setup creates `CDM-<WindowsUser>-<ServerLabel>.exe` in the same directory and launches it. Future launches of this clone use the credentials embedded in **that EXE**.

## 2. Choose how to reach Coolify

- **Trusted HTTPS:** enter the full dashboard URL. Certificates remain verified.
- **Private LAN / private VPN:** direct `http://192.168.1.50:8000`, another private address or local hostname works without public DNS or a certificate; the private network provides the transport boundary.
- **Public IP with HTTP-only Coolify:** choose the **automatic SSH tunnel**, not direct public HTTP. This transports requests locally via encrypted SSH without DNS or Let's Encrypt.

For SSH, enter the VPS hostname/IP, SSH username, SSH port (often `22`), local port (`18000` by default), and remote Coolify host/port (often `127.0.0.1:8000`).

## 3. Generate and authorize a CDM-only SSH key

Click **Generate dedicated SSH key**. CDM generates a new ED25519 identity independent of any personal Windows SSH key.

The wizard then asks:

**Do you already have a working SSH alias configured on this Windows PC?**

- **Yes:** enter the existing alias name, e.g. `contabo`, and click **Authorize using SSH alias**. The alias is used **only for first-time authorization**, not by the resulting clone.
- **No:** a selectable command and **Copy VPS command** button appear. Copy the full command, open the VPS console via your hosting provider or another authorized access path, log in as the intended SSH user and paste it. This adds CDM's generated public key to that account's `~/.ssh/authorized_keys`.

Do not confuse the VPS IP field with an SSH alias. The two fields are not interchangeable.

### Verify VPS identity

Click **Verify VPS SSH fingerprint**. Independently compare the displayed `SHA256:...` value with the server's own fingerprint:

```bash
ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256
```

The verification window has separate **Copy** buttons for both fingerprint and command. Confirm only when they match.

Windows `ssh-keyscan.exe` can fail with `unsupported KEX method sntrup761x25519-sha512@openssh.com`. CDM obtains the host key with an isolated, unauthenticated `ssh.exe` handshake rather than relying on `ssh-keyscan.exe`.

On Windows, OpenSSH may reject keys with inherited group read permissions (`UNPROTECTED PRIVATE KEY FILE`). CDM restricts the permissions of its **own temporary SSH private-key file** via Windows `icacls.exe` before opening the tunnel, without modifying personal SSH keys.

## 4. Create one Coolify API token

In Coolify, go to **Keys & Tokens → API Tokens**. Create **one** token with both **READ** and **DEPLOY**, without Root/Write/Read Sensitive Data. Paste it into CDM and click **Verify and create/save**.

The wizard performs **read-only** API verification, then creates the personalized clone. No deployment occurs during configuration. For manual browser-based Coolify login, the wizard can open `http://127.0.0.1:18000` through its own SSH tunnel.

The running clone owns its SSH tunnel, establishes a local forward, checks `/api/health` and reads the API inventory. An SSH client alias on the new computer is not required.

## 5. Use the dashboard

- Use the single searchable project picker and application search/status filters.
- Check staging card boxes for **Deploy selected staging**.
- **Deploy ALL staging** selects all eligible staging applications in the current project filter, excluding blocked deploys. Both actions prompt for confirmation.
- Right-click a card for **Lock position** or **Block deployment**. Flags are embedded in the clone and survive its next save/restart.
- Drag the grip to reorder cards within the same staging, production or unknown section. The blank drop target appends a card at the end; a fully free-form grid with persistent empty cells is not available yet.
- CDM monitors requested deployments sequentially; cancelling local monitoring does not guarantee a server-side deployment is cancelled.

## 6. Update an existing clone

Updating `CDM.exe` does **not** automatically update a personalized clone. Close your clone, use the new generic executable with explicit `--upgrade-clone "<path-to-personal-exe>"`, then relaunch the clone. See [BUILDING.md](BUILDING.md).

A timestamped `.bak` of the personal clone is created. Both backup and clone hold recoverable token/SSH credentials; keep them private. The generic template has none.

## Troubleshooting

| Symptom | Likely cause / action |
| --- | --- |
| HTTPS error: untrusted root | The certificate chain or address is not trusted. Use a valid HTTPS host or managed SSH. |
| HTTPS IP returns 503 | Reverse proxy does not route the requested host; use the configured host or managed SSH. |
| Public HTTP address rejected | Use SSH forwarding instead of transmitting the API token on public HTTP. |
| SSH exits with code 255 | Read OpenSSH's actual stderr (shown by the wizard); check authorization, username, pinned host fingerprint and port. |
| SSH key permissions rejected | CDM automatically restricts its temporary key ACL; check the diagnostic if `icacls` fails. |
| `ssh.exe` not found | Install the optional Windows OpenSSH Client. |
| Port `18000` occupied | Close the old tunnel or change the local forward port. |
| Old settings not loaded | Expected: this version **never imports older file-based settings**. Use the new wizard. |
| Explorer icon looks old | Windows may have cached it by filename; test a copy under a new name before rebuilding. |

## Status and limits

As of 31 October 2026: automatic managed SSH, API read access, generic and personal clone startup, and disposable-clone upgrades have been exercised on Windows. **The real staging deploy and complete review of the new card controls are still pending.**

## Data and license

The personalized EXE contains an **unencrypted** embedded settings payload, including the API token and SSH private key. Hash verification detects corruption, not theft. Disposable SSH identity/host-key files and staging files may appear in the Windows **temporary directory**, but no durable `LocalAppData` settings store is used.

CDM is offered free under the [CDM Free Redistribution License v1.0](LICENSE), which allows free use and sharing but forbids sale or paid distribution without written authorization.

## If a personalized CDM clone is lost or stolen

A personalized `.exe`, an unapplied `.pending` file, and timestamped `.bak` files contain extractable credentials. An attacker with such a file may have **two independent forms of access**: a Coolify API token and, if SSH forwarding was configured, a dedicated SSH private key. Rotating the token alone does not disable that key.

1. **Revoke API token(s)** — Sign in to Coolify using your normal browser/account. Select the correct team, open **Keys & Tokens → API Tokens**, locate the token used by CDM and select **Revoke token**. Revoke every exposed token if the clone contains more than one. Confirm the old token is no longer accepted. See the official [Coolify API Tokens guide](https://coolify.io/docs/core/security/credentials/api-tokens).
2. **Remove the CDM SSH authorization if used** — Open the VPS provider's console or log in using a **different, still-authorized SSH identity**. Use the **same Linux user** that was configured in CDM, then inspect its SSH authorization file:

   ```bash
   nano ~/.ssh/authorized_keys
   ```

   Find the public-key line installed during CDM setup by comparing the key material with the saved CDM public key (or its independently recorded fingerprint). **Delete only that exact line**, then save the file. Do not erase `authorized_keys` or other users' keys. If you cannot confidently identify CDM's line, do not remove one arbitrarily: first inventory and positively identify the authorized keys. The wizard's authorization command stores the key without a distinctive comment, so a missing clone cannot identify it by filename or a guaranteed `CDM` comment.
3. **Re-create access** — From the clean generic `CDM.exe`, create a new Coolify token with the required permissions and a **new, separately authorized SSH key**, if necessary. The lost clone's token and server-side SSH authorization should remain revoked.

**No password or Windows-account binding is required by CDM.** Its portability is intentional; the user must revoke exposed credentials after a copy is compromised.
