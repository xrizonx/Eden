# Windows 11 x64 CI/CD

[Documentation index](README.md) / [Automated validation](testing.md)

The [Windows x64 workflow](../.github/workflows/windows-build.yml) produces a Windows 11-compatible,
64-bit build of the Eden server from the repository's pinned .NET SDK (see `global.json`).
The GitHub-hosted `windows-2025` runner is Windows Server 2025, **not Windows 11**.
The workflow cross-targets the Windows x64 runtime; a successful build and test on that runner
does not establish live Windows 11 client acceptance.

## When it runs

- Pull requests targeting `main`: compile, run focused hosting/policy checks, publish a downloadable build ZIP.
- Pushes to `main`: run the same validation and upload the build artifact.
- Pushes of version tags beginning with `v`: validate and attach the ZIP and SHA-256 file to a GitHub Release.
- **Actions > Windows x64 build > Run workflow**: request an on-demand build after the workflow exists on the default branch.

The workflow checks out the code, installs the exact SDK specified by `global.json`,
restores NuGet and formatter tools, builds `Eden.slnx` in Release mode, runs only
`FullyQualifiedName~Eden.Protocol.Tests.Hosting`, executes the two Python policy tests
and `tools/Format.ps1 -Check`, and publishes `Eden.Server` self-contained for `win-x64`.
The full .NET regression suite is deliberately not run automatically (see [testing policy](testing.md)).

The artifact is named **Eden-Windows-11-x64** and includes
`Eden-win-x64.zip` and `Eden-win-x64.zip.sha256`.
On a successful run, open **Actions > Windows x64 build > run summary > Artifacts**
to download it. Artifacts are retained for 14 days; version-tagged GitHub Releases
are a more durable distribution location.

## Running the build on Windows 11

1. Extract `Eden-win-x64.zip` into an empty directory. The ZIP's root contains `Eden.Server.exe`.
2. Supply the matching build-3662168 client and resource cache separately, following
   [Windows client setup](development-client.md). No EVE client, CCP assets, downloaded catalog
   export, world database, accounts or passwords are shipped with the CI artifact.
3. Generate or copy a matching `catalog.sqlite` into the published server's `data` directory
   using [catalog setup](development-client.md#creating-and-recreating-the-databases).
   Preserve an existing `world.sqlite` separately when upgrading; never overwrite it.
4. Run `./Eden.Server.exe` in PowerShell, with any desired configuration or command-line flags
   from [launcher and remote hosting](launcher.md).

The ZIP bundles the .NET runtime, so the target PC does not need the .NET SDK merely to launch
the server. Local catalog preparation still requires the prerequisites listed in the Windows
client guide, including PowerShell 7 and Python 3.11+; some optional features may use Python
at runtime. Windows Defender Firewall may require an inbound rule for multiplayer hosting.

## Release and verification

To create a versioned release, push a `v*` tag pointing at a tested commit, such as
`v0.1.0`. The workflow builds that exact tag and publishes the Windows ZIP only after its
checks pass. Check the accompanying SHA-256 file against the ZIP before installing.

This is binary compilation and distribution, **not** automatic deployment onto anyone's
computer or server. No game client is launched as part of CI. Running the software and
validating live Windows 11 gameplay/network compatibility remain separate maintainer checks.

Hosted Windows build minutes and artifact storage can incur GitHub Actions usage costs.
