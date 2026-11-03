# CDM â€” Roadmap pubblica

## Stato
- [x] Codice CDM 3.0.x, interfaccia WPF Windows e test importati da uno snapshot dei sorgenti.
- [x] Cronologia di sviluppo precedente ESCLUSA dalla nuova copia pubblicabile.
- [ ] Audit privacy finale di tutto il testo, immagini e binario generico.
- [ ] Compilazione e smoke test di CDM 3.0.2 su Windows.
- [ ] Nuovo repository GitHub con UNA cronologia iniziale pulita.
- [ ] Release pubblica 3.0.2: EXE generico, ZIP e SHA256.

## Regole operative
- Nessuna credenziale, token, chiave SSH, indirizzo del VPS reale o percorso utente nei file pubblici.
- Mai pubblicare cloni personali dell'applicazione.
- Nessuna GitHub Action; build e release esclusivamente locali e manuali.
- Aggiornare questa roadmap e il diario tecnico dopo ogni modifica sostanziale.
### 2026-11-03 â€” FASE 3: esempi IPv4
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
