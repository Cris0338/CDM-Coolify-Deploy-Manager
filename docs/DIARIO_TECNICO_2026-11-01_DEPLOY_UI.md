# 2026-11-01 — CDM 2.2.2 (sviluppo) — Stato deploy sulla card e drag non bloccato

## Richiesta sul campo

Durante un deploy reale l'utente desidera riconoscere sulle singole card che Coolify sta ancora lavorando, tramite un cerchietto animato **collegato a dati veri**, e spostare le card mentre il deploy procede. Nel test di utilizzo della release 2.2.1 il drag è stato impedito durante il deploy e poteva essere eseguito soltanto dopo la fine.

## Analisi basata sul codice di 2.2.1

- `CoolifyApi.StartDeployAsync` usa `POST /api/v1/deploy` e ottiene `deployment_uuid` reale dal risultato API.
- `DeployQueueAsync` chiama già `GET /api/v1/deployments/{uuid}` ogni quattro secondi per monitorare `status` fino a `finished` o `failed/cancelled/error`. **Non serve inventare percentuali o interrogare nuove API.**
- `MainWindow.CanReorderCards` era `!busy && ...`. Il flag `busy` copre l'intero deploy fino alla fine, impedendo di iniziare il drag. Il layout e il deploy sono operazioni indipendenti; il limite era una policy dell'interfaccia, non una restrizione dell'API.

## Implementazione nel branch isolato

Branch: `feature/live-deploy-card-status-20261101` (non ancora mergiato). Versione di sviluppo `2.2.2`; **la release pubblicata v2.2.1 e i relativi asset restano intatti**.

| Area | Intervento | Commit |
| --- | --- | --- |
| `DeploymentActivity.cs` | Stati API positivamente identificati come attivi: `queued`, `pending`, `waiting`, `starting`, `in_progress`. Sconosciuto/terminale non anima. | `d8af714` |
| `ViewModels/AppCardView.cs` | Stato live e proprietà WPF `IsLiveDeploymentActive` con `PropertyChanged`. | `9b9fa4f` |
| `MainWindow.xaml` | Arco visibile e ruotante nella card soltanto con `IsLiveDeploymentActive=true`; tooltip = stato Coolify reale. | `31a6bfe` |
| `MainWindow.xaml.cs` | Avvio animazione solo dopo POST accettato con UUID; aggiornamento per ogni risposta GET ogni 4 s; stop su stato terminale o fine monitoraggio. Nuovo flag `inventoryRefreshing` per bloccare il drag solo quando si ricostruiscono le card, lasciandolo disponibile durante il deploy. | `922ddfa` |
| `App.xaml.cs` | Test offline `--verify-live-deploy-ui` per stati del server, notifica binding e policy drag. | `4432330` |
| `BUILD_PORTABLE.ps1` | Nuovo smoke del binario pubblicato, senza accesso a Coolify reale. | `9c7ebfb` |
| `CoolifyDeployManager.csproj` | Versione di sviluppo 2.2.2, distinta dal tag 2.2.1. | `125f150` |

## Limiti espliciti

- Il cerchietto segue i **deploy avviati e monitorati da CDM**. Non è una percentuale fittizia; non rappresenta automaticamente job avviati da altri client senza un'ulteriore sincronizzazione.
- Il comando **Stop Queue** interrompe il monitoraggio locale e non annulla necessariamente il job remoto di Coolify. Il cerchietto non rimane a girare se il controllo del suo stato si interrompe; il deploy remoto va ricontrollato.
- Il blocco di Configurazione/Refresh/avvio di ulteriori deploy durante una coda attiva è rimasto invariato per evitare conflitti di connessione e operazioni concorrenti.
- Una card con **Posizione bloccata** e i filtri/search attivi continuano a impedire lo spostamento come previsto.
- I test statici su GitHub: rilevati riferimenti coerenti fra XAML, ViewModel, policy e test; struttura XAML bilanciata. **Non è ancora stata eseguita la compilazione WPF su Windows né la prova di drag durante un deploy reale**.

## Prossimo passo

L'utente deve collaudare separatamente la versione 2.2.2 da branch (build offline e interfaccia), **senza aggiornare automaticamente il proprio clone 2.2.1**. Dopo i PASS e un test manuale di una card in deploy con drag delle altre card, decidere se e quando pubblicare 2.2.2. Nessuna pubblicazione o modifica al VPS eseguita in questa sessione.

## 2026-11-01 — Terminale dedicato alle comunicazioni, niente eventi UI

**Segnalazione utente:** trascinare una card produceva `Ordine delle card salvato` nel terminale; il terminale deve riguardare solo quello che esce ed entra dall'applicazione, ossia traffico Coolify/SSH e diagnostica di tali comunicazioni. È una richiesta separata dallo spinner e dal drag durante il deploy.

**Root cause:** `MainWindow.xaml.cs` chiamava `AddLog(Loc.T("OrderSaved"))` da `CommitCardMove`, e `AddLog(Loc.T("LockedPositionPrevented"))` in caso di drag non consentito. Ulteriori eventi puramente locali finivano nello stesso registro: avvio con ID build, classificazione interna dei progetti sconosciuti, configurazione salvata e pressione di Stop Queue. Non sono richieste/risposte Coolify e non devono comparire nella console dedicata.

**Modifica** `038519532d69519a088ad6b8527e0a0338cc18c9`: rimossi i sei punti di registrazione di attività interna. Per spostamenti bloccati, il motivo è mostrato nella riga di stato `OperationText` soltanto quando nessun deploy è in corso, evitando di sovrascrivere il nome del deploy attivo. L'ordinamento e il salvataggio persistente della card sono invariati, così come registro SSH, interrogazioni API, stati remoti e problemi di comunicazione.

**Regression guard** `a1539c255643584377a19c97e4e1f1bc5567014e`: `BUILD_PORTABLE.ps1` rifiuta una build quando le sei chiamate `AddLog` a eventi interni vengono reintrodotte; stampa `PASS: card reordering, local locks and settings events do not pollute the communication terminal.` se assenti.

**Stato dei test:** modifica e controllo statico inseriti su branch `feature/live-deploy-card-status-20261101`; ancora da verificare mediante build reale Windows e prova di drag durante un deploy. Nessuna modifica alla release `v2.2.1`, al VPS, al clone personale o ai suoi backup.

**Affinamento** `6a5755655faa0f81dd0f43706aec13110ae15f0d`: esclusi dal terminale anche `DeploySkippedLocked`, `LastStatusLog` (fine coda locale prima del refresh) e `StatusWaiting` (annullamento locale). Il deploy saltato viene comunicato in `OperationText`, non nel registro di rete; stop locale e fine coda sono già indicati in `OperationText`. Il log continua a mostrare le effettive richieste/risposte API e SSH, lo stato restituito da Coolify, le letture di health e i relativi errori. Aggiornato test di non-regressione in `BUILD_PORTABLE.ps1`, commit `95695c0b076514d9761d54460ed210cb11ce9992`: ora controlla **nove** pattern di messaggi UI/locali vietati.

**Nota di chiusura di questo passo:** non abbiamo ancora una build Windows su questo branch; prima verifica in esecuzione ancora necessaria. Le modifiche al codice sono solo nel branch 2.2.2, non nella pre-release pubblicata.

## 2026-11-01 10:53 CET — Windows build e test offline della 2.2.2: PASS

L'utente ha compilato dal branch `feature/live-deploy-card-status-20261101` con `powershell -NoProfile -ExecutionPolicy Bypass -File .\\BUILD_PORTABLE.ps1`; nel momento della build il commit di codice era `46b88be3c94e8b31d56092a198e4a85e9cd8de59`, build UTC `2026-11-01T09:53:56Z`, versione `2.2.2`. `dotnet publish` Windows x64 WPF: **riuscito** (9,3 s riportati). `portable/CDM.exe` generico SHA256 `E363C98E6927B07C09E43F0E629EA58C3438349255FC9B88553DC4ADF905123C`, dimensione ~64,56 MiB.

**Risultati riportati:** 8/8 classificazione; 14 cataloghi locale coerenti, IT/FR/EN 227/227; profili embedded e upgrade di cloni disposable; URL private/https e rifiuti HTTP pubblici; File.Replace Windows; verifica hash logo/ico; template EXE vuoto; build metadata; health card; SSH recovery policy; salvataggio deferred `.pending` e backup; **nuovo smoke stato del cerchietto basato sullo stato Coolify e drag consentito mentre la rete è impegnata: PASS**; **nuovo guard di assenza di messaggi UI nel terminale: PASS**.

Il compilatore ha avvisato della presenza di un clone reale `CDM-Cris0338-coolify.exe` e di due backup privati già esistenti (`20261031-224602.bak` e `before-save.bak`): **non è un errore e non vanno pubblicati o sostituiti automaticamente**. Nessuna distribuzione della 2.2.2 è stata creata. La release `v2.2.1` resta invariata.

**Prossimo test:** chiudere il clone corrente per evitare concorrenza sul tunnel SSH; fare una copia di prova locale del clone, aggiornare *solo la copia* usando il template 2.2.2, quindi verificare connessione, drag e animazione durante un vero deploy di staging. Confermare il risultato prima di proporre una eventuale release 2.2.2. L'aggiornamento di questo diario è documentale e successivo alla build; non richiede ricompilazione del medesimo codice.

## 2026-11-01 — Clone locale 2.2.2 di collaudo creato: PASS

L'utente ha confermato da PowerShell: `PASS: clone di test CDM 2.2.2 pronto`. È stata copiata la versione personalizzata in un nuovo clone di prova e il template generico 2.2.2 l'ha aggiornata con `--upgrade-clone-silent` (exit code 0). Il clone di prova è locale ed escluso da Git; la versione personale originale 2.2.1 e la release GitHub v2.2.1 restano inalterate. I dati di connessione sono incorporati nel clone di prova ma non sono stati registrati o caricati.

**Stato:** preparazione clone PASS; avvio reale della UI, connessione, spinner durante deploy reale, drag durante deploy e assenza di log UI rimangono da verificare. Prossimo passo: avviare solo il clone di prova e osservare connessione e dashboard; usare una risorsa di staging soltanto dopo una verifica preliminare.

## 2026-11-01 — Prima prova UI reale del clone di test

L'autore riferisce **«sembrerebbe ok»** dopo aver avviato il clone locale 2.2.2 e aver ricevuto le istruzioni per controllare versione, connessione Coolify, card e assenza di messaggi di riordino nel terminale. Registrato come **riscontro preliminare positivo**, non come validazione puntuale dei quattro controlli né come PASS del deploy reale.

**Prossimo controllo:** avviare dalla UI un deploy su una risorsa di staging selezionata dall'autore; durante il deploy verificare animazione nella card interessata, trascinamento di una card diversa non bloccata e terminale riservato a richieste/risposte API/SSH. Alla conclusione osservare che la rotazione si fermi. Nessun comando di deploy viene avviato dall'assistente.

## 2026-11-01 11:02 CET — Regressione confermata: crash durante il drag con deploy attivo

**Risultato del primo collaudo reale sul clone di prova v2.2.2:**
- Il deploy è partito e il **cerchietto associato a Coolify funziona** (osservazione diretta utente: `deploy, cerchietto ok`).
- **Trascinamento di una card durante il deploy → crash del software.** Quindi l'accettazione del drag concorrente è **FAIL**; i precedenti smoke offline NON coprono la situazione WPF reale. Non dichiarare completata la funzionalità.
- Al successivo avvio le connessioni SSH gestite falliscono: `Local port 18000 is already in use. Close the old SSH tunnel or choose another port.`. Log include `Managed SSH tunnel stopped: No managed OpenSSH process.`, nuova riconnessione fallita con porta occupata. Probabile processo OpenSSH rimasto dal precedente processo CDM dopo terminazione improvvisa: **da identificare con PID e proprietario prima di chiuderlo**. Potrebbe anche essere un altro listener; nessuna terminazione automatica senza prova di identità.
- Il deploy remoto può proseguire su Coolify malgrado il crash del client; **non ripetere il POST di deploy solo per riottenere la connessione**.

**Indagine tecnica preliminare:** il drag WPF usa `DragDrop.DoDragDrop` (modal/nested dispatcher loop), un adorner con `RenderTargetBitmap.Render(card)` e, al `Drop`, `SettingsStore.Save(settings)` seguito da `RenderCards()`. Il loop del monitor server aggiorna lo stato della card ogni quattro secondi durante l'operazione drag. Il fatto che il conflitto sia temporalmente associato non prova quale operazione abbia generato l'eccezione. **Recuperare l'evento Windows .NET Runtime / Application Error con stack trace** e l'identità del listener porta 18000 prima di decidere la correzione. Il codice 2.2.1 e la release GitHub non sono stati modificati.

**Prossimo passo:** PowerShell di diagnostica solo-lettura per elenco listener porta 18000 + eventi Application recenti, poi correzione mirata e nuova build di prova. Documentare i test dopo il fix.

## 2026-11-01 — Root cause del crash e patch WPF (compilazione Windows ancora da fare)

**Evidenza diagnostica ottenuta da Windows Application Event Log, ore 11:00:42:**
- `Application Error` ID 1000, eseguibile `CDM-Test-2.2.2.exe`, versione 2.2.2.0; CLR exception `0xe0434352`.
- `.NET Runtime` ID 1026: `System.Windows.Markup.XamlParseException` con `System.InvalidOperationException: Impossibile animare '(0).(1)' in un'istanza di oggetto non modificabile.` Stack: `Storyboard.ProcessComplexPath`, `BeginStoryboard.Begin`, `StyleHelper.InvokeApplicableDataTriggerActions`, `FrameworkTemplate.LoadTemplateXaml`. L'animazione dichiarata nella `Path.Style` della DataTemplate tentava di animare un `RotateTransform` WPF congelato al momento di ricreare le card dopo un drag. **Causa confermata; non è un errore Coolify.**
- Porta locale `18000` in LISTEN: `ssh.exe` PID **33596**, ParentPID **10628**, avvio 10:58:10. Il processo CDM del crash è `0x2984` = **10628** in decimale. La corrispondenza dimostra un SSH figlio del CDM terminato bruscamente, rimasto orfano. Non terminare processi SSH indiscriminatamente; identificare per PID, parent PID, porta e `-L 127.0.0.1:18000:` prima di chiuderlo.

**Correzione sorgente isolata su `feature/live-deploy-card-status-20261101`:**
- `80ccf5f` `MainWindow.xaml`: eliminato lo `Storyboard` sul `RotateTransform` della DataTemplate; mantenuto il binding `IsLiveDeploymentActive` alla proprietà `Visibility` del cerchietto; agganciati gli eventi `Loaded`, `IsVisibleChanged`, `Unloaded`.
- `e67c729`, `98aa277` `MainWindow.xaml.cs`: istanziato un `RotateTransform` **mutabile e dedicato** per ciascun `Path` reale a runtime; `BeginAnimation` diretto solo per card visibili; stop su chiusura/visibilità falsa; alias `ShapePath` per non confondere System.IO.Path. Il cerchietto rimane governato dallo stato API, non da un timer simulato.
- `fc54928` `MainWindow.xaml.cs`: aggiunto test WPF in modalità senza rete `VerifyLiveSpinnerRetemplateForSmoke()`. Apre la finestra senza refresh o connessione, crea due vere card WPF, alterna ripetutamente l'ordinamento con spinner attivo, controlla che il transform non sia frozen e che l'animazione si fermi allo stato `finished`. Questo copre il percorso WPF assente dal test dei soli viewmodel che non aveva rilevato la regressione.
- `8e6874e` `App.xaml.cs`: nuova opzione isolata `--verify-live-spinner-retemplate` del generico; `98afae9` `BUILD_PORTABLE.ps1`: avvia lo smoke con timeout di 30 secondi e pretende il marker PASS.

**Stato:** patch e nuovo smoke sono sul branch di sviluppo; **non dichiarare la correzione collaudata finché la compilazione Windows e lo smoke reale WPF non danno PASS**. Nessun deploy è stato rilanciato, nessun EXE o processo SSH dell'utente toccato, nessun commit al tag v2.2.1.

## 2026-11-01 — Recupero riuscito della porta 18000 e del tunnel SSH

Dopo il crash WPF, l'utente ha eseguito la procedura di liberazione mirata della porta `18000`, verificando proprietario del socket e comando SSH prima della terminazione del solo vecchio `ssh.exe` PID `33596`, figlio del processo CDM crashato PID `10628`. Il comando prevedeva poi l'avvio del clone personale stabile `CDM-Cris0338-coolify.exe` (2.2.1) che ricostruisce il tunnel gestito. **Conferma esplicita dell'utente: «ok... funziona».** L'accesso SSH/Coolify è quindi recuperato e il blocco operativo porta 18000 è chiuso.

Restano aperti **il crash e la sua correzione 2.2.2 non ancora verificata su Windows**. Non ripetere il deploy per questo recupero; la versione 2.2.1 è stata usata senza modifiche. Prossimo passo: recuperare il branch aggiornato, ricompilare 2.2.2 e superare il nuovo smoke WPF prima di riprovare a spostare card durante il deploy.

## 2026-11-01 11:41 CET — Build 2.2.2 FALLITA per errore di sintassi; patch sul branch

**Input:** log completo fornito dall'autore della build Windows `powershell -NoProfile -ExecutionPolicy Bypass -File .\\BUILD_PORTABLE.ps1`, compilazione da `7ccacf6eeb1d` con build UTC `2026-11-01T10:40:30Z`. Gli smoke iniziali (policy terminale, logo, classificazione 8/8, localizzazione, profilo, URL e `File.Replace`) sono PASS; compilazione .NET WPF Windows fallita con **83 errori** a cascata, a partire da `MainWindow.xaml.cs(502,65)`, `CS1002`/ `CS1519`. Questo **non** è un risultato PASS e non genera un nuovo binario verificato.

**Diagnosi sorgente esatta:** la precedente sostituzione automatica dell'alias WPF `ShapePath` ha introdotto la firma sintatticamente errata `private static void SetLiveDeploymentSpinner(ShapePath path), bool shouldSpin)`. La parentesi chiusa prima della virgola fa interpretare erroneamente tutti i metodi successivi.

**Correzione commit `bb8e1e644e7191dab274d24c5dc2ec18d1db445b`:** rimosso solo il carattere errato dalla dichiarazione; firma ora `private static void SetLiveDeploymentSpinner(ShapePath path, bool shouldSpin)`. Nessun cambiamento alla logica WPF o SSH. Non affermare che tutto compili finché il proprietario non ripete `dotnet publish` via `BUILD_PORTABLE.ps1` e mostra i risultati. Il clone stabile 2.2.1, il vecchio clone test e i relativi token non vengono modificati da questo commit.

**Prossimo passo:** `git pull --ff-only` sul branch di sviluppo e ripetere build su Windows. Verificare in particolare il nuovo smoke `PASS: real WPF card retemplate during live spinner does not crash or freeze transforms.`; non effettuare deploy finché non è PASS.


## 2026-11-01 — Seconda compilazione Windows: errore CS0136

L'autore ha eseguito `git pull --ff-only` sul branch passando da `7ccacf6` a `f6bdecf` e ha ripetuto `powershell -NoProfile -ExecutionPolicy Bypass -File .\\BUILD_PORTABLE.ps1`. La prima correzione della firma C# ha eliminato gli 83 errori a cascata: gli stessi controlli preliminari PASS (classificazione 8/8, 14 cataloghi, configurazioni, profilo, sicurezza URL). La compilazione `net10.0-windows win-x64` dal commit `f6bdecf63ca7`, UTC `2026-11-01T10:43:46Z`, è però **fallita con un solo errore**: `App.xaml.cs(249,25): error CS0136`.

**Causa:** il nuovo ramo CLI di test WPF dichiarava `var dashboard = new MainWindow ...` nello stesso metodo in cui il ramo ordinario successivo dichiara `var dashboard = new MainWindow()`. C# impedisce questo riuso del nome nella struttura degli scope esistenti, anche se il codice di test restituisce prima dell'avvio ordinario.

**Correzione applicata direttamente nel branch:** commit `146d7b3670fbb86a4aedae84d4b55b953895849e`. Rinominata la sola variabile locale del test `dashboard` in `smokeDashboard` e tutti i suoi riferimenti; nessuna modifica a tunnel SSH, logica deploy o clone personale. **Compilazione e test WPF restano da verificare su Windows**; non presentare come PASS.

**Prossimo passo:** `git pull --ff-only`, poi `BUILD_PORTABLE.ps1` e osservare il nuovo smoke WPF retemplate in esecuzione. La 2.2.1 rimane invariata e in uso con il tunnel SSH funzionante sulla porta 18000.

## 2026-11-01 — Compilazione Windows definitiva della patch WPF: PASS

L'autore ha compilato il branch `feature/live-deploy-card-status-20261101` con `BUILD_PORTABLE.ps1`. **Sorgente compilato**: `48c87cf6df3d`; `Built 2026-11-01T10:46:03Z`; versione `2.2.2`. `dotnet publish` WPF `net10.0-windows win-x64` **PASS** (7,8 s); `portable/CDM.exe` generico, 64,56 MiB, hash SHA256 **`12F5B9F44EF25E749BA40B8C56D13BE10B7326037EE23A11515AB56045AB5DEF`**. L'hash corrisponde all'output appena compilato.

**Tutti i test del build script PASS**, inclusi 8/8 classificazione, localizzazione 14 lingue (IT/FR/EN complete), profilo embedded/cloni sintetici, URL SSH/HTTPS, File.Replace, log di sole comunicazioni, versione/Git/UTC e stato deploy per card. Crucialmente: **`PASS: real WPF card retemplate during live spinner does not crash or freeze transforms.`** Il test apre e ricrea realmente le card WPF mentre lo spinner è animato e ne controlla l'arresto quando lo stato diventa `finished`. Superata la regressione che aveva causato eccezione da transform Freezable non modificabile. Smoke separati health, tunnel SSH recovery, creazione/upgrade clone sintetico e save deferred `.pending`: tutti PASS.

**Nota su file locali rilevati dallo script**: dentro `portable/` ci sono il clone stabile `CDM-Cris0338-coolify.exe` con backup, il vecchio clone di test `CDM-Test-2.2.2.exe` con backup, e **`.CDM-Test-2.2.2.exe.pending`**. Questi file non sono stati aggiornati dalla compilazione generica, né caricati su GitHub. Il `.pending` va esaminato prima di aggiornare il clone di prova, evitando di perderne le modifiche o sovrascriverlo mentre è attivo.

**Stato di accettazione:** build e smoke WPF **PASS**, ma **non** ancora confermato il drag effettivo di una card durante un deploy Coolify reale, lo stop dell'animazione al termine del lavoro reale, la persistenza dell'ordine e la pulizia terminale con questa build. **Nessuna release GitHub 2.2.2 pubblicata e nessuna modifica alla release stabile v2.2.1.**

**Prossimo passo:** verifica locale non distruttiva sul file `.pending` e gli eseguibili in esecuzione; solo dopo, aggiornare esclusivamente il clone di test e fare il collaudo manuale con un unico CDM/tunnel attivo.

## 2026-11-01 — Inventario locale del clone test e del file di salvataggio sospeso

L'utente ha eseguito il controllo solo-lettura di processi CDM attivi e dei file del clone. Nel risultato **nessun processo CDM elencato**, quindi nessuna UI CDM risultava in esecuzione al momento della lettura. File presenti:

- `portable/CDM-Test-2.2.2.exe`: 67 693 634 byte, ultimo aggiornamento `01/11/2026 10:57:35`.
- `portable/.CDM-Test-2.2.2.exe.pending`: 67 693 634 byte, ultimo aggiornamento `01/11/2026 11:00:42`, coincidente con la registrazione del crash WPF. Dimensioni uguali non significano contenuti o profilo uguali.

Il sorgente `SettingsStore.Save` genera `.pending` come **intero eseguibile personalizzato** tramite `ExecutableProfile.Build`, includendo impostazioni, ordine card ed eventuali credenziali; il normale aggiornamento viene programmato all'uscita, mentre un crash può lasciare il `.pending` recuperabile (`Services/SettingsStore.cs`, righe 53–67). Il comando `--upgrade-clone-silent` accetta un clone `.exe`, verifica il profilo, costruisce il nuovo EXE generico 2.2.2 conservando le credenziali e crea un backup senza accedere a Coolify.

**Decisione operativa:** non eliminare `.pending` e non sovrascrivere il vecchio clone; preparare un **nuovo clone di prova locale** a partire da una copia del `.pending`, aggiornando soltanto questa copia con il template compilato che ha superato gli smoke. Conserverà così l'eventuale ultimo ordinamento salvato al momento del crash, senza alterare gli altri due file. Evitare avvii contemporanei con CDM personale sulla porta 18000. Test UI manuali ancora da eseguire.

## 2026-11-01 — Clone recuperato dalla copia di .pending: PASS

L'utente ha eseguito il comando di ripristino non distruttivo: copia di `portable/.CDM-Test-2.2.2.exe.pending` in `portable/CDM-Test-2.2.2-Recovered.exe`, previa conferma dell'hash SHA256 del template generico 2.2.2 `12F5B9F44EF25E749BA40B8C56D13BE10B7326037EE23A11515AB56045AB5DEF`; aggiornamento con `CDM.exe --upgrade-clone-silent` e verifica del codice di uscita. **Riscontro dell'utente:** `PASS: clone recuperato e aggiornato alla build 2.2.2`.

La procedura non avvia software o nuovi tunnel, conserva l'originale `CDM-Cris0338-coolify.exe`, il precedente clone `CDM-Test-2.2.2.exe` e il file `.pending`. Il clone recuperato include una copia delle credenziali e non va caricato su GitHub. Il backup creato dal tool upgrade resta locale.

**Ancora da verificare:** avvio del clone recuperato, UI WPF su card attiva, assenza di crash con riordino, corretto arresto del cerchietto dopo deploy reale e assenza di righe locali nel log. Prima dell'avvio assicurarsi che nessun altro CDM o `ssh.exe` stia occupando la porta 18000; non terminare automaticamente processi sconosciuti. Nessuna release 2.2.2 pubblicata.

## 2026-11-01 12:11 CET — Accettazione sul campo 2.2.2: deploy e drag simultaneo PASS

**Versione collaudata:** clone locale `portable/CDM-Test-2.2.2-Recovered.exe`, generato dalla copia salvata durante il crash e aggiornato dal template compilato con sorgente `48c87cf6df3d`; SHA256 del template generico `12F5B9F44EF25E749BA40B8C56D13BE10B7326037EE23A11515AB56045AB5DEF`. L'autore conferma direttamente: **«fatto sia senza che con il deploy... tutto ok... animazione ok... finito ok»**.

**Verifiche manuali PASS:**
- Riapertura del clone, avvio tunnel gestito alla porta locale `18000`, health SSH confermato alle 12:09:32; lettura inventario 8 progetti / 30 applicazioni, zero storici non disponibili.
- Riordino manuale card a deploy inattivo **PASS**.
- Riordino manuale card durante un deploy reale **PASS**, senza crash WPF; il precedente problema `XamlParseException` da `RotateTransform` congelato non si manifesta più in questa prova.
- Spinner della card durante l'attività Coolify **PASS**; scomparsa/stop a fine lavoro **PASS**.
- Log terminale focalizzato su chiamate/risposte API e connessione SSH, senza messaggi relativi allo spostamento locale delle card **PASS**.

**Deployment Coolify osservato:**
- Destinazione: `CHATAIDE-V2 / chataide-v2:staging/users-staging`.
- `12:09:55`: richiesta `POST /deploy`, Coolify ha accodato e restituito un identificativo.
- `12:09:59`: `in_progress` (commit `HEAD`).
- `12:11:07`: `finished`, commit effettivo `976106bd36608d1f297e0573ecf1c0c757c14e0d`.
- `12:11:07`: `OK: deploy terminato. Health check non verificato separatamente.` **Non** confondere la fine del job con una verifica indipendente di health.
- `12:11:08`: nuovo inventario 8 progetti / 30 applicazioni, zero storici mancanti.

**Conclusione:** funzionalità richieste per lo stato live sulla card, riordino durante deploy e pulizia del terminale **accettate sul campo** nella build di test 2.2.2. Rimangono non ancora collaudati: persistenza dell'ordine dopo chiusura/riapertura, stop queue/disconnessione e test su altro PC. Nessuna modifica a codice o credenziali, nessun tag o release 2.2.2 pubblicata: prossima decisione esplicita dell'autore se effettuare il test persistenza e/o procedere alla distribuzione di una release **generica** senza credenziali. La pre-release 2.2.1 rimane invariata.

## 2026-11-01 — Nuova funzionalità: avvio con Windows nell'area notifiche (codice, test da eseguire)

**Nuova richiesta dell'autore:** dopo aver confermato che l'ordine delle card **persiste anche alla riapertura** (PASS su 2.2.2), aggiungere l'opzione `Avvia con Windows, ridotto nell'area notifiche` accanto all'orologio. Nessun deploy aggiuntivo richiesto.

**Implementazioni sul branch isolato `feature/live-deploy-card-status-20261101` (versione 2.2.2 ancora NON pubblicata):**

- `Services/WindowsStartup.cs` (commit `4ace2e3`): chiave `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`, valore univoco `CDM-Coolify-Deploy-Manager`; comando `"percorso eseguibile personale" --startup-tray`; scrittura/lettura/rimozione solo su esplicita scelta, privilegi amministrativi non necessari, nessuna credenziale nel registro. Attivando un clone sostituisce l'eventuale altro clone registrato: una sola voce per utente. Disattivando un clone non cancella la voce appartenente a un altro clone.
- `Services/SettingsStore.cs` (`ec281f9`): supporto per applicare la preferenza opzionale `startWithWindows` **soltanto al salvataggio esplicito delle Impostazioni**. Le modifiche all'ordine delle card e gli aggiornamenti del file `.pending` non riscrivono il registro di Windows. Per un nuovo clone generico l'eventuale avvio automatico punta al nuovo eseguibile personale.
- `SettingsWindow.xaml`, `SettingsWindow.xaml.cs`, `MainWindow.xaml.cs` (commits `19b702f`, `d85c5b9`, `5fdc8ff`): checkbox tradotta, lettura dell'effettiva registrazione della *copia corrente*, passaggio della scelta al salvataggio.
- `MainWindow.xaml.cs` (`4a9f88e`, `e78d603`): iconcina nella system tray basata sull'icona ufficiale del file CDM; nascondimento e rimozione dalla taskbar quando si minimizza, ripristino su doppio clic e dal menu contestuale, comando Esci (chiusura reale e stop del tunnel SSH), cleanup dell'icona su chiusura; aggiornamento testi dopo cambio lingua.
- `Services/SingleInstanceGate.cs` (`86553fe`): mutex ed evento nominati per percorso del clone (SHA256 del solo percorso). Un secondo avvio del medesimo `.exe` invia un segnale per aprire la prima finestra anziché lanciare un secondo tunnel. I differenti cloni non condividono automaticamente la mutex.
- `App.xaml.cs` (`1854e30`): la CLI `--startup-tray` di un clone personalizzato salta lo splash e crea la finestra nascosta, lasciando attivo caricamento/connessione SSH; l'avvio manuale normale conserva lo splash precedente. Il CDM generico non usa la modalità nascosta senza un profilo.
- `CoolifyDeployManager.csproj`, `GlobalUsings.cs` (`9c1b7e4`, `b7e0373`, `a9ed928`): abilita WinForms NotifyIcon in WPF, ma disabilita gli import impliciti WPF/WinForms per evitare ambiguità di `.NET 10`, reintroducendo soltanto i global imports .NET standard.
- `Localization/{en,fr,it}.json`, `MainWindow.xaml`: traduzioni complete per checkbox, testo esplicativo, Apri, Esci e tooltip per riduzione nell'area notifiche. Le altre 11 lingue conservano il fallback inglese.
- `App.xaml.cs` (`35d6716`, `a9efd92`), `BUILD_PORTABLE.ps1` (`ef4c27a`): nuovo smoke `--verify-tray-ui` per comando Windows Run *soltanto in forma stringa*, collisione di due istanze e relativa segnalazione, comportamento reale di minimizzazione/ripristino con una finestra WPF. **Nessuna scrittura al registro, nessuna rete, nessun deploy, nessun vero tunnel.**

**STATO ATTUALE: CODICE COMMITTATO; NON ANCORA COMPILATO O COLLAUDATO SU WINDOWS.** Non presentare test come PASS. CDM personale recuperato e stabile rimane utilizzabile; la release 2.2.1 non viene toccata. Non attivare ancora Windows autostart per nessun clone finché publish e smoke della 2.2.2 non sono PASS. La presenza dell'icona nella tray va validata anche visivamente su Windows 10 e l'attivazione/disattivazione dal pannello Impostazioni va verificata su clone test; controllare la persistenza del vecchio ordinamento card.

**Prossimo passo:** `git pull --ff-only`, `BUILD_PORTABLE.ps1`, esaminare l'eventuale prima diagnostica del compilatore e il nuovo `PASS: Windows tray icon hides/restores WPF...` prima di aggiornare e avviare esclusivamente una copia di test del clone.

## 2026-11-01 — Windows 2.2.2 build PASS: startup to tray

**Build proprietario Windows 10 confermata PASS**: comando `powershell -NoProfile -ExecutionPolicy Bypass -File .\\BUILD_PORTABLE.ps1`; sorgente/commit incorporato **`8585427dff9e`**, build UTC **`2026-11-01T11:39:51Z`**; publish `net10.0-windows win-x64` completo in 10,6 secondi, `portable/CDM.exe` 74,64 MB, SHA256 **`E4F3D9989185694A17C97344C6E014A45CCFE784812586D71C05FB181B41E6E4`**, hash verificato contro l'output appena generato.

**Smoke PASS**: cataloghi e classificatore (prima del publish), icon/logo SHA, profili embedded, link, File.Replace; startup del generico senza credenziali; identity metadata; live spinner e drag; template WPF con spinner live; **`PASS: Windows tray icon hides/restores WPF, validates user autostart command without touching registry.`**; health viewmodel; SSH reconnect policy; creazione/upgrade clone sintetico, migrazione credenziali/lock/ordini, salvataggio differito e backup.

**Osservazione importante**: il test del registro produce soltanto la stringa del comando di avvio; non abilita autostart per l'utente. I test WPF usano una finestra senza API/SSH. **Non ancora verificata sul PC la scelta manuale nelle Impostazioni, la reale comparsa dell'icona nell'area notifiche con app e tunnel attivi, l'istanza unica con vero clone e l'avvio automatico dopo login Windows.** La 2.2.1 resta invariata e non si è pubblicata una release 2.2.2.

**Inventario di file aggiuntivi nel publish directory** (come da output): `CDM-Cris0338-coolify.exe` con backup, clone test `CDM-Test-2.2.2.exe` con backup, clone test recuperato `CDM-Test-2.2.2-Recovered.exe` con backup, `.CDM-Test-2.2.2.exe.pending` originale. Tutti rimasti locali, non includere file personali negli asset release né nel repo.

**Prossimo passo**: aggiornare *solo* `portable/CDM-Test-2.2.2-Recovered.exe` con il generico di hash noto, quando la copia non è in esecuzione, senza interferire con altri tunnel; poi testare minimizzazione, tray context menu, secondo avvio stesso EXE, checkbox HKCU Run on/off (ispezione del percorso soltanto, nessun segreto) e finalmente login Windows solo quando scelto dall'autore.

## 2026-11-01 — Clone di prova aggiornato con build tray 2.2.2: PASS

L'autore ha eseguito sul PC Windows la procedura controllata `--upgrade-clone-silent` sul SOLO eseguibile `portable/CDM-Test-2.2.2-Recovered.exe`, partendo dal generico `portable/CDM.exe` costruito dal commit sorgente `8585427dff9e` e verificato tramite SHA256 `E4F3D9989185694A17C97344C6E014A45CCFE784812586D71C05FB181B41E6E4`. La procedura verificava anche l'assenza del processo di prova attivo e dell'eventuale suo `.pending` prima dell'upgrade. **Conferma esplicita dell'autore:** `PASS: clone di prova aggiornato alla nuova 2.2.2`.

**Esito:** clone di test ora aggiornato alla build contenente le funzionalità Windows startup/tray; backup creato dal tool di upgrade e vecchio clone personale `CDM-Cris0338-coolify.exe` non modificato. Non è stata abilitata alcuna chiave Windows Run; nessuna connessione o deploy avviati dalla procedura. Il clone test contiene credenziali personali e resta locale: non pubblicare in GitHub.

**Prossimo controllo manuale:** verificare che porta locale `18000` e processi CDM non siano occupati da altro programma prima di aprire il clone di prova; eseguire minimizzazione (scomparsa dalla taskbar), comparsa icona nell'area notifiche, doppio clic per ripristinare finestra e menu contestuale Apri/Esci. Successivamente provare doppio avvio stesso clone e impostazione opzionale Windows Run in modo controllato. Nessun nuovo deploy richiesto per queste prove.
