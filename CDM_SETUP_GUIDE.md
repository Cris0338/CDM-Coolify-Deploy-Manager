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
