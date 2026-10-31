# Diario tecnico CDM — 2026-10-31: chiusura 2.2.1

Repository: `Cris0338/CDM-Coolify-Deploy-Manager` — autore: Cris0338. Branch di sviluppo: `feature/embedded-clone-20261031`. Nessun segreto, chiave SSH, token, clone personale o backup deve essere copiato nel repository.

## Milestone completate e prova

1. **Salvataggio del clone personale.** Individuato e corretto il difetto `File.Replace($pending,$target,$null)` con Windows PowerShell: percorso di backup nullo convertito in percorso vuoto. Ora la sostituzione usa un backup reale `.before-save.bak`, il processo helper viene avviato solo in chiusura, e i test sintetici confermano applicazione di `.pending`, backup precedente identico per SHA256, conservazione di token fittizio/ordine card/blocchi. Principali fonti: `Services/ExecutableProfile.cs`, `Services/SettingsStore.cs`, `App.xaml.cs`, `BUILD_PORTABLE.ps1`.
2. **Tunnel SSH dopo un minuto.** Il controllo di `MainWindow.xaml.cs` esistente a 60 secondi segnala il processo SSH terminato; è stata introdotta la riconnessione automatica con esclusione di deploy in corso, chiusura e modalità non gestite, più diagnostica di codice uscita/stderr in `Services/SshTunnel.cs`. Il test offline della policy è PASS. La causa della precedente terminazione non è accertata, e le modifiche alle card non hanno dimostrato una causa diretta.
3. **Versione identificabile.** CDM `2.2.1` visualizza in Informazioni, titolo e registro la versione assembly, il commit Git e la data UTC della build. I test offline verificano l'identità dentro l'EXE generico e nel clone di prova dopo upgrade. File: `BuildInfo.cs`, `AboutWindow.xaml(.cs)`, `MainWindow.xaml.cs`, `CoolifyDeployManager.csproj`, `Localization/{en,fr,it}.json`, `BUILD_PORTABLE.ps1`.
4. **Build Windows dell'autore:** compilazione riuscita in 11,8 s. EXE generico con Git `56ec905c0b68`, build UTC `2026-10-31T21:33:56Z`, SHA256 `3037F118C4529256910ECF84A6B29B0AB8F9A7F072C8AB4A77100E81CE6F953B` (~64,55 MB). TUTTI i test del build: PASS (versione, generic clean, health card, SSH policy, clone sintetico startup/upgrade, backup e pending aggiornato). Non significa aver eseguito un deploy reale.
5. **Accettazione del clone reale:** l'autore ha confermato di aver aggiornato il proprio clone, collegato a Coolify, e osservato per oltre 14 minuti un tunnel SSH stabile, senza disconnessioni nel registro. Non sono stati eseguiti deploy reali, né prova su secondo PC.
6. **Decisione finale:** congelare il software 2.2.1 funzionante, pubblicare solo i componenti **generici** come GitHub **pre-release**, proseguire i collaudi dei deploy in esercizio ordinario. Esclusi interventi congetturali ulteriori.
7. **Preparazione GitHub:** documentazione di rilascio v2.2.1 in EN/FR/IT, `RELEASE_GUIDE.md`, `CHANGELOG_2.2.1.md`, README, `PROJECT_STATUS_2026-10-31.md` e `ROADMAP.md` allineati all'accettazione. La pubblicazione effettiva richiede anche un tag verificato e l'upload dei file binari generici prodotti *localmente* da `PACKAGE_RELEASE.ps1`. Il connettore GitHub disponibile può aggiornare codice/branch ma non creare una Release con asset binari: NON segnare release pubblicata finché non risulta davvero presente sulla pagina GitHub Releases.

## Da completare dopo la preparazione

- Verificare main fast-forward al commit di consolidamento (se autorizzato); non riscrivere la cronologia.
- Preparare localmente `CDM.exe`, `CDM-2.2.1-win-x64.zip`, `CDM-2.2.1-SHA256.txt` e verificare i controlli anti-credenziali.
- Creare tag e **pre-release** v2.2.1 con questi soli tre asset; controllare URL e SHA256 pubblici. Se taggata la build esattamente verificata, puntare al commit `56ec905c0b6894898a78fc9a92eba93b7925593a`.
- Da testare in futuro: deploy volontario staging, eventuale riconnessione reale SSH e secondo PC. Non mescolare nuovi interventi al rilascio congelato.

Documento complementare: `docs/AUDIT_2026-10-31_FULL_CODE.md` (esiti dettagliati e problemi ancora aperti).

## Consolidamento sul branch principale

Il 31/10/2026 il branch `main` è stato avanzato con operazione **fast-forward** da `c9514ec55c96b35734cf23670d0ea5c4dee56f48` a `1ea1de7e55148cb7f605b57e3ef8881bf4eb04f9`, senza riscrittura della cronologia né merge artificiale. Diff verificato: 115 commit avanti, zero indietro. Gli otto commit successivi al sorgente provato dall'autore (`56ec905c0b68`) sono **solo documentazione**, senza modifiche al codice eseguibile. La sincronizzazione dei sorgenti con GitHub è quindi completata. La **GitHub Release con tre asset non è ancora pubblicata** e richiede il pacchetto generico presente sulla macchina Windows dell'autore.
