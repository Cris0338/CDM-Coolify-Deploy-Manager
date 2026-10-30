# Coolify Deploy Manager — v2.0.3 · 2026-10-30

## Correzione compilazione WPF

- Aggiunto `using System.IO;` in `Services/SettingsStore.cs`: ripristina i simboli `Path`, `File`, `Directory`.
- Aggiunto `using System.IO;` in `Services/SshTunnel.cs`: ripristina `IOException`.
- Aggiunto `using System.IO;` in `MainWindow.xaml.cs`: ripristina `IOException`.
- Confermato `using System.Net.Http;` in `Services/CoolifyApi.cs` dalla v2.0.3.
- Versione assembly aggiornata a 2.0.3.

## Invariato

- Test di classificazione (staging e release/produzione).
- API Coolify, deploy e monitoraggio.
- UI WPF con card e drag & drop.
- Tunnel SSH automatico.
- Credenziali protette localmente via Windows DPAPI.

## Stato dei test

- Controllo statico su struttura del progetto e XML XAML eseguito.
- Non è disponibile un compilatore WPF Windows nell'ambiente di preparazione; compilare su Windows prima dell'uso.
