# CDM — Diario tecnico pubblico

## 2026-11-03 — Preparazione della ripartenza pulita
- Creato uno snapshot dei soli file attuali dell'applicazione, escludendo deliberatamente ogni cronologia Git precedente.
- I documenti storici e i backup restano esclusivamente nella cartella privata dell'autore.
- Bonificati gli esempi di configurazione conosciuti; restano da verificare tutti gli altri riferimenti e i binari compilati.
- Nessuna pubblicazione e nessuna eliminazione effettuata da questa procedura.
- Prossimo passo: analisi dei report privacy, test Windows, quindi inizializzazione del nuovo Git e rilascio.
## 2026-11-03 — FASE 3: IPv4 pubblici negli esempi
- Bonificati gli esempi di IP pubblico nei sorgenti attuali (guida, localizzazione, interfaccia e test), usando TEST-NET-3 riservata alla documentazione.
- Gli indirizzi privati, loopback e CGNAT dei test sono stati mantenuti.
- Verifica testuale finale richiesta con CDM_FASE2_VERIFICA_PRIVACY.ps1, poi build Windows e audit binario prima di pubblicare.
- Nessun intervento sulla cronologia Git o sulle release GitHub.
2026-11-03: FASE 4 — rimosse informazioni accessorie PNG dalle 3 immagini di documentazione; backup fuori dal candidato; no modifiche al repo remoto.
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

## 2026-11-03 — FASE 7: build dal nuovo Git root
- Commit Git iniziale `0983609ba48c4fd509e538f965266767d652f28e`, senza genitori; 75 file tracciati.
- Build Windows x64, test WPF/SSH offline, controllo EXE privo di profilo personale, ZIP con 6 membri autorizzati e SHA256: PASS.
- Lo snapshot autonomo compila senza l'archivio grafico privato. Nessuna pubblicazione GitHub eseguita in questa fase.

## 2026-11-03 — FASE 8: preflight GitHub
- Verificati il backup completo dello storico precedente (SHA256 e mirror Git), l'integrita del nuovo root e la corrispondenza di build/test al commit iniziale: PASS.
- Script di sola lettura, senza cancellazioni ne pubblicazioni.

## 2026-11-03 — FASE 9: nuovo repository pubblico
- Eliminato manualmente il precedente repository pubblico, conservando i backup privati fuori dal progetto pubblicato.
- Ricreato `Cris0338/CDM-Coolify-Deploy-Manager` (GitHub repository ID `1444769418`), inizialmente vuoto.
- Pubblicato `main` tramite push normale del solo commit `0983609ba48c4fd509e538f965266767d652f28e`; verificati GitHub: 1 commit root, 75 file, nessun genitore, nessuna cronologia precedente nel nuovo repository.
- Nessun `push --force`; eventuali copie esterne del vecchio repository non sono sotto il controllo di questa procedura.

## 2026-11-03 — FASE 10: build definitiva e release v3.0.2
- FASE 10A: eseguito `PACKAGE_RELEASE.ps1` sul root pulito sincronizzato con il commit pubblico. Controlli finali sui tre artefatti e manifest, inclusa whitelist di sei membri dello ZIP: PASS.
- File: `CDM-v3.0.2.exe` (78.325.656 byte); `CDM-3.0.2-win-x64.zip` (72.295.083 byte); `CDM-3.0.2-SHA256.txt` (171 byte).
- FASE 10B: pubblicazione manuale GitHub CLI e verifica API: release stabile, non draft e non prerelease, pubblicata il `2026-11-03T20:44:55Z`; tag `v3.0.2` e `main` entrambi su `0983609ba48c4fd509e538f965266767d652f28e`.
- SHA256 EXE pubblicato: `80af809600ba753f9b8d1164b5c3639e4fb5972eb21ca21d60734cc2a3c6868b`.
- SHA256 ZIP pubblicato: `4cd35ffad04f5fed35e6d6383785e38ba3a937d54e2a0fd317768e48ad304292`.
- Release ufficiale: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/tag/v3.0.2

## 2026-11-03 — FASE 11: manutenzione post-release
- Motivo: `PUBLISH_RELEASE_3.0.2.ps1` cercava erroneamente `<Version>3.0.1</Version>`, pur richiedendo esplicitamente la versione 3.0.2.
- Modifica minima: regex di confronto aggiornata a `<Version>3.0.2</Version>`; nessun cambiamento alla logica di pubblicazione o ai binari.
- Aggiornati `ROADMAP.md` e `DIARIO_TECNICO.md` con risultati e prossimi passi.
- Metodo: commit di manutenzione su `main` con parent `0983609ba48c4fd509e538f965266767d652f28e`, senza alterare `v3.0.2` o gli asset gia pubblicati.
- Verifiche previste: confronto statico della condizione nello script, diff limitato a tre file, ref `main` aggiornato e tag `v3.0.2` immutato. Nessun rebuild Windows necessario per questa modifica solo allo script e ai documenti.
- Passo successivo: sincronizzare il repository locale con `git pull --ff-only origin main`; non eseguire nuovamente la pubblicazione `v3.0.2`.
