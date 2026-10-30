# CDM WPF 2.1.0 — Istruzioni compilazione

1. Estrai lo ZIP in una cartella nuova (non sovrascrivere la 2.0.3).
2. Apri VS Code nella cartella dove trovi `COMPILA_PORTABLE.ps1`.
3. Esegui questo comando nel terminale PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\COMPILA_PORTABLE.ps1
```

I test di classificazione vengono eseguiti prima della build. In caso di esito positivo trovi `portable\CoolifyDeployManager.exe`. Il programma usa .NET incorporato, ma la modalità SSH richiede `ssh.exe` di Windows.

### Esempio di configurazione SSH

Scegli **Tunnel SSH automatico** anziché **Tunnel locale già aperto**. Inserisci alias `my-vps` (sostituiscilo con il tuo), porta locale `18000`, host remoto `127.0.0.1`, porta Coolify `8000`. Chiudi il precedente comando `ssh -N -L ...` se ancora attivo. Il campo HTTPS non serve in questa modalità.

I token read/deploy già configurati sono letti dal profilo Windows e non vengono incorporati nell'EXE. Sono cifrati con DPAPI.

### Modifiche

- Barra titolo scura su tutte le finestre WPF e pulsanti per chiudere/ridurre/ingrandire.
- Contrasto scuro coerente, inclusi i menu a tendina della Configurazione.
- Lingua Windows automatica con scelta EN/FR/IT.
- Errore SSL, timeout HTTPS e health check più comprensibili; aggiornamento bloccato mentre la precedente richiesta è in corso.

**Non è ancora una release pubblica:** questa versione deve essere compilata e collaudata su Windows.
