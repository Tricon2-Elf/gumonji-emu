# Gumonji Emulator

![alt text](https://game.watch.impress.co.jp/docs/20040827/gumo01.jpg)

Gumonji Emulator is an independent server emulator for the discontinued Gumonji
online game. It recreates parts of the original login and game server so the
client can connect to a locally run server. The project is also a place to
document and explore the original network protocol.

This is a work in progress. Login, character creation and selection, zone entry,
and a growing set of world interactions are implemented; full gameplay and
multiplayer behavior are not yet available. Original client assets are not
included.

## Project layout

- `gumonji.Network/` — VCE transport, packet framing and packet definitions.
- `gumonji.Common/` — packet handlers, sessions, game logic and EF Core data access.
- `gumonji.Server/` — executable host for the frontend, game, and original
  `zonesv` backend connections.
- `gumonji.Common.Tests/` — xUnit tests for protocol and server behavior.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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

Start both server listeners in one process:

```sh
dotnet run --project gumonji.Server
```

By default, the frontend listens on TCP port `12421` and the game server on
`23432`; the experimental original-zone backend listens on `127.0.0.1:12422`.
SQLite data is stored in `gumonji.db` in the process's working
directory. The database and its WAL files are ignored by Git. EF Core applies
pending migrations automatically when the server starts.

Accounts are created on first login: enter a username and password in the
client, then create a character when prompted. There is no preconfigured test
account. The default handoff address advertised to the client is `127.0.0.1`,
which is suitable when the client runs on the same computer. Server settings
such as ports, bind address, database path and advertised address are defined
by `EmuOptions` in `gumonji.Common/GumonjiSession.cs`.

Launch the client with these arguments to connect to the local emulator:

```sh
gumonji.exe femsg=127.0.0.1 city=1 url=gumonji://1/
```

## Original `zonesv` backend (experimental)

Run only the backend listener with `dotnet run --project gumonji.Server --
--backd-only`. To run the frontend and backend while leaving TCP `23432` free
for the original `zonesv`, use `dotnet run --project gumonji.Server --
--no-game`. Without either switch, all three listeners start. The backend
accepts the zone's login, player handoff token check, status, character lock, save/load, existence,
door-ID allocation, and empty passage-link queries described in
[`docs/zonesv_backd_protocol.md`](docs/zonesv_backd_protocol.md). Packed zone
character data is stored in the `BackdCharacters` SQLite table. This is not
yet a full substitute for the original backend; other generated message IDs
and a live `zonesv` handshake remain unverified. The backend binds to
loopback by default. Set `GUMONJI_BACKD_PASSWORD` to require the zone's
configured server password; without it, any local zone process can log in.

The original zone INI is Blowfish-encrypted. To inspect or change its settings,
install Python and `pycryptodome`, then decrypt and re-encrypt **copies**:

```sh
python -m pip install pycryptodome
python scripts/zonesv_ini.py decrypt path/to/1/zonesv.ini zonesv.txt
python scripts/zonesv_ini.py encrypt zonesv.txt zonesv-new.ini
```

This executable keeps its backend address in the binary, not the INI. To
redirect it to the local listener, patch a separate copy:

```sh
python scripts/patch_zonesv_backd.py zonesv_win.exe zonesv_local.exe
```

The patcher checks for exactly one known original address and refuses to
overwrite its input or an existing output. The INI tool defaults to the
original zone INI's 2048-byte size; use `--size` for another original INI
size. Keep the decrypted file private because it contains `server_pass`.

## Tests

Run the test suite from the repository root:

```sh
dotnet test gumonji.slnx
```

## Disclaimer

This is an unofficial community project for educational and game-preservation
purposes. It is not affiliated with or endorsed by the original developers or
publisher.
