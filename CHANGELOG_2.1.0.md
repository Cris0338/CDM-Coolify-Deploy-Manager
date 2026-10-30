# CDM WPF 2.1.0 — UI and languages

- Custom dark title bars for main, settings, guide and about windows.
- Consistent dark WPF ComboBox controls and text/background contrast.
- English, French, Italian localization; automatic Windows language recognition and user override.
- Persistent user language in settings, existing DPAPI-encrypted token storage unchanged.
- More specific connection messages: TLS certificate failures, 12-second HTTPS timeout and HTTP health status.
- Refresh button disabled during inventory loading; a second click does not start a competing request.
- Automatic SSH mode requires an alias at Save time. No changes to deployment API logic or SSH tunnel process.

This release needs a Windows build and UI/connection test before a public GitHub release.
