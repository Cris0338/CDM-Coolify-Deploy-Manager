# CDM — Roadmap pubblica

## Stato
- [x] Codice CDM 3.0.x, interfaccia WPF Windows e test importati da uno snapshot dei sorgenti.
- [x] Cronologia di sviluppo precedente ESCLUSA dalla nuova copia pubblicabile.
- [x] Audit privacy finale di testo, immagini e binario generico superato prima della pubblicazione.
- [x] Compilazione e smoke test di CDM 3.0.2 su Windows superati (FASE 7; packaging ufficiale FASE 10A).
- [x] Nuovo repository GitHub creato con un singolo commit iniziale pulito (le manutenzioni successive sono nuovi commit separati).
- [x] Release pubblica v3.0.2: EXE generico, ZIP e SHA256, verificati su GitHub.

## Regole operative
- Nessuna credenziale, token, chiave SSH, indirizzo del VPS reale o percorso utente nei file pubblici.
- Mai pubblicare cloni personali dell'applicazione.
- Nessuna GitHub Action; build e release esclusivamente locali e manuali.
- Aggiornare questa roadmap e il diario tecnico dopo ogni modifica sostanziale.
### 2026-11-03 — FASE 3: esempi IPv4
- [x] Sostituiti gli IP pubblici presenti negli esempi dell'applicazione con indirizzi TEST-NET-3, senza toccare test locali/CGNAT.
- [ ] Confermare con scansione Fase 2 il risultato sul candidato locale.
- [ ] Eseguire compilazione Windows, smoke test e audit dell'EXE generico prima della nuova pubblicazione.
- [x] FASE 4: rimossi chunk PNG accessori da immagini demo senza alterare pixel; backup privato creato.
- [x] FASE 4: corretta compatibilita refresh cloni 3.0.0/3.0.1/3.0.2 e controllo build 3.0.2; [ ] ripetere build/smoke offline.
- [x] FASE 4: riallineato nome fittizio del test SSH; [ ] ripetere build e smoke completi prima della release.
- [x] FASE 4: logo app senza metadati C2PA, mantenendo IDAT identico e hash logo sanificato vincolato in BUILD_PORTABLE.ps1; [ ] rieseguire build/smoke/audit.

## 2026-11-03T17:25:55Z — Documentazione pubblica
- [x] Smoke manuale EXE generico v3.0.2.
- [x] Link e testi pubblici allineati alla release 3.0.2 NON ANCORA pubblicata.
- [ ] Nuovo commit Git root unico e verifica dei file tracciati.
- [ ] Sostituzione manuale del repository remoto, previa decisione esplicita.
- [ ] Build e release da commit pubblico definitivo.

## 2026-11-03T17:35:08Z - FASE 6
- [x] Nuovo snapshot Git root locale con asset di compilazione inclusi (da verificare nel report).
- [ ] Ripetere build, smoke e audit binario dal commit root.
- [ ] Solo dopo: sostituzione pubblica di GitHub previa autorizzazione esplicita.
- [ ] Compilare release definitiva dal commit pubblico.

## 2026-11-03 — FASI 7–11: pubblicazione verificata
Le checklist delle fasi 3–6 sopra riportano lo stato storico al momento della loro compilazione. Lo stato corrente e il seguente:

- [x] FASE 7: build Windows x64, test offline WPF/SSH, EXE generico, ZIP a 6 file e SHA256 verificati dal commit iniziale `0983609ba48c4fd509e538f965266767d652f28e` (75 file).
- [x] FASE 8: controllati backup storico privato, SHA256, mirror Git e corrispondenza sorgenti/build; nessuna pubblicazione in questa fase.
- [x] FASE 9: eliminato il vecchio repository GitHub, ricreato vuoto con lo stesso nome e pubblicato solo il commit pulito su `main`, senza `push --force`.
- [x] FASE 10A: ricompilata e confezionata la release definitiva dal commit pubblico; verificati manifest SHA256 e whitelist di 6 file nello ZIP.
- [x] FASE 10B: release pubblica stabile `v3.0.2` del 2026-11-03, tag legato al commit `0983609ba48c4fd509e538f965266767d652f28e`; allegati: `CDM-v3.0.2.exe`, `CDM-3.0.2-win-x64.zip`, `CDM-3.0.2-SHA256.txt`.
- [x] FASE 11: corretto il controllo versione in `PUBLISH_RELEASE_3.0.2.ps1`; aggiornati ROADMAP e DIARIO TECNICO in un commit di manutenzione distinto, senza modificare il tag della release.

### Prossimi passi
- [ ] Sincronizzare le copie locali con `git pull --ff-only origin main` prima di nuove modifiche.
- [ ] Per una futura release, predisporre uno script specifico o parametrico, evitando di ripubblicare `v3.0.2`; le release restano build e caricamenti locali manuali, mai GitHub Actions.
- [ ] Allineare in un successivo intervento i documenti storici `RELEASE_GUIDE.md` e `RELEASE_NOTES_3.0.2.md` che descrivono ancora la fase precedente alla pubblicazione.

Release: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/tag/v3.0.2
