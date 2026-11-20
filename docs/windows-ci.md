# Windows 11 x64 CI/CD

[Documentation index](README.md) / [Automated validation](testing.md)

The [Windows x64 workflow](../.github/workflows/windows-build.yml) compiles all nine .NET projects in the solution and
packages a Windows 11-compatible 64-bit server and a complete development/tooling kit. It installs the current stable .NET 10 SDK and follows the
`global.json` compatibility policy (`latestFeature`) rather than requiring a single SDK patch. GitHub's `windows-2025` runner is **Windows Server 2025, not Windows 11**: successful CI shows
Windows x64 build compatibility, not live Windows 11 game-client acceptance.

## Build triggers

- Pull requests targeting `main`: build and validate, uploading both ZIP archives.
- Pushes to `main`: build and validate, uploading both ZIP archives.
- Pushes of version tags beginning with `v`: validate, then attach both archives and checksums to a GitHub Release.
- After merging into `main`, use **Actions > Windows x64 build > Run workflow** for an on-demand build.

The pipeline verifies that every `.csproj` beneath `src`, `tests` and `tools` is included in `Eden.slnx`, restores
NuGet dependencies with the selected stable .NET 10 SDK, builds the entire solution in Release mode, runs the focused
`Eden.Protocol.Tests.Hosting` tests, checks the two Python policy suites and formatting, and validates Python 3 and PowerShell tooling
syntax. It separately publishes the server and benchmark executable for `win-x64`, copies the compiled test assemblies,
and checks required output files. The full regression suite is not run automatically (see [testing policy](testing.md)).

## What is downloadable

| Download | Contents |
| --- | --- |
| `Eden-Windows-11-x64` | `Eden-win-x64.zip` and SHA-256 checksum. A self-contained Windows x64 server with `Eden.Server.exe`, .NET runtime, managed libraries, embedded resources, skill-client helpers and administration web assets. |
| `Eden-Windows-11-x64-complete` | `Eden-win-x64-complete.zip` and checksum. All Git-tracked files plus compiled Windows server, benchmarks and test assemblies. |

The complete ZIP preserves the original repository structure, including `src/`, `tests/`, `tools/` (PowerShell
and Python utilities, catalog sources and audit tools), `assets/`, `docs/`, `.config/`, `.github/`, `Eden.slnx`,
`global.json`, project settings and example host configuration. It also adds:

```text
build/windows-x64/server/Eden.Server.exe
build/windows-x64/benchmarks/Eden.Benchmarks.exe
build/windows-x64/tests/Eden.Protocol.Tests.dll
```

The server and benchmark executables are self-contained. The source tree, benchmark harness and test project can
also be rebuilt using the pinned .NET SDK. The included Python scripts are **source utilities**, not converted
Windows EXEs: they require appropriate Python interpreters and sometimes external data or tooling. CI checks their
Python 3 syntax but does not run every importer/exporter. Docker shell scripts and Linux Compose definitions are
preserved for completeness; they are not native Windows binaries. Client-side Python 2.7 compilation and
catalog generation require their documented external inputs, and CI does not run them without those inputs.

To download either build, open **Actions > Windows x64 build > successful run > Artifacts**. These artifacts are
retained for 14 days; version-tagged GitHub Releases provide a durable location.

## Running the Windows server

For just the runtime, extract `Eden-win-x64.zip` and run `./Eden.Server.exe` in PowerShell. From the complete kit,
run `./build/windows-x64/server/Eden.Server.exe`, or use `dotnet run --project src/Eden.Server` after installing
a compatible stable .NET 10 SDK. The server ZIP bundles its runtime; the build tools do not bundle an entire SDK.

Supply the matching EVE client build 3662168 and resource cache separately per
[Windows client setup](development-client.md). Generate or copy the matching `catalog.sqlite` into the server's
`data` directory. Preserve existing `world.sqlite` saves when upgrading. In the **complete kit**, run
`./tools/DevelopmentClient.ps1` and `./tools/Setup-Catalog.ps1` from the repository root as described in that
guide; local catalog preparation requires PowerShell 7, Python 3.11+, a compatible stable .NET 10 SDK and the matching client
inputs. Some optional tools also require extra external dependencies. Windows Firewall may need an inbound rule
for remote multiplayer hosting.

No proprietary EVE client binaries, CCP resource cache, generated catalog database, saved worlds, secrets,
cached dependencies or other untracked local files are included in either archive.

## Release and checksums

Push a version tag such as `v0.1.0` to publish both ZIPs and their `.sha256` files only after the build job
succeeds. Verify ZIP integrity with the accompanying hash before extracting. This pipeline builds and distributes
software; it does not deploy to a PC or server, launch a game client, or verify live game/network compatibility.

Hosted Windows runners, artifact storage and two full distributions may consume GitHub Actions allowances.
