# CDM â€” Diario tecnico pubblico

## 2026-11-03 â€” Preparazione della ripartenza pulita
- Creato uno snapshot dei soli file attuali dell'applicazione, escludendo deliberatamente ogni cronologia Git precedente.
- I documenti storici e i backup restano esclusivamente nella cartella privata dell'autore.
- Bonificati gli esempi di configurazione conosciuti; restano da verificare tutti gli altri riferimenti e i binari compilati.
- Nessuna pubblicazione e nessuna eliminazione effettuata da questa procedura.
- Prossimo passo: analisi dei report privacy, test Windows, quindi inizializzazione del nuovo Git e rilascio.
## 2026-11-03 â€” FASE 3: IPv4 pubblici negli esempi
- Bonificati gli esempi di IP pubblico nei sorgenti attuali (guida, localizzazione, interfaccia e test), usando TEST-NET-3 riservata alla documentazione.
- Gli indirizzi privati, loopback e CGNAT dei test sono stati mantenuti.
- Verifica testuale finale richiesta con CDM_FASE2_VERIFICA_PRIVACY.ps1, poi build Windows e audit binario prima di pubblicare.
- Nessun intervento sulla cronologia Git o sulle release GitHub.
2026-11-03: FASE 4 â€” rimosse informazioni accessorie PNG dalle 3 immagini di documentazione; backup fuori dal candidato; no modifiche al repo remoto.
2026-11-03: FASE 4, correzione della policy di upgrade 3.0.2 e del relativo smoke test (input 3.0.0/3.0.1/3.0.2, output 3.0.2); corretto controllo della FileBuildPart di destinazione. Nessuna modifica ai profili reali, nessuna modifica a GitHub. Il test Windows completo resta da ripetere.
2026-11-03: FASE 4 - corretto solo il fixture username di VerifyForSmoke in SshBootstrap.cs (utente demo -> demo-cdm). L anonimizzazione aveva lasciato divergenti input e attesa; le policy di sicurezza SSH e il bootstrap reale non sono stati modificati. Backup privato creato; smoke/build da rieseguire. Nessuna operazione GitHub.
2026-11-03: FASE 4 - eliminato solo chunk PNG caBX (C2PA) dal logo incluso in app, senza decodifica o modifica di IDAT/pixel. BUILD_PORTABLE.ps1 mantiene verifica SHA256 dello ZIP e dell artwork originale, rimuove caBX anche dopo reimport, e controlla SHA256 del logo sanificato. Icona ICO invariata. Backup privato esterno al candidato. Nessun accesso a GitHub. Test e audit binario da ripetere.

## 2026-11-03T17:25:55Z — Fase 5: documentazione 3.0.2 corrente
- Test manuale confermato: EXE generico senza configurazione, wizard e Guida apribili, schermata Info v3.0.2.
- README EN/IT, guida setup, build, note release e checklist di pubblicazione riscritti senza affermare che v3.0.2 sia gia pubblicata.
- Esclusi link a release v3.0.0/v3.0.1, vecchi SHA e commit di vecchie release.
- Documenti precedenti conservati soltanto nel backup privato FASE5.
- Nessuna modifica al repository originale e nessuna operazione GitHub.
- Prossimo passo: preparazione del nuovo commit root singolo; pubblicazione solo dopo verifica della nuova cronologia.
## 2026-11-03T17:35:08Z - FASE 6: sorgenti pubblici autonomi e root Git locale
- La fase 5 e il test manuale del generico v3.0.2 sono PASS.
- Nuova copia di lavoro isolata, senza vecchia cronologia e senza remote Git.
- BUILD_PORTABLE.ps1 usa logo PNG bonificato e ICO verificato inclusi nel codice sorgente, senza ZIP privato.
- I due hash SHA256 grafici sono bloccati nello script di compilazione.
- La build della nuova copia deve essere RIPETUTA prima di qualsiasi pubblicazione: il codice di build e stato aggiornato dopo la fase 4.
- Nessuna pubblicazione e nessuna modifica al repository originale/GitHub.
