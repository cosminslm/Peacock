# Kit offline HITMAN World of Assassination (Peacock)

Programmi Windows per giocare a **HITMAN World of Assassination** in locale, senza login sui server IOI, usando [Peacock](https://thepeacockproject.org/wiki/intel/installation).

Questi script **non** sostituiscono il patcher e **non** modificano i file del gioco. Orchestrano la release ufficiale di Peacock (server locale + `PeacockPatcher.exe`) e risolvono i tre errori che bloccano l'installazione fatta a mano.

## Il tuo caso (assessment)

| Cosa | Dove | Problema |
| --- | --- | --- |
| Gioco retail `HITMAN3.exe` | `C:\Games\HITMAN - World of Assassination\Retail\` | Build agosto 2023 |
| Peacock 6.5.1 packaged | `C:\Games\HITMAN - World of Assassination\Peacock` | Vecchio, e **non** va messo nella cartella del gioco |
| `Peacock-master.zip` 8.9.1 | `Downloads\Peacock-master` | **Sorgente**, non eseguibile: niente `chunk0.js` / `nodedist` |

Tre cause, in ordine:

1. **Hai scaricato il codice, non una release.** Il pulsante GitHub "Download ZIP" / `Peacock-master.zip` contiene `package.json`, `components/`, i sorgenti C# del patcher. Non contiene `chunk0.js`, `nodedist\`, `Start Server.cmd`. Non si gioca: si compila. Le build pronte sono `Peacock-vX.Y.Z.zip` nella pagina [Releases](https://github.com/thepeacockproject/Peacock/releases/latest).
2. **Mismatch di versione.** Server 6.5.1 e patcher 8.9.1 non sono intercambiabili. Il patcher fa pattern matching sulla memoria di `HITMAN3.exe` e parla col server: devono uscire dalla **stessa** zip.
3. **Admin + Defender.** Il patcher reindirizza gli endpoint del gioco su `http://127.0.0.1`. Senza elevazione non aggancia il processo. Defender lo mette in quarantena (falso positivo cronico) → "clicco e non succede niente".

Questo kit automatizza i tre punti. Il core Peacock (server TypeScript / patcher C#) non viene toccato.

## Cosa NON è questo kit

- Non è un crack, non sblocca il gioco se non lo possiedi.
- Non patcha i file su disco: `PeacockPatcher.exe` (ufficiale) intercetta il processo **in RAM** e punta il config domain a localhost.
- Non rende giocabile questa clone Git da sola. Per giocare serve la **release packaged**.

## Uso quotidiano: un solo file

Tasto destro su **`Gioca.cmd`** → **Esegui come amministratore**. Fine.

Fa da solo, in quest'ordine:

1. Se Peacock packaged **c'è già** (es. `C:\Games\HITMAN...\Peacock` 6.5.1 con i tuoi `userdata`) **non scarica nulla**.
2. Se **manca**, scarica la release ufficiale **una tantum** e non la rioscarica alle partite successive.
3. Prova l'esclusione Defender.
4. Avvia server + patcher + `HITMAN3.exe`.
5. All'uscita aspetta ~8 secondi così Peacock scrive il profilo su disco (altrimenti le missioni sembrano resettarsi).

`Diagnostica.cmd` / `ScaricaRelease.cmd` / `EsclusioneDefender.cmd` restano opzionali. **Non** lanciare `ScaricaRelease` ogni volta: era quello che cancellava il flusso e rischiava un secondo Peacock vuoto.

I progressi stanno in `...\Peacock\userdata` e `...\contractSessions` della **stessa** cartella che vedi all'avvio. Il launcher sceglie in automatico l'install con i profili più pieni, così non salti da 6.5.1 (XP vero) a una 8.9.1 nuova (missioni a zero).

### `config.json`

Copia `config.example.json` in `config.json` e, se serve, cambia `gameExe`:

```json
{
  "peacockDir": "C:\\Users\\cosmi\\Documents\\Peacock",
  "gameExe": "C:\\Games\\HITMAN - World of Assassination\\Retail\\HITMAN3.exe",
  "serverUrl": "127.0.0.1",
  "launchGame": true,
  "stopOnGameExit": false,
  "applyOfflineOptions": true,
  "preferredInstallDir": "C:\\Users\\cosmi\\Documents\\Peacock"
}
```

`ScaricaRelease.cmd` scrive da solo `peacockDir`.

## Offline vero (niente internet)

Peacock **è** il server di gioco. Dopo il patch, HITMAN parla solo con `127.0.0.1`.

Limiti:

- Al **primo** login Peacock interroga il *negozio da cui proviene l'exe* (Epic o Steam), non Xbox, per la lista DLC (`entP`). Internet + launcher di quella piattaforma aperti. Poi il profilo resta in `userdata\` nella cartella Peacock.
- **Epic** (il tuo caso se non hai Steam): Epic Games Launcher aperto e loggato. Steam **non** serve.
- **Xbox Game Pass / Microsoft Store**: Peacock **non** lo supporta. Non usare l'exe dell'app Xbox.
- Poi puoi impostare (lo fa `Gioca.cmd` se `applyOfflineOptions` è true):
  - `updateChecking=false`
  - `leaderboards=false`
  - `imageLoading=OFFLINE`
- I contratti featured/user nuovi non si scaricano senza rete. Campagna, escalation, mastery, freelancer sul profilo locale restano.

Dopo il primo login con il launcher della tua piattaforma, quel launcher può anche andare offline.

## Perché non compilare questa repo

Questa checkout è il monorepo (Yarn 4, Node 20/22/24, esbuild, patcher Visual Studio). Compilare è per chi sviluppa Peacock. Per giocare:

```
https://github.com/thepeacockproject/Peacock/releases/latest
→ Peacock-v8.9.1.zip   (NON *-linux.zip, NON Source code)
```

Dentro ci devono essere:

- `chunk0.js`
- `nodedist\node.exe`
- `PeacockPatcher.exe`
- `Start Server.cmd`
- `resources\dynamic_resources_h*.rpkg`

Se manca uno di questi, è il source. `Diagnostica.cmd` lo dice.

## File

| File | Ruolo |
| --- | --- |
| `Gioca.cmd` | Avvio one-click (admin, server, patcher, gioco) |
| `Diagnostica.cmd` | Assessment read-only |
| `ScaricaRelease.cmd` | Download + unzip release ufficiale |
| `EsclusioneDefender.cmd` | Falso positivo patcher |
| `Launch-WOA-Offline.ps1` | Logica launcher |
| `Diagnose-Peacock.ps1` | Logica diagnostica |
| `Get-OfficialRelease.ps1` | Logica download |
| `Add-DefenderExclusion.ps1` | Logica Defender |
| `Apply-OfflineOptions.ps1` | Flag `options.ini` offline |
| `lib/PeacockOffline.psm1` | Libreria condivisa |

## Licenza

Gli script di questo kit sono AGPL-3.0 come Peacock. Il gioco è di IO Interactive; Peacock non è affiliato.
