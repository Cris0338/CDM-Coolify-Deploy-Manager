# Prompt for continuing CDM in a new conversation

Sto continuando lo sviluppo del mio software personale **CDM — Coolify Deploy Manager**, Windows WPF/.NET 10, repo pubblico `Cris0338/CDM-Coolify-Deploy-Manager`, branch di lavoro `feature/embedded-clone-20261031`. È un progetto **di Cris0338, separato da Chataide.fr**.

**Prima di lavorare leggi obbligatoriamente su GitHub:** `docs/HANDOFF_2026-10-31_UI.md`, `PROJECT_STATUS_2026-10-31.md`, `CHECKLIST_TEST_WPF.md`, `RELEASE_GUIDE.md` e i sorgenti XAML interessati. Verifica il commit HEAD attuale invece di assumere che sia invariato.

Quattro screenshot dell'applicazione precedente mostrano:
1. menu contestuale `Blocca posizione / Blocca deploy` con enorme gutter/checkmark bianco su fondo scuro: **rifare lo stile WPF correttamente**;
2. intestazione con `Utente / VPS` sotto il sottotitolo anziché **sotto il badge verde `Portable · Configurato`**: correggere con layout responsivo;
3. logo in alto e nella finestra Informazioni **sfocato**: usare le immagini ufficiali ad alta risoluzione, non l'ICO piccolo scalato, mantenendo branding e ico dell'EXE;
4. finestra `Informazioni`: bottone chiudi rosso appare come un `›` anziché X, stringa licenza troncata, e nella foto vecchia la versione è `2.1.1`. ATTENZIONE: il codice attuale ha già versione `2.2.0` e la sezione PayPal.Me: verificare con nuovo build/upgrade del clone, senza attribuire alla versione attuale problemi visti solo nell'EXE vecchio.

Controllare la coerenza di tutte le modali, le checkbox dark, la possibilità reale di trascinare card e persistenza di posizione/blocchi. Non inventare implementazioni già superate. Preferisco una correzione alla volta, **comandi PowerShell singoli**, niente spiegazioni interminabili; aggiornamenti GitHub sul branch di sviluppo con hash commit. Non eseguire mai GitHub Actions.

Stato e vincoli: `CDM.exe` generico **senza configurazione**; il clone `CDM-<utente>-<server>.exe` incorpora token, SSH e impostazioni, non deve MAI essere pubblicato. Licenza `CDM Free Redistribution License v1.0`: uso e condivisione gratis, vendita vietata. PayPal.Me confermato `https://www.paypal.com/paypalme/Cris0338/`, donazione di importo libero, con testi **in 14 lingue** e pulsante nella schermata About + `.github/FUNDING.yml`.

La branch feature ha i sorgenti dell'integrazione PayPal, ma **main è ancora al vecchio checkpoint** e non esiste alcuna release GitHub pubblicata all'ultimo controllo. Il pacchetto `release-artifacts` preparato prima dei nuovi cambiamenti è OBSOLETO: dopo le correzioni ricompilare e ripacchettare `CDM.exe` generico, ZIP e checksum usando `BUILD_PORTABLE.ps1` e `PACKAGE_RELEASE.ps1` (nessun upload automatico). Non spostare/taggare su main senza collaudi e mia conferma.

**Parti dalle anomalie del menu contestuale e del logo.** Prima verifica i sorgenti correnti su GitHub, poi proponi/implementa correzioni sul branch; dammi un comando PowerShell per volta per testare il risultato. Conserva un diario tecnico in repo.
