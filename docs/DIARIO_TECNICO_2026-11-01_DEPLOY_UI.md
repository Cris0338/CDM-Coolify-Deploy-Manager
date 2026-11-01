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
