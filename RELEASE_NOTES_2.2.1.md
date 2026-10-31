# CDM 2.2.1 — Windows portable (pre-release)

**Release candidate tested on Windows 10 x64 — 31 October 2026.**

**Source revision of the owner-tested executable:** `56ec905c0b6894898a78fc9a92eba93b7925593a`. Published generic binary build: CDM 2.2.1, Git `56ec905c0b68`, built `2026-10-31T21:33:56Z`, SHA256 `3037F118C4529256910ECF84A6B29B0AB8F9A7F072C8AB4A77100E81CE6F953B`. Repackaging may produce a different SHA256 or build timestamp with the same source revision: always publish checksums corresponding to the exact files actually uploaded.

## English

**Coolify Deploy Manager 2.2.1 (Windows 10/11 x64).** Free, independent, unofficial desktop dashboard for Coolify. Includes project/application cards, staging deployment confirmation, deployment and position locks, saved card ordering, and managed SSH or HTTPS connectivity. No deployment happens automatically.

**Changes:** self-contained generic EXE and personalized clone lifecycle; fixed deferred save on Windows with recoverable prior-executable backup; managed SSH reconnect after an unexpectedly exited tunnel; clearer health badges and card interactions; About now displays actual version, Git revision and UTC build time.

**Validation:** Windows compilation and offline regression tests passed for a clean generic EXE, disposable clone creation and upgrade, saved card settings and backups, card health updates, metadata and SSH recovery rules. Owner confirmed their personal CDM clone connected to Coolify and the managed SSH tunnel remained stable for more than 14 minutes. **A real staging deployment and full cross-computer portability test are not yet certified**; this is labeled pre-release for that reason.

**Downloads:** only the generic `CDM.exe`, `CDM-2.2.1-win-x64.zip` and `CDM-2.2.1-SHA256.txt` are public. The generic EXE has **no credentials**. **Never share your personalized CDM clone, backup or pending file**: these contain your API token and potentially an SSH private key.

**License:** CDM Free Redistribution License v1.0. Free for personal and professional use and free sharing; resale without authorization prohibited. Not affiliated with Coolify.

**Voluntary support:** https://www.paypal.com/paypalme/Cris0338/ — optional, any amount, no payment required.

## Français

**CDM 2.2.1 pour Windows 10/11 x64.** Gestionnaire indépendant et non officiel de Coolify : tableau de bord, cartes déplaçables, déploiement staging avec confirmation, verrouillages et connexion SSH automatique ou HTTPS.

**Correctifs :** sauvegarde fiable des paramètres dans l'exécutable personnalisé lors de la fermeture, sauvegarde de l'ancienne version, reconnexion automatique du tunnel SSH s'il se termine, affichage de la version, du commit Git et de la date de compilation dans « À propos ».

**Vérification :** compilation et tests automatiques Windows réussis ; tunnel SSH personnel stable pendant plus de 14 minutes. Déploiement staging réel et portabilité entre PC non encore validés : **préversion**.

Téléchargez uniquement l'exécutable générique sans identifiants. **Ne publiez jamais votre exécutable personnalisé ni ses sauvegardes**. Licence : CDM Free Redistribution License v1.0, utilisation et partage gratuits, revente interdite sans accord.

Soutien libre et facultatif : https://www.paypal.com/paypalme/Cris0338/

## Italiano

**CDM 2.2.1 per Windows 10/11 x64.** Applicazione indipendente e non ufficiale per Coolify: dashboard, card riordinabili, deploy di staging con conferma, blocchi persistenti e collegamento SSH automatico o HTTPS.

**Correzioni:** salvataggio delle impostazioni nel clone alla chiusura, backup recuperabile dell'EXE precedente, riconnessione automatica del tunnel SSH se il processo si interrompe, versione reale con commit Git e data di compilazione nelle Informazioni.

**Verifiche:** compilazione e test locali Windows superati; il clone personale si è collegato e il tunnel SSH è rimasto stabile per oltre 14 minuti. Deploy reale di staging e portabilità su un secondo PC ancora da verificare: **pre-release**.

Scarica soltanto l'EXE generico senza credenziali. **Non condividere il clone personale né i suoi backup**. Licenza CDM Free Redistribution License v1.0: uso anche professionale e condivisione gratuiti; rivendita vietata senza autorizzazione.

Sostegno facoltativo: https://www.paypal.com/paypalme/Cris0338/

---

**Release integrity:** attach the generic EXE, ZIP and checksum file produced by `PACKAGE_RELEASE.ps1`, after validating those exact assets. GitHub's automatically generated source archives are not Windows executables.
