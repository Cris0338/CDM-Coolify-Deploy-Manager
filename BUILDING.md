# CDM 3.0.2 — Build locale Windows

**Requisiti:** Windows 10/11 x64, PowerShell 5.1+, SDK .NET 10 x64, Git for Windows. Per tunnel gestito serve OpenSSH Client.

## Asset grafici

Il nuovo repository include il logo PNG gia bonificato e l'icona ICO approvata in `src/CoolifyDeployManager/Assets/`. `BUILD_PORTABLE.ps1` verifica i rispettivi SHA256 prima di compilare: non occorre piu l'archivio privato `CDM_Logo_Assets.zip` in Downloads. Il logo conserva gli stessi dati grafici della versione originale, senza chunk PNG `caBX`; l'icona ICO e invariata.
## Compilazione e test offline

Dalla radice del repository:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

La procedura esegue test di classificazione, localizzazione, profili, regole SSH, controlli WPF, versioni, salvataggio e rigenerazione del clone, quindi crea `portable\CDM-v3.0.2.exe` Windows x64 self-contained. Usa esclusivamente sorgenti e asset verificati. I test offline non modificano il VPS.

Per preparare un pacchetto locale (senza upload):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\PACKAGE_RELEASE.ps1
```

La cartella `release-artifacts` conterrà soltanto `CDM-v3.0.2.exe`, `CDM-3.0.2-win-x64.zip` e `CDM-3.0.2-SHA256.txt`. Il manifest deve corrispondere ai file; nessun EXE personale deve essere compreso.

## Release ufficiale

La pubblicazione non è ancora stata eseguita. Dopo che `main` del NUOVO repository sarà pubblico e corrisponderà al commit locale, la procedura manuale di preparazione è:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\PUBLISH_RELEASE_3.0.2.ps1
```

**Solo con approvazione esplicita dell'autore**, lo stesso script con `-Publish` crea la release su GitHub. Prima va esaminata [RELEASE_GUIDE.md](RELEASE_GUIDE.md). Non sono previste GitHub Actions.

## Cloni personali

I cloni personali contengono credenziali riutilizzabili non cifrate. Non includere mai `portable/`, `.bak`, `.pending`, token, chiavi SSH o EXE personali nel repository o negli artefatti pubblici.
