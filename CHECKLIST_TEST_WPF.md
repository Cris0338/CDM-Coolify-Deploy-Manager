# CDM WPF release acceptance checklist

This checklist must be completed on Windows before publishing a prebuilt executable.

- [ ] Build with .NET 10 x64; classification and localization tests pass.
- [ ] No executable or local settings are committed to Git.
- [ ] Custom dark window chrome works: drag, minimize, maximize, close and resize.
- [ ] Settings fields, password inputs and drop-downs are legible with the dark theme.
- [ ] Language menu shows all supported locales; Windows language detection and manual override work.
- [ ] Switching languages refreshes dashboard texts without losing settings or tokens.
- [ ] Preview locales gracefully fall back to English when a translation is missing.
- [ ] Read and deploy tokens persist encrypted and remain hidden in the UI/log.
- [ ] HTTPS connection validates the certificate and handles invalid certificates explicitly.
- [ ] Managed SSH opens/closes correctly using trusted host keys without an external console.
- [ ] An occupied local SSH port produces a clear error, without adopting an unrelated process.
- [ ] Refresh discovers projects, environments and application inventory correctly.
- [ ] Staging/production/release classification handles conflicting signals conservatively.
- [ ] Drag-and-drop card order persists across a restart.
- [ ] Individual staging deployment completes; confirm deployed commit and check website health independently.
- [ ] Production deployment always requires explicit confirmation; no automatic production batch.
- [ ] Canceling local monitoring does not claim to cancel server-side deployments.
- [ ] Publish the single-file EXE only after manual validation and security review.
