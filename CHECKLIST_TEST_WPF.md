# 2026-10-30 — WPF 2.0: checklist compilazione e collaudo

**Stato:** sorgenti preparati; non compilati in questo ambiente (SDK Windows/WPF non disponibile).

## Test locale

- [ ] `COMPILA_PORTABLE.ps1` esegue 8/8 test di classificazione
- [ ] `dotnet publish` produce 0 errori
- [ ] `portable\CoolifyDeployManager.exe` è il solo file nella cartella portable
- [ ] Finestra WPF si apre in 1280×720 e 1920×1080, anche con 125% DPI
- [ ] Importa token e impostazioni legacy senza mostrarli in chiaro
- [ ] «Configurazione» → «Tunnel SSH automatico», alias ssh, porta locale 18000, host remoto 127.0.0.1, porta remota 8000
- [ ] Nessuna finestra PowerShell si apre all'avvio
- [ ] Il programma avvia ssh.exe con BatchMode e StrictHostKeyChecking e verifica `http://127.0.0.1:18000/api/health`
- [ ] `ssh.exe` viene chiuso uscendo dall'app, ma solo se creato dal programma
- [ ] Porta locale già occupata → errore esplicito (non riutilizza un tunnel sconosciuto)
- [ ] Perdita SSH → non invia nuove richieste deploy
- [ ] Carica progetti e app; Prix release → PRODUZIONE, Prix staging → STAGING
- [ ] Rendering di `running:healthy`, `exited:unhealthy`, stato sconosciuto
- [ ] Ultimo deploy/status/commit reali
- [ ] Riordino card tramite maniglia, resta salvato dopo riavvio
- [ ] Conferma produzione mostra nome + branch + ambiente
- [ ] Batch staging comprende solo applicazioni staging e solo del progetto selezionato
- [ ] Un deploy staging reale → `finished` + refresh stato; il programma non attribuisce automaticamente healthy
- [ ] «Ferma attesa / coda» interrompe la coda locale senza dichiarare fermato il job su Coolify

## Limiti noti

- L'eseguibile è single-file e include .NET; la modalità SSH automatica usa **OpenSSH Client di Windows**, non una libreria SSH incorporata. Su PC senza `ssh.exe` serve abilitarlo.
- Le credenziali DPAPI non si trasferiscono tra utenti Windows o PC.
- Non pubblicare il tool fino al superamento della checklist e al controllo di hash e dipendenze.
