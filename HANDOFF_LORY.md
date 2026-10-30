# Handoff sviluppatore — Coolify Deploy Manager WPF 2.0

Progetto ricreato da zero nella UI WPF, mantenendo l'implementazione API v1.3 collaudata sul server dell'utente.

## File chiave

- `src/CoolifyDeployManager/App.xaml` — palette moderna e stili
- `src/CoolifyDeployManager/MainWindow.xaml(.cs)` — dashboard, tab progetti, filtri, card, log, deploy, batch, drag/drop
- `src/CoolifyDeployManager/SettingsWindow.xaml(.cs)` — HTTPS, tunnel interno/esterno, token
- `src/CoolifyDeployManager/Services/SshTunnel.cs` — process SSH gestito, avvio automatico, host key verification, health check, cleanup
- `src/CoolifyDeployManager/Services/CoolifyApi.cs` — API Coolify preesistenti, NON riscrivere senza test
- `src/CoolifyDeployManager/Services/SettingsStore.cs` — DPAPI, compatibilità settings.json
- `src/CoolifyDeployManager/Models.cs` — classificatore tramite nome, branch, progetto, ambiente; `release`→prod
- `tests/ClassifierTests/` — test pure .NET per 8 scenari

## Sicurezza

Il client SSH avviato dal processo WPF usa `-o BatchMode=yes`, `-o StrictHostKeyChecking=yes`, `-o ExitOnForwardFailure=yes`, `-L 127.0.0.1:<port>:<remote-host>:<port>`, `-N -T`. Nessun token passato come argomento. Il processo è posseduto e terminato dal programma; non viene adottato un listener già esistente. L'OpenSSH del sistema è un prerequisito del tunnel, NON incluso nell'EXE.

## Stato della consegna

Test strutturali statici eseguiti sul codice e sull'XAML. **Build WPF Windows non eseguita in questo ambiente**. In caso di errori di compilazione, correggere la base completa e rieseguire i test, evitando patch temporanee da distribuire. Il rilascio pubblico va eseguito solo dopo test d'integrazione con Coolify.
