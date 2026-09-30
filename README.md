# Gumonji Emulator

![alt text](https://game.watch.impress.co.jp/docs/20040827/gumo01.jpg)

Gumonji Emulator is an independent server emulator for the discontinued Gumonji
online game. It recreates parts of the original login and zone server so the
client can connect to a locally run server. The project is also a place to
document and explore the original network protocol.

This is a work in progress. Login, character creation and selection, zone entry,
and a growing set of world interactions are implemented; full gameplay and
multiplayer behavior are not yet available. Original client assets are not
included.

## Project layout

- `gumonji.Network/` — VCE transport, packet framing and packet definitions.
- `gumonji.Common/` — packet handlers, sessions, game logic and EF Core data access.
- `gumonji.Server/` — executable host for the frontend, zone, and original
  `zonesv` backend connections.
- `gumonji.Common.Tests/` — xUnit tests for protocol and server behavior.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Python 3 if you want to run the original `zonesv` with a patched backend address.
- A Gumonji client installation configured to connect to your server. The
  installer is available from the [Internet Archive](https://web.archive.org/web/20071025112431/http://www.gumonji.net/download/gumonji_setup.exe).
  Client files and game assets are not distributed with this repository.

## Build

From the repository root:

```sh
dotnet restore gumonji.slnx
dotnet build gumonji.slnx
```

## Run locally

For the emulator's own frontend, zone, and backend services, run this from the
repository root:

```sh
dotnet run --project gumonji.Server
```

Then launch the client with:

```text
gumonji.exe femsg=127.0.0.1 city=1 url=gumonji://1/
```

The frontend listens on TCP port `12421`, the emulator's zone server on
`23432`, and Backd on `127.0.0.1:12422` by default.
SQLite data is stored in `gumonji.db` in the process's working
directory. The database and its WAL files are ignored by Git. EF Core applies
pending migrations automatically when the server starts.

Accounts are created on first login: enter a username and password in the
client, then create a character when prompted. There is no preconfigured test
account. The default handoff address advertised to the client is `127.0.0.1`,
which is suitable when the client runs on the same computer. Server settings
such as ports, bind address, database path and advertised address are defined
by `EmuOptions` in `gumonji.Common/GumonjiSession.cs`.

## Run with the original `zonesv` (experimental)

Use separate terminals for these steps on Windows. Keep the original `zonesv`
folder and its `1/zonesv.ini` and other assets together. Do not start the
emulator's zone listener at the same time: both it and `zonesv` use TCP `23432`.

1. From the repository root, patch a **copy** of the original executable. Replace
   `C:\path\to\zonesv` with your zone-server directory:

   ```powershell
   python .\scripts\patch_zonesv_backd.py "C:\path\to\zonesv\zonesv_win.exe" "C:\path\to\zonesv\zonesv_local.exe"
   ```

   The script changes the EXE's built-in backend address to `127.0.0.1:12422`
   (domain `orange`). It does not modify the original EXE or the INI, and
   refuses to overwrite an existing output file. If you already have
   `zonesv_local.exe`, choose a new output filename.

2. From the repository root, start the emulator's frontend and Backd while
   leaving the zone port free:

   ```powershell
   dotnet run --project .\gumonji.Server -- --no-zone
   ```

3. In another terminal, start the patched EXE **from its own directory** so
   its relative paths resolve:

   ```powershell
   Set-Location "C:\path\to\zonesv"
   .\zonesv_local.exe
   ```

4. Launch the game client with the same connection arguments as above:

   ```text
   gumonji.exe femsg=127.0.0.1 city=1 url=gumonji://1/
   ```

The frontend should accept the client on `12421`, then hand it off to the
original zone server on `23432`; `zonesv` connects back to Backd on `12422`.
`--backd-only` starts just Backd and will not provide the client-facing
frontend. `--no-game` remains an alias for `--no-zone`.

Backd supports the messages described in
[`docs/zonesv_backd_protocol.md`](docs/zonesv_backd_protocol.md), but the
original-zone integration is still incomplete. The backend binds to loopback
by default. To enforce the zone's configured server password, set
`GUMONJI_BACKD_PASSWORD` in the emulator's terminal before starting it;
without that variable, a local zone process can log in without a password
check.

The backend address is in the EXE, not `zonesv.ini`. Editing the encrypted
INI is optional and requires `pycryptodome`. Work on copies and keep the
decrypted file private because it contains `server_pass`:

```powershell
python -m pip install pycryptodome
python .\scripts\zonesv_ini.py decrypt "C:\path\to\zonesv\1\zonesv.ini" zonesv.txt
python .\scripts\zonesv_ini.py encrypt zonesv.txt zonesv-new.ini
```

## Tests

Run the test suite from the repository root:

```sh
dotnet test gumonji.slnx
```

## Disclaimer

This is an unofficial community project for educational and game-preservation
purposes. It is not affiliated with or endorsed by the original developers or
publisher.
