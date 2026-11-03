# CDM 3.0.2 — Setup e utilizzo

Questa guida descrive CDM per Windows 10/11 x64. Il programma è indipendente e non ufficiale rispetto a Coolify.

## 1. EXE generico

Avvia `CDM-v3.0.2.exe`, senza configurazioni preesistenti. Wizard e Guida si aprono in finestre indipendenti; puoi scorrere la Guida mentre compili il wizard. L'EXE generico NON include token o chiavi personali.

## 2. Modalità di connessione

- **HTTPS:** usa l'URL HTTPS del tuo server con certificato attendibile.
- **LAN/VPN privata:** una connessione HTTP privata è ammessa se il server è raggiungibile in rete privata.
- **Tunnel SSH automatico:** per Coolify pubblicato in HTTP su VPS, usa il tunnel SSH che inoltra una porta locale Windows (es. `18000`) al servizio Coolify sul loopback VPS (es. `127.0.0.1:8000`). Non inserire `http://` o `:8000` nel campo nome host SSH.

Esempio documentale di IP pubblico: `203.0.113.42`; non è un server reale.

## 3. Creazione dell'accesso SSH limitato

Inserisci l'utente Linux esistente sul VPS, la porta SSH e la destinazione loopback. Premi per generare una nuova chiave Ed25519 dedicata, quindi usa **Copia comando VPS**. Il pulsante mostra un'animazione e il feedback di avvenuta copia.

Nel terminale autorizzato del server (anche da Coolify sul server host, non dentro un container) incolla il comando completo. Il bootstrap crea un nuovo utente `<admin>-cdm` con accesso SSH limitato al solo port forwarding locale verso Coolify. Le policy impediscono sessioni shell e inoltri arbitrari. Lo script non importa chiavi di vecchi cloni né modifica le credenziali amministrative esistenti.

## 4. Impronta del server SSH

Prima di accettare la chiave host, confronta l'impronta SSH SHA256 mostrata dal wizard con quella ottenuta direttamente dal server. Il test **Verifica SSH limitato** controlla anche che non sia possibile aprire una shell remota con l'utente dedicato. Durante il test il tunnel deve autenticarsi correttamente.

## 5. Token Coolify

In Coolify, **Keys & Tokens → API Tokens**, crea un token con soli permessi **READ + DEPLOY**. Non aggiungere ROOT, WRITE o READ:SENSITIVE. Il wizard verifica in sola lettura la raggiungibilità e le risposte API: questo non effettua un vero deploy e non conferma tutte le autorizzazioni richieste per i deploy futuri.

## 6. EXE personale

Dopo la conferma, CDM crea un clone personale con nome basato sul tuo utente/server e suffisso `-v3.0.2.exe`. Il clone incorpora token, eventuale chiave SSH e impronta del server, oltre a lingua e preferenze. **Il profilo non è cifrato:** conserva esclusivamente in privato il clone e qualunque backup, `.bak` o `.pending`.

## 7. Dashboard

La dashboard mostra l'inventario Coolify. Puoi scegliere il progetto, riordinare le card, bloccarne posizione e deploy e seguire l'avanzamento dei deploy avviati esplicitamente. Le attività di pura interfaccia non vengono confuse con i log SSH/Coolify. Il progetto scelto viene ricordato; se non esiste più, viene selezionato l'insieme dei progetti.

## 8. Tray e avvio Windows

La finestra può essere minimizzata nella tray e ripristinata. La gestione delle registrazioni di avvio Windows consente di vedere e rimuovere selettivamente le voci CDM. La voce relativa all'istanza in uso viene segnalata. Non viene eseguita una ricerca indiscriminata di EXE nel computer.

## 9. Aggiornare un clone personale v3

Chiudi il clone anche dalla tray; lancia il generico 3.0.2 con `--refresh-v3-clone` seguito dal percorso del vecchio EXE personale versione 3.0.0 o 3.0.1. Il processo controlla versione reale, nome, profilo incorporato e integrità, e preserva le impostazioni. I cloni 2.x non sono importabili. Non eseguire questa operazione su file di altre persone o di provenienza sconosciuta.

## 10. Perdita del clone personale

Se perdi un EXE personale o un file `.bak`/`.pending`, revoca separatamente il token API in Coolify e la chiave SSH dedicata sul VPS. Per la chiave SSH individua esattamente la chiave pubblica del clone in `authorized_keys` usando l'impronta nota: non eliminare chiavi di altri account. La revoca del token API non revoca la chiave SSH.

## 11. Problemi comuni

| Sintomo | Verifica |
| --- | --- |
| Certificato HTTPS non attendibile | Usa un host HTTPS con certificato valido oppure il tunnel SSH gestito. |
| HTTPS su indirizzo IP restituisce 503 | Controlla che il reverse proxy instradi davvero quel nome host. |
| URL HTTP pubblico rifiutato | Usa il tunnel SSH verso il servizio loopback. |
| `ssh.exe` assente | Installa il componente Windows OpenSSH Client. |
| Errore SSH 255 | Controlla il log OpenSSH, utente, porta, chiave autorizzata e impronta dell'host. |
| Porta locale `18000` occupata | Chiudi il tunnel precedente o scegli una porta locale libera. |
| Chiave privata con ACL troppo permissive | CDM restringe i permessi del proprio file temporaneo mediante `icacls`; verifica l'errore effettivo. |
| Configurazioni v2 non importate | È previsto: la migrazione da v2 è intenzionalmente esclusa. |

Per controllare direttamente l'impronta del server da una sessione autorizzata puoi usare:

```bash
ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256
```

La modalità SSH conserva la gestione dei file temporanei, ma **non** è un deposito cifrato delle chiavi personali: il clone incorpora le credenziali.

## 12. Gestione avanzata dell'avvio automatico Windows

Il pulsante **Gestisci avvio Windows** è presente nella Guida. La ricerca delle registrazioni CDM avviene solo quando apri la modale, non durante la lettura della Guida. La finestra elenca le voci CDM `Run`/`RunOnce` in `HKCU` e `HKLM` (ove accessibili), il comando registrato e la voce **In uso**, se corrisponde esattamente all'EXE aperto. Non cerca eseguibili sull'intero disco.

Puoi selezionare una o più voci e richiederne la cancellazione. La modale chiede conferma e verifica ancora la corrispondenza della registrazione prima della rimozione: non cancella file EXE, intere chiavi di registro o voci di altri programmi. Il salvataggio dell'opzione di avvio automatico sul clone personale usa la propria voce `HKCU`.

## 13. Distribuzione e supporto

Il solo EXE generico è destinato alla release pubblica. Vedi [LICENSE](LICENSE), [BUILDING.md](BUILDING.md), [README_IT.md](README_IT.md) e [SUPPORT.md](SUPPORT.md). La release 3.0.2 non è ancora pubblicata al momento della preparazione di questa documentazione.
