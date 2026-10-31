# CDM — Coolify Deploy Manager (Guida in italiano)

**Autore:** [Cris0338](https://github.com/Cris0338) · © 2026 · **[Licenza CDM di ridistribuzione gratuita v1.0](LICENSE)**

CDM è una dashboard Windows 10/11 x64, indipendente e non ufficiale, per gestire gli applicativi Coolify. È gratuito **anche per uso professionale** e puoi modificarlo e condividerlo **gratuitamente**. **Non è consentito venderlo, metterlo dietro un paywall o includerlo in pacchetti a pagamento** senza autorizzazione scritta dell'autore. Le donazioni spontanee all'autore sono benvenute.

## I due EXE: fondamentale

- **`CDM.exe`** — modello generico, **sempre vuoto**. Apre la procedura guidata, senza caricare credenziali o impostazioni precedenti.
- **`CDM-<UtenteWindows>-<NomeVPS>.exe`** — clone personale. Incorpora **dentro l'EXE stesso** indirizzo, token READ+DEPLOY, chiave SSH dedicata (se serve), impronta del VPS, lingua, ordinamento card e relativi blocchi.

Non esistono file persistenti `settings.json`, `CDM.profile`, né un archivio in `LocalAppData`. Nessuna password per sbloccare il clone: contiene credenziali recuperabili. **Non condividere il tuo EXE personale e nemmeno i suoi backup `.bak`.**

## Prima configurazione

1. Apri `CDM.exe`; scegli lingua, nome del server e tipo di collegamento.
2. Per HTTPS verificato usa l'URL. Per un Coolify sulla LAN/VPN puoi usare HTTP con IP privato, **senza dominio**.
3. Per Coolify remoto disponibile soltanto in HTTP via IP pubblico, scegli **Tunnel SSH automatico**.
4. Genera la chiave SSH di CDM: la guida chiede **Hai già un alias SSH?**  
   **Sì** → inserisci l'alias esistente e autorizza la chiave.  
   **No** → copia il comando visualizzato e incollalo nel terminale del VPS, con l'utente SSH indicato.
5. Verifica l'impronta SSH del server; anche il comando di controllo è copiabile.
6. In Coolify crea **un unico token READ + DEPLOY** senza permessi Root/Write. La verifica iniziale è solo lettura.
7. Verifica e salva: CDM genera il tuo EXE personale, che si collegherà autonomamente senza dipendere dall'alias del PC.

Il tunnel SSH richiede che Windows disponga del client OpenSSH; non richiede certificati SSL pubblici o DNS.

## Dashboard e deploy

Le applicazioni sono suddivise in staging, produzione e non classificate. La selezione dei progetti è una tendina con ricerca. Le card staging hanno checkbox per **Deploy selezionati**; esiste anche **Deploy TUTTI gli staging** nel progetto filtrato. Entrambe le azioni richiedono conferma. Clic destro su una card: **Blocca posizione** o **Blocca deploy**. Drag & drop con zona vuota per spostare una card in fondo alla stessa sezione. Non esiste ancora una griglia liberamente vuota e persistente.

La splash animata e l'icona ufficiale sono incorporate nell'EXE.

## Compilazione Windows

Scarica `CDM_Logo_Assets.zip` nella cartella Download di Windows, senza estrarlo. Da PowerShell, nella cartella repo:

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

Vengono eseguiti test locali e compilato `portable\CDM.exe`. **Mai GitHub Actions**. La compilazione **non aggiorna automaticamente il clone personale**.

Per aggiornare un clone, chiudilo e usa il nuovo `CDM.exe`:

```powershell
$clone = (Resolve-Path '.\portable\CDM-NomeUtente-NomeServer.exe').Path
Start-Process -FilePath '.\portable\CDM.exe' -ArgumentList ('--upgrade-clone "{0}"' -f $clone) -Wait
```

Sostituisci il nome di esempio con quello effettivo; viene creato un backup contenente anche le credenziali.

## Stato al 31 ottobre 2026

**Verificato su Windows:** build locale, clone personale, tunnel SSH indipendente, API Coolify e inventario di 30 applicazioni, verifica degli asset grafici, test di aggiornamento di clone temporaneo.

**Da verificare prima del rilascio:** deploy reale di uno staging, tutti i controlli WPF aggiornati e portabilità su un secondo PC.

La documentazione tecnica completa è in [README.md](README.md), [CDM_SETUP_GUIDE.md](CDM_SETUP_GUIDE.md), [BUILDING.md](BUILDING.md) e [DEVELOPER_NOTES.md](DEVELOPER_NOTES.md).

## Scaricare CDM facilmente

Dopo il rilascio, la pagina ufficiale sara [GitHub Releases](https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases): bastera scaricare il generico `CDM.exe` oppure il pacchetto ZIP con guida italiana. Non esiste ancora una release pubblicata. `PACKAGE_RELEASE.ps1` prepara i file per il caricamento manuale, senza pubblicarli. Vedi [RELEASE_GUIDE.md](RELEASE_GUIDE.md) e il [punto del 31 ottobre](PROJECT_STATUS_2026-10-31.md).

## 🍺 Offri una birra a Cris0338!

CDM è **completamente gratuito**. Se ti piace e vuoi sostenere il suo sviluppo, puoi scegliere liberamente qualsiasi importo, senza alcun obbligo:

**[Offri una birra tramite PayPal.Me](https://www.paypal.com/paypalme/Cris0338/)**

Il messaggio è tradotto nella finestra «Informazioni» dell'applicazione secondo la lingua selezionata. Il pulsante Sponsor di GitHub porta alla stessa pagina. Non è richiesto alcun pagamento per utilizzare, scaricare o condividere CDM.
