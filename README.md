# Grid Bot

Grid Bot is a Discord bot, and the surrounding platform of libraries and services that support it, built to bridge Discord with Roblox's rendering and script-execution infrastructure ("Grid Servers"). At a high level it lets a Discord user:

- **Render** a Roblox character/avatar (or a batch of them) to an image, directly from a Discord command.
- **Execute Luau (and, historically, raw Lua) scripts** against a Grid Server and get the console output back in Discord.

The bot itself is a thin orchestration layer, it authenticates with Discord, parses commands, applies safety checks (flood/rate limiting, blacklisted keyword scanning, per-guild locking, etc.), and then hands work off to one or more Grid Server instances, which are the actual Roblox executables that do the rendering/execution. Those executables are **not** included in this repository and must be sourced independently (see [Notice](#notice) below).

## Repository layout

This is a monorepo containing the bot itself, a companion recovery daemon, and the shared class libraries both depend on:

| Path | Purpose |
|---|---|
| `services/grid-bot` | The main Grid Bot daemon, Discord gateway client, command handling, rendering/execution pipeline, Dockerfile. |
| `services/recovery` | `grid-bot-recovery`, a decentralized sidecar daemon that periodically health-checks the bot over RPC and can restart/report on it independently of the main deployment. |
| `lib/clients` | HTTP clients for talking to Roblox APIs. |
| `lib/configuration` | Configuration plumbing shared across the bot and its components. |
| `lib/grid` | Talks to Grid Servers, both at the SOAP/HTTP level and at the process level (native processes on Windows, containers on Linux). |
| `lib/vault` | HashiCorp Vault integration used for pulling settings/secrets at runtime. |
| `lib/floodcheckers` | Rate-limiting/anti-spam logic, used to stop abuse of the Render and Execute Script commands. |
| `lib/service-discovery`, `lib/redis`, `lib/networking`, `lib/logging`, `lib/threading`, `lib/text`, `lib/random`, `lib/hashing`, `lib/file-system` | General-purpose infrastructure libraries shared across the above. |
| `docs/` | The [MkDocs](https://www.mkdocs.org/)-based documentation site (`mkdocs.yml`), including legal/ToS pages. |
| `.github/workflows/` | The component-based CI/CD pipeline (`build.yml`, `deploy.yml`, `docs.yml`), see [DevOps](#devops) below. |

Each library/service directory has its own `README.md` with more detail than is practical to repeat here.

# History

## Origins, before this repository existed

This repository's own commit history starts in September 2021, but the bot itself is older than that. According to an entry on the [Retro Dev Fandom wiki](https://retro-dev.fandom.com/wiki/Roblox_Grid), written by people close to the project rather than the maintainer, the bot was originally called "RobloxGayPI," built by a developer going by "nsg" for experimenting with Roblox's internal APIs. That account says an early JavaScript version was thrown together quickly by someone named "Yakov" and a collaborator; Nikita Petko (the "nsg"/owner) then rewrote it in TypeScript, and, still unsatisfied, rewrote it a second time in C# on .NET, carrying pieces like the command registry over from the abandoned TypeScript codebase. The same source says the TypeScript-era code had already leaked publicly around September 2021, and separately mentions the bot was once known internally under the "RccService" name before that terminology was deliberately phased out, which matches the many `RccService`/`RCCService`-named types still visible throughout this codebase today. Treat this as secondhand community history rather than an official record, it isn't sourced from this repository or its maintainer, but it lines up with what the repository itself shows:

- The `archive/old-njs-src` branch is a single "Initial Commit" snapshot (pushed December 6, 2022) of a **TypeScript** codebase, `discord.js` for the Discord side, hand-rolled SOAP/XML parsing (`fast-xml-parser`, `xml-parse`) for talking to the Grid Server, and a command set (`ExecuteScript`, `RenderTask`, `ViewConsole`, `OpenJob`/`CloseJob`, `KillRCCService`, etc.) that maps almost one-to-one onto commands still present in the current .NET bot. This is very likely the TypeScript rewrite the wiki describes, preserved here as a historical snapshot rather than active code.
- This repository's very first commits (September 18, 2021) already introduce the project as `MFDLabs.Grid.Bot` in C#/.NET, consistent with the TypeScript phase having ended and the current C#/.NET lineage beginning before this repository's history starts.
- The heavy `RCCService`/`RccService` naming throughout `lib/grid` and elsewhere is a direct holdover from that earlier phase, and several `enhancement/`-prefixed branches later in this repo's life (e.g. disbanding old naming, restructuring `Grid.ComputeCloud` → `Grid.Client`) read as continued cleanup of that legacy.

## The repository's own history

From here on, this reflects the repository's git history directly. Grid Bot's commits on `master` go back to **September 2021**, under the name `MFDLabs.Grid.Bot`, developed on an internal instance (`mfdlabs-grid-development`) before moving to the public `mfdlabs` GitHub organization. Since then the project has accumulated **405 commits on `master`**, well over **200 topic branches** (spanning `feature/`, `fix/`, `hotfix/`, `enhancement/`, `ops/`, and `dev/` prefixes tied to numbered GitHub issues), and **1,177 CI-generated build tags** recording individual builds/releases from 2021 through mid-2026.

A rough timeline, reconstructed from the commit and tag history:

- **September 2021, Repository begins.** The C#/.NET codebase as `MFDLabs.Grid.Bot`, targeting .NET Framework (v4.7.2) initially. Early commits lay down the Discord command framework, extensions, and the first CI build pipeline (`build.yml`).
- **2021–2022, Rapid feature growth.** 2022 is the busiest year in the project's history (243 commits on `master` alone), adding features like the render pipeline, script execution commands, an "Arbiter" backlogging/queue system, auto-deployer tooling, and Google Analytics-based metrics.
- **2023, Consolidation and modernization.** Fewer, larger changes: migration off `MFDLabs.*`-prefixed internal libraries (merging `Microsoft.Ccr.Core` and concurrency helpers, reworking settings providers), a move toward .NET 6/7, and **Nomad support** (`#235`, October 2023) as an alternative to the Windows-registry/Docker-only deployment model, see [DevOps](#devops) for how deployment tooling evolved from here.
- **Late 2023 – early 2024, Rename and restructure.** `MFDLabs.Grid.Bot` is renamed to plain `Grid.Bot` (issue `#101`), `MFDLabs.Grid.AutoDeployer` becomes `Grid.AutoDeployer`, and the repository moves onto .NET 8. Shared libraries are split out and later re-merged (`#376`) as the team iterated on whether to manage them as separate NuGet-published repos or as part of this monorepo.
- **February 29, 2024, "Acceleration towards OS" (#280).** The Apache-2.0 `LICENSE` is added and the project is prepared for open-sourcing.
- **March 21, 2024, Infrastructure shutdown.** The hosted, production instance of Grid Bot was taken offline. Per the project's own announcement (`docs/index.md`), the bot's AWS infrastructure (a 16-core/32 GiB Linux host plus a client VPN) was costing roughly **$11,000/year**, which was no longer sustainable to run as a hobby project. Documentation, legal pages, and moderation docs were trimmed accordingly (`#299`, "Shutdown/movement announcement preparation"), and the maintainer opened the door for someone else to host it going forward.
- **2024–2026, Maintenance mode, as source-available infrastructure code.** Development continues at a slower pace: dependency bumps (Discord.Net v3.8.1), the `grid-bot-recovery` sidecar service is introduced, the repository is reorganized into the current component-based `lib/` + `services/` layout (`#322`), and Docker image tags continue to be published as recently as **July 26, 2026**, indicating the code is still actively built/tested even though the original public-facing deployment remains offline.
- **May 13, 2025, `grid-service-websrv` absorbed into the bot (`#343`, commit `817088c`).** The standalone HTTP API companion project is deprecated; its functionality is ported in as a new `Grid.Bot.Web` assembly, toggled by the `IsWebServerEnabled` setting, collapsing what had been two deployable components (bot + web API) into one.
- **Ongoing branches** such as `dev/rust-rewrite` and `dev/go-dev` suggest exploratory work toward rewriting parts of the stack in other languages, alongside continued `.NET`-based `enhancement/` and `hotfix/` work on `master`.

## Release cadence, from the tag history

The repository's CI pipeline (originally TeamCity, later GitHub Actions) tags every build it produces, `1,177` tags in total as of this writing, spanning September 2021 to July 2026:

| Year | Tags pushed | Note |
|---|---|---|
| 2021 | 57 | All pushed in a single batch on 2021-11-21, the initial migration of the repo's history onto GitHub. |
| 2022 | 472 | The peak year by far. December 2022 alone accounts for 334 tags, driven by same-day iterative hotfix loops (e.g. 85 tags pushed on 2022-12-05 alone, chasing the `ViewConsole`/`GetAllSettings` bugs). |
| 2023 | 460 | Front-loaded into January–February (335 tags) finishing out the `feature/grid-server-recovery-pt2` and related work, then tapering off sharply after the `ops/217-major-project-restructure` and `lua-sandbox` work in March. |
| 2024 | 124 | Concentrated around the `Acceleration towards OS` / shutdown period (Feb–Mar) and a `grid-bot-`-prefixed retagging scheme introduced in July 2024 following the `ops/322-component-based-repository` restructure. |
| 2025 | 38 | Sparse, bursty maintenance: a cluster in May, a couple of tags in September, a small cluster in November–December. |
| 2026 | 26 | A couple of tags in January, then a cluster of 24 in late July 2026, the most recent activity in the repository. |

A few patterns stand out:

- **Same-day iteration storms.** Several of the busiest single days (2022-12-05/06/07, 2023-01-01, 2023-02-12) show dozens of tags pushed within hours of each other, the CI system re-tagging on every push while a specific bug (e.g. `hotfix/viewconsole-works-barely`, `feature/grid-server-recovery-pt2`) was being iterated on live, sometimes against production.
- **Long dormant stretches.** The largest gaps between any tagged activity are **216 days** (2024-10-09 → 2025-05-13), **192 days** (2026-01-14 → 2026-07-25), and **138 days** (2023-03-13 → 2023-07-29), the first two line up with the post-shutdown maintenance-mode era described above, where the project goes quiet for months and then gets a concentrated round of dependency/CI catch-up work.
- **A tagging-scheme change around mid-2024.** Early tags follow a bare `YYYY.MM.DD-HH.MM.SS_<branch>_<sha>[-Config]` format; from July 2024 onward (`grid-bot-2024.07.01-...`) tags are prefixed with the component name (`grid-bot-`, `grid-bot-recovery-`), reflecting the split into the current multi-service `lib/` + `services/` layout.

## Issues and pull requests

- **Issue numbering runs #1 → #382**, and pull requests run #2 → #379, the two counters are close because almost every substantive issue got a matching PR (often opened within minutes, sometimes on the same day). Of the pull requests, **177 were merged and 35 were closed without merging**, the unmerged ones are mostly Dependabot version-bump PRs superseded by a later batch bump (e.g. eight separate `Bump Newtonsoft.Json...` PRs, #178–#185, all closed unmerged on the same day in favor of one consolidated update), plus a handful of duplicate/abandoned attempts at the same fix (e.g. `#53` and `#60` duplicate `GRIDBOT-17: Arbiter Backlogging System`, later done properly as issue `#41`/PR `#41`).
- **The "iteration storm" days now have names attached.** The 2022-12-05/06/07 tag spike lines up with issues `#160`–`#175`, a rapid back-and-forth on `ViewConsole` embed bugs, `GetAllSettings` truncation, and command circuit breakers, each shipped as its own tiny PR within the hour. The 2023-01-01 spike is issue `#202`/PR `#202`, "Base implementation for recovery. (Stage 1: Rewrite)", someone was clearly debugging the Grid Server recovery system live on New Year's Day.
- **`[MAJOR]`-tagged issues mark the worst production incidents**: `#146` (sudden CPU spike on the gateway server, Nov 2022), `#221` (call for a multi-threaded web server after the process manager bottlenecked, Apr 2023), `#223` (massive CPU usage increase, Jun 2023), and `#230` (the bot reconnecting to Discord "a massive amount of times within a short period," Jun 2023), all tagged `kind: hotfix` and resolved same-day or next-day once triaged.
- **PR #299, "Shutdown/movement announcement preparation,"** is the actual commit that took down the docs/contact pages referenced in the [History](#history) section above, merged the same day the shutdown was announced, March 21, 2024.
- **Post-shutdown, the issue tracker's character changes.** From mid-2025 onward, a growing share of new issues are auto-filed, low-effort community noise rather than real engineering work, blank `[Bug]:`/`[Feature]:` templates, a couple of Arabic/Cyrillic-titled one-liners, and outright spam (issue `#372`, literally titled "spam"). This tracks with the project's shift from active development to a repository that's occasionally revived for a specific fix (issue `#378`, "Synchronize Grid with @rbxinfra/rcc-core," and `#376`, "Remerge libraries back into this repository," both closed July 25–27, 2026, the most recent activity in the tracker).
- **By label**, closed issues skew toward `kind: enhancement` (~51) and `kind: feature` (~38) over `kind: fix` (~30) and `kind: hotfix` (~5) in this sample, more feature/ops work than firefighting, which fits a project whose team had the bandwidth to restructure (`#101`, `#217`, `#322`) and modernize (`#150`, `#199`) rather than just keep the lights on, at least until the shutdown.

# Want to report an issue?

These issue reports apply to the currently deployed bot.

You can request the following: 
1. [Bug Reports](https://github.com/mfdlabs/grid-bot/issues/new?assignees=nikita-petko&labels=kind:%20fix%2Cstatus:%20backlogged%2Cstatus:%20required-review&template=bug_report.yml&title=%5BBug%5D%3A+)
2. [Feature Requests](https://github.com/mfdlabs/grid-bot/issues/new?assignees=nikita-petko&labels=kind:%20feature%2Ckind:%20enhancement%2Cstatus:%20backlogged%2Cstatus:%20required-review&template=feature_request.yml&title=%5BFeature%5D%3A+)
3. [Security Issues](https://github.com/mfdlabs/grid-bot/issues/new?assignees=nikita-petko&labels=kind:%20hotfix%2Cpriority:%20key%20deliverable%2Cstatus:%20backlogged%2Cstatus:%20required-review&template=security_vulnerabilty.yml&title=%5BVulnerability%5D%3A+)
4. [Blacklist Appeals](https://github.com/mfdlabs/grid-bot/issues/new?assignees=nikita-petko&labels=kind:%20appeal%2Cstatus:%20backlogged%2Cstatus:%20required-review&template=blacklist_appeal.yml&title=%5BAppeal%5D%3A+)

# Notice

## Current status

The original hosted deployment of Grid Bot was **shut down on March 21, 2024** due to hosting costs (see [History](#history)). This repository remains actively maintained as source-available infrastructure code, new commits, dependency updates, and CI-built container images continue to be published, but there is currently no public instance of the bot running. Anyone wishing to self-host or take over hosting can reach out via the contact info on the project's [documentation site](https://grid-bot.ops.vmminfra.net).

## Usage of Roblox, or any of its assets.

# ***This project is not affiliated with Roblox Corporation.***

The usage of the name Roblox and any of its assets is purely for the purpose of providing a clear understanding of the project's purpose and functionality. This project is not endorsed by Roblox Corporation, and is not intended to be used for any commercial purposes.

This project uses an executable to interact with Roblox character renders and Luau code execution. This executable will not be provided in this repository, you must source it yourself. The executable provided must be one that supports JSON script executions.

Historically, the HTTP API in front of that executable was provided by a separate companion project, [grid-service-websrv](https://github.com/mfdlabs/grid-service-websrv). That project is now deprecated: as of [PR #343](https://github.com/mfdlabs/grid-bot/pull/343) (commit `817088c`, merged May 13, 2025), its web-server functionality was folded directly into this bot as a new `Grid.Bot.Web` assembly (`services/grid-bot/lib/web`), gated by the `IsWebServerEnabled` setting, so grid-service-websrv is no longer a required separate component for new deployments.

## Copyright and Licensing

This project is licensed under the Apache-2.0 License, and is provided as is. The project is not intended to be used for any commercial purposes.

All code and releases in this repository, that were made before the license was added, are subject to copyright and are unlicensed. The license only applies to code and releases made after the license was added. Usage of code and releases made before the license was added is at your own risk, but distribution of code and releases made before the license was added is subject to DMCA takedown requests.

This notice only serves the purpose of giving a clear understanding of project boundaries and limitations, and should be taken into account when using this project.

# Installation

The installation of this project is simple, and can go as follows:


This repository provides Docker builds at [Docker](https://hub.docker.com/r/mfdlabs/grid-bot).

This repository also supplies [releases](https://github.com/mfdlabs/grid-bot/releases), which can be ran with any distribution of [.NET 8.0.1](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
    
```bash
dotnet Grid.Bot.dll
```

Please see the [notice](#notice) for information on copyright, licensing and distribution.

# Configuration

The configuration in this repository can be loaded 2 ways:

- Through the [environment](#environment).
- Through the [Vault](#vault). (recommended)

## Required Settings

| _Variable Name_             | Variable Type | Provider Name | Description                                                                                                                                      |
|-----------------------------|---------------|---------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| BotToken                    | string        | Discord       | The token that corresponds to the Bot that will consume this code.                                                                               |
|                             |               |               |                                                                                                                                                  |
| **IF ON WINDOWS**           |               |               |                                                                                                                                                  |
| GridServerExecutableName    | string        | Grid          | The name of the executable used for grid server operations.                                                                                      |
| GridServerRegistryKeyName   | string        | Grid          | The name of the registry key that stores the full path to the grid server executable.                                                            |
| GridServerRegistryValueName | string        | Grid          | The name of the registry value that stores the full path to the grid server executable.                                                          |
|                             |               |               |                                                                                                                                                  |
| **IF USING DOCKER**         |               |               |                                                                                                                                                  |
| GridServerImageName         | string        | Grid          | The name of the Docker image that the process manager will use.                                                                                  |
| GridServerImageTag          | string        | Grid          | The tag of the Docker image that the process manager will use.                                                                                   |
| GridServerSettingsKey       | string        | Grid          | The settings key for the grid server used to fetch it's configuration, when using Windows this is most likely already specified in the registry. |

You may find information about these settings, and other settings in their respective providers under [`services/grid-bot/lib/settings/Providers`](./services/grid-bot/lib/settings/Providers).

## Environment

This method of fetching configuration simply turns to the environment to search for strings.
The settings cannot be persisted this way.

Examples:

```powershell

$env:BotToken = "Testing!"

dotnet Grid.Bot.dll

```

```bash

BotToken = "Testing!" dotnet Grid.Bot.dll

# or

export BotToken = "Testing!"

dotnet Grid.Bot.dll

```

## Vault

This method of fetching configuration fetches settings from Vault.
If a setting is not found in Vault, it will fall back to the [environment](#environment)
If you are using this method you will have to define the following environment variables beforehand:

- VAULT_ADDR - The address to the Vault server, this is optional and if not using this the [environment](#environment) will be forced.
- VAULT_TOKEN or VAULT_CREDENTIAL - The token or credential to use, if using approle, the format is as follows: `{roleName|roleId}:{secretId}`
- VAULT_MOUNT - Optional, if not set it defaults to `grid-bot-settings`, but allows you to override the mount point for settings (see [SettingsProvidersDefaults.cs](./services/grid-bot/lib/settings/SettingsProvidersDefaults.cs))

These all supply a path that is dependent on an environment variable called ENVIRONMENT, which defaults to development.
e.g, grid-bot-settings/development/discord/debug, would contain the settings for the DiscordProvider for the development environment:

![](https://mfdlabs-infrastructure.s3.amazonaws.com/perma-share/gb-user/2024-06-15+234623.png)

The format of these are normally: {environmentName}/{providerName}, and the providerName changes to the lowercase and replaces word splits with dashes (UsersClientSettings -> users-client)

# Pre-JSON execution and pre-Luau

In order to support pre-JSON execution, you must specify the `PRE_JSON_EXECUTION` constant defition:

```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);PRE_JSON_EXECUTION</DefineConstants>
</PropertyGroup>
```

And you must rebuild these libraries: Grid.ProcessManagement and Grid.ProcessManagement.Docker, as they also use this constant defition.

While this supports the old method of raw Lua execution, all features are not guaranteed to work, such as the LuaVM (You can disable the LuaVM via the setting LuaVMEnabled in the Scripts provider).

# DevOps

## History

The DevOps setup in this repository has gone through three fairly distinct eras:

**1. TeamCity + checked-in scripts (2021–2022).** The earliest builds ran on an internal TeamCity instance (`env.BUILD_YEAR` for TeamCity artifacts, issue `#190`; Kotlin/YAML/JSON TeamCity configs, issue `#127`), with PowerShell/CMD build and auto-release-uploader scripts checked directly into the repo. Deployment in this era meant running the bot as a Windows service/executable, located via a Windows registry key that pointed at the Grid Server executable, hence the `GridServerRegistryKeyName`/`GridServerRegistryValueName` settings that still exist for Windows hosts today. An early, short-lived `MFDLabs.Grid.AutoDeployer` service (`#48`, `#98`, `#137`) handled rolling out new builds to remote machines before this was later folded into the GitHub-native pipeline.

**2. GitHub Actions + Docker + a single checked-in Nomad job (Dec 2022 – mid-2024).** Issue `#155`, "Move to GHA," moved builds off TeamCity entirely. This wasn't a leisurely modernization, an internal cloud account migration in March 2023 took the TeamCity server and its build agents offline alongside the bot's own hosting, and reconstructing the old pipeline on the new infrastructure was judged not worth the effort, so `#155` was bumped to the top of the priority board as a prerequisite for any further development. Around the same time (`#235`, October 2023, "Separate grid servers from the bot (move bot to Docker)"), the bot itself became containerizable, and [HashiCorp Nomad](https://www.nomadproject.io/) was adopted as the scheduler for running it, with deployment described by a single hand-maintained `nomad/grid-bot.nomad` HCL file, hand-edited across many small "Update grid-bot.nomad" commits every time a setting, resource limit, or route changed (`#245`, `#246`, `#254`, `#257`, `#287`, `#302`, `#304`/`#305`, etc.). An internal design note from this period also explains a few of the more cryptic issue labels visible in this repo's tracker: the `opsec: love-all-platforms`/`opsec: love-all-environments` labels stem from an internal "LAP" ("Love All Platforms") initiative to expand the bot beyond Windows-only support.

**3. The component-based system (July 2024 – present).** Issue `#322`/PR `#323`, "Component Based repository" (merged July 1, 2024), replaced the hand-maintained Nomad file with a generic, declarative system that treats every deployable service the same way. The checked-in `nomad/grid-bot.nomad` file was removed entirely, Nomad job definitions are no longer stored in this repository at all; they're generated on demand from metadata that lives next to each service instead. This is also when the repository was reorganized into today's `lib/` (shared libraries) + `services/` (deployable components) layout, and `grid-bot-recovery` was onboarded onto the same system alongside `grid-bot`.

## How the current system works

Each deployable service under `services/` (currently `grid-bot` and `recovery`) carries its own **`.component.yaml`** manifest, which is the single source of truth for how that component is built, containerized, and deployed. A manifest has two main sections:

- **`build`**, the `.csproj` to build, additional MSBuild args (e.g. `IMAGE_TAG`), and a `docker` block naming the Dockerfile and the target image (e.g. `mfdlabs/grid-bot`).
- **`deployment`**, everything Nomad needs to run the container: job/namespace naming, per-environment placement `constraints`, the Vault role to authenticate as, resource requests, network mode and ports, Consul service registrations (with health checks and Traefik routing tags), volume mounts, and `config_maps`/`artifacts`, inline scripts and files (e.g. fetching the Vault CA chain, minting a client-settings token, pre-creating data directories) that get materialized on the Nomad client before the container starts.

Values in a manifest can reference CI-provided variables with `${{ env.SOME_VAR }}` syntax (e.g. `${{ env.NOMAD_VERSION }}`, `${{ env.NOMAD_ENVIRONMENT }}`), which get substituted in at build or deploy time, this is what lets one `.component.yaml` file serve staging and production alike without duplicating the whole config.

This manifest is consumed by two separate GitHub Actions workflows, split so that building an image and actually rolling it out to a Nomad cluster are independent operations:

- **`build.yml`** runs on every push. It reads which components changed from `#!components: ...` / `#!deployable-components: ...` markers embedded in the commit message (falling back to explicit input on manual runs), uses [`mfdlabs/component-finder-action`](https://github.com/mfdlabs/component-finder-action) to locate the matching `.component.yaml` files under `services/`, computes a timestamp+shortSHA version string (the same `YYYY.MM.DD-HH.MM.SS-<sha>[-dev]` scheme visible in the repo's tags, formally, `yyyy.MM.dd-hh.mm.ss-{gitSha}`, optionally with a manual suffix such as `_patch335.1` for a targeted patch build against issue `#335`), validates each manifest's `build` section, then builds and, unless suppressed with `#!skip-image!#`/`#!skip-release!#` commit-message flags, tags, pushes the resulting Docker image and cuts a GitHub Release.
- **`deploy.yml`** is manually dispatched with a list of components, target environment (`staging`/`production`), and a `resources` string (e.g. `grid-bot,1000:2048` for CPU/RAM). It uses the same component-finder action to locate manifests, then [`mfdlabs/component-nomad-parser-action`](https://github.com/mfdlabs/component-nomad-parser-action) to translate each `.component.yaml`'s `deployment` section into an actual Nomad HCL job spec on the fly, this is the step that effectively regenerates what used to be the checked-in `nomad/grid-bot.nomad` file, just no longer persisted to disk, and finally submits each generated job to the cluster with the Nomad CLI.

Beyond the two flags above, a handful of other `#!`-prefixed markers in a commit message steer the pipeline: `#!skip-build!#` skips the build entirely; `#!components` names which components a commit touches (all components are assumed if omitted); `#!deployable-components` names which of those are eligible to deploy (none are deployable by default, a component has to opt in); and `#!skip-deploy!#` skips deployment outright (functionally the same as leaving `#!deployable-components` empty). This is also why commit messages elsewhere in this README's history section sometimes look unusually structured, they're not incidental, they're pipeline directives.

Net effect: instead of one shared, manually-edited Nomad file per repository, every service declares its own deployment shape next to its own code, and the actual HCL is an ephemeral build artifact rather than something a developer edits by hand.

# License

This project is licensed under the Apache-2.0 License:

```
   Copyright 2024 MFDLABS

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
```
