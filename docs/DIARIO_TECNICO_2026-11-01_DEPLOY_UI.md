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
