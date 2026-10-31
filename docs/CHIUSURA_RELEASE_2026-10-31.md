# 2026-10-31 — Chiusura release CDM v2.2.1

## Stato della distribuzione (verificato con GitHub API)

- Release pubblica: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/tag/v2.2.1
- ID GitHub: `430233550`; `draft=false`, `prerelease=true`; pubblicata `2026-10-31T22:51:40Z`.
- Tag `v2.2.1` → commit `3ad03bb74cfbfbd84f19a04c677892b1c693662f`.
- EXE generico `CDM.exe`: **67.689.213 byte**, SHA256 `4B50975A82F6F542388B033DDB20B266C9A0F430FAF39BDC2423C56628E3915B` (digest GitHub identico al risultato della build locale). Embedded version `2.2.1`, Git `3ad03bb74cfb`, UTC `2026-10-31T22:20:52Z`.
- ZIP `CDM-2.2.1-win-x64.zip`: **61.812.620 byte**, digest GitHub SHA256 `FDF404D8C93589CDC6DCA4987571EDB9EEC40EFF0DFBCF4C9B69F580C9E1575F`.
- Manifest `CDM-2.2.1-SHA256.txt`: **164 byte**, digest GitHub SHA256 `8E2F596428422B779286173B337F1555289FD7BC9E3C8BE139018816559288C4`.
- Esattamente tre asset pubblici, tutti nello stato `uploaded`. Il clone personale e i relativi backup non sono stati pubblicati.

## Accettazione

Il proprietario ha fornito i risultati PASS di tutti i test Windows per compilazione, versione e metadati, template pulito, simulazione clone/upgrade, salvataggio differito e backup, health delle card, logica di recupero tunnel SSH. Ha verificato la connessione del clone reale a Coolify e più di 14 minuti di SSH stabile senza disconnessioni osservate. Il programma 2.2.1 è congelato: nessun intervento al codice dopo la build pubblicata.

**Non dichiarati eseguiti:** deploy reale su staging e test su un secondo PC; da svolgere durante il normale uso. La recovery SSH su disconnessione effettiva rimane da osservare soltanto se il problema si ripresenta. Queste attività non bloccano la pre-release che l'autore ha approvato.

## Consolidamento del repository

- `ROADMAP.md`: pubblicazione verificata, stato chiuso, test futuri distinti.
- `CHANGELOG_2.2.1.md`, `RELEASE_NOTES_2.2.1.md`, `RELEASE_GUIDE.md`, `README.md`, `README_IT.md` e `PROJECT_STATUS_2026-10-31.md`: documenti finali allineati al materiale realmente pubblicato.
- Release online contiene inizialmente un testo più vecchio che citava la prima build di prova (`56ec905c0b68`); il file `RELEASE_NOTES_2.2.1.md` è stato corretto per descrivere la build distribuita. **Completato:** l'autore ha eseguito `gh release edit v2.2.1 --notes-file RELEASE_NOTES_2.2.1.md`. Verificato con GitHub API: la descrizione pubblicata contiene ora il commit `3ad03bb74cfb` e lo SHA256 `4B50975A82F6F542388B033DDB20B266C9A0F430FAF39BDC2423C56628E3915B`, corrispondenti all'EXE distribuito.
- Le operazioni di pubblicazione tramite GitHub CLI vanno eseguite solo sul repository esistente e NON devono mai rieseguire `gh release create`, cambiare il tag o caricare i file credenziali.
- Il lavoro relativo all'immagine/logo nella pagina README è escluso da questa chiusura in seguito alla richiesta di annullamento; nessuna modifica al logo viene richiesta o effettuata in questa fase.

**CHIUSO:** descrizione confermata con GitHub API; release `v2.2.1` pubblica (`draft=false`, `prerelease=true`), tre asset `uploaded`, tag e SHA256 validati. `main` e branch sviluppo erano sincronizzati al commit `e5d28858bb795a74268155fc5312ef60eb6d66e4` prima di quest'ultimo aggiornamento puramente documentale. Nessun nuovo build, deploy, upload binario o modifica all'app. Test di staging e secondo PC rimandati all'uso ordinario, non classificati come PASS.
