# CDM Guided Setup — HTTPS, IP, SSH and API

This is the intended in-app troubleshooting sequence for a user with no previous SSH configuration.

## A. Choose HTTPS if available

Enter the full trusted HTTPS URL of the Coolify dashboard. CDM performs the read-only `GET /api/v1/version` request with the new API token. If TLS is untrusted, explain that an IP certificate can be self-signed or issued for a *hostname* rather than the IP. **Never silently disable certificate verification.**

A 503 on an HTTPS IP can be a reverse-proxy hostname mismatch and is not proof that Coolify is offline. Use the configured dashboard domain or SSH fallback.

## B. If Coolify is reachable only on a VPS IP/port

In CDM choose **Managed SSH**. Enter:
- VPS hostname/IP and SSH username;
- SSH port (usually 22), local forward port (default 18000);
- Coolify local address on VPS (`127.0.0.1`) and remote port (usually `8000`).

1. Click **Generate dedicated SSH key**; CDM creates ED25519 credentials without a passphrase.
2. Authorize the public key for the specified VPS user. You can use an already-working Windows SSH alias in the wizard, or copy the public key through existing admin access into `~/.ssh/authorized_keys`. CDM does not ask for or store the VPS account password.
3. Click **Scan and verify VPS host fingerprint**. Independently check the displayed SHA256 fingerprint on the VPS using `ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub`. Confirm only if identical.
4. Click **Open Coolify in browser**. CDM opens its own SSH tunnel and navigates to `http://127.0.0.1:18000`; the local HTTP request is protected in transit by SSH.
5. Log in normally to Coolify in the browser, open **Keys & Tokens → API Tokens**, and generate one token with only **READ + DEPLOY**.
6. Paste that token in CDM; click Verify and create/save. CDM checks read permission without launching a deployment.
7. CDM creates `CDM-WindowsUser-ServerLabel.exe`, launches it and exits the original.

## C. Errors and interpretation

| Result | Meaning | Wizard guidance |
| --- | --- | --- |
| `GET /api/health` -> 200 OK over HTTP:8000 | Coolify reachable through IP | Do not send tokens through public HTTP |
| Protected endpoint -> 401 | Token missing or invalid | Recheck token |
| Protected endpoint -> 403 | API IP restriction / permissions | Inspect Coolify's Allowed API IPs and selected scopes |
| HTTPS/IP -> untrusted root | TLS chain not trusted | Use trusted hostname or SSH |
| HTTPS/IP -> 503 proxy page | Reverse proxy has no route for Host | Use dashboard hostname or SSH |
| SSH exits before /api/health | Authentication/host key/port failed | Check public-key authorization, SSH username, fingerprint and forwarding |
| Windows `ssh.exe` not found | Optional Windows OpenSSH Client unavailable | Enable OpenSSH Client, then retry |

## D. Security and portability

The personal clone includes usable credentials. It is portable across Windows devices with the required runtime capabilities, **not confidential from anyone who has a copy of it**. Revoke the API token and SSH public key if a clone leaks. The SSH client is an OS prerequisite for SSH mode, not supplied within the EXE.

CDM is an independent unofficial application, and browser login is not OAuth authorization-code grant to CDM.

## Previous versions

Previous CDM releases may have saved settings under `%LOCALAPPDATA%\CoolifyDeployManager\settings.json` or alongside the application in `CDM.profile`. **CDM 2.2.0 does not inspect, read, import, alter or delete these files.** The generic `CDM.exe` always opens a blank configuration wizard. Only a personalized clone can load settings, and only from its own executable trailer. To reuse old settings, enter them explicitly in the new wizard.

The local build includes a regression smoke check that the published generic template contains no configuration, even when older user settings exist on the current machine.

## Remote HTTP address detected in the HTTPS field

If a user pastes a public HTTP URL (including a bare IPv4:port address), CDM shows a translated explanation and a one-click **Use secure SSH tunnel for this address** option. Selecting it copies the VPS hostname/IP and Coolify remote port into SSH setup, using 127.0.0.1 for the service address on the server. The SSH username and first-time key authorization remain user-specific. CDM never transmits an API token directly to a remote HTTP endpoint.

## Local Coolify with no domain or Internet publishing

Coolify supports access from a private LAN through `http://192.168.1.50:8000`. CDM's first connection mode accepts HTTPS URLs and HTTP addresses on private RFC1918 networks, IPv6 unique-local/link-local, localhost, private VPN/CGNAT ranges and local hostnames. No domain, public DNS, reverse proxy, SSL certificate or SSH provisioning is necessary to manage a private LAN Coolify instance. The wizard's SSH suggestion is reserved for **public** HTTP addresses. A service such as a local photo library may be accessed by the application's exposed host port or privately inside Docker's network; it does not need a public domain.

## Windows OpenSSH: unsupported ssh-keyscan KEX

Some Windows builds of `ssh-keyscan.exe` advertise `sntrup761x25519-sha512@openssh.com` but cannot negotiate it. The error `choose_kex: unsupported KEX method sntrup761x25519-sha512@openssh.com` occurs before host keys can be retrieved; it is not an SSH password, port or server failure. CDM intentionally uses `ssh.exe` for an unauthenticated host-key handshake with a temporary isolated `known_hosts` file. The SSH authentication is expected to fail, but the verified-on-confirmation public server host key is retained inside the clone. The user must compare its SHA256 fingerprint using a separately trusted server channel before accepting it. No server SSH configuration change is needed.

## Windows OpenSSH rejects the temporary key ("bad permissions")

Some Windows installations grant newly created temporary files inherited permissions to extra local groups (for example `CodexSandboxUsers`). Windows OpenSSH refuses SSH identity files readable by these groups and exits with code 255. Before starting the managed tunnel, CDM now runs the built-in `icacls.exe` on its own temporary `identity` file: inherited grants are removed and only the current user's Windows SID gets an explicit full-control grant. The command runs automatically, without elevation or modifying existing SSH keys. An `icacls` failure is reported explicitly and prevents SSH startup. The disposable private key is removed when the tunnel stops. If authentication still fails, the wizard displays the original OpenSSH stderr.

The SSH fingerprint verification dialog uses the application's dark-themed buttons (including Copy / Yes / No).

## UI workflow (31 October 2026 feature branch)

Choose a project from the dropdown and type in its first search row to filter project names. Stage deployment now has two separate actions: check one or more staging cards and deploy **selected**, or deploy **all** staging cards within the chosen project. Both actions ask for confirmation. Right-click any card to lock its placement or disable deployment; these flags are embedded in the personalized EXE and persist across restart. The blank drag-and-drop region moves a card to the **end of its own section** (not to another classification group). A three-second branded splash is shown only on normal startup.

Important: publishing a newly rebuilt generic CDM.exe does **not** automatically replace an existing portable personalized clone. Do not delete an existing working clone before testing an upgrade path.
