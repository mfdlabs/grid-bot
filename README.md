# Grid Bot

This repository contains code for the Grid Bot, this bot is designed to interact with Roblox via:

- Rendering Roblox characters.
- Executing Luau code.

# Notice

## Usage of Roblox, or any of its assets.

# ***This project is not affiliated with Roblox Corporation.***

The usage of the name Roblox and any of its assets is purely for the purpose of providing a clear understanding of the project's purpose and functionality. This project is not endorsed by Roblox Corporation, and is not intended to be used for any commercial purposes.

This project uses an executable to interact with Roblox character renders and Luau code execution. This executable will not be provided in this repository, you must source it yourself. The executable provided must be one that supports JSON script executions.

The API of this executable must also be provide by the user, we provide a simple API that just provides the neccessary HTTP requests to interact with the executable: [grid-service-websrv](https://github.com/mfdlabs/grid-service-websrv).

## Copyright and Licensing

This project is licensed under the Apache-2.0 License, and is provided as is. The project is not intended to be used for any commercial purposes.

All code and releases in this repository, that were made before the license was added, are subject to copyright and are unlicensed. The license only applies to code and releases made after the license was added. Usage of code and releases made before the license was added is at your own risk, but distribution of code and releases made before the license was added is subject to DMCA takedown requests.

This notice only serves the purpose of giving a clear understanding of project boundaries and limitations, and should be taken into account when using this project.

# Installation

The installation of this project is simple, and can go as follows:

- Either through the basic method of cloning the repository and having it fetch dependencies from the private NuGet repository.
- or by removing [nuget.config](nuget.config), cloning the repository with `--recurse-submodules` and then running a target within [Makefile](Makefile) that has `local` in the name (e.g. `make build-local-debug`), or going into each library manually [Grid.Bot](./src/Grid.Bot.csproj), [Shared.Commands](./shared/commands/Shared.Commands.csproj), [Shared.Events](./shared/events/Shared.Events.csproj), [Shared.Settings](./shared/settings/Shared.Settings.csproj) and [Shared.Utility](./shared/utility/Shared.Utility.csproj), and then defining the property `LocalBuild` in the configuration:
    
    ```xml
    <PropertyGroup>
        <LocalBuild>true</LocalBuild>
    </PropertyGroup>
    ```

This repository also provides a [Dockerfile](Dockerfile) to build the project in a container, but the current setup only works in CI/CD environments, as the container requires that the code be 'published' to the ./deploy directory beforehand (see [build.yml](.github/workflows/build.yml)).

This repository also supplies [releases](https://github.com/mfdlabs/grid-bot/releases), which can be ran with any distribution of [.NET 8.0.1](https://dotnet.microsoft.com/download/dotnet/8.0) or higher:
    
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

You may find information about these settings, and other settings in their respected providers under [this directory](./shared/settings/Providers).

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
If you are using this method you will have to define the following environment variables beforehand:

- VAULT_ADDR - The address to the Vault server.
- VAULT_TOKEN or VAULT_CREDENTIAL - The token or credential to use, if using approle, the format is as follows: `{roleName|roleId}:{secretId}`
- VAULT_MOUNT - Optional, if not set it defaults to `grid-bot-settings`, but allows you to override the mount point for settings (see [SettingsProvidersDefaults.cs](./shared/settings/SettingsProvidersDefaults.cs))

These all supply a path that is dependent on the current build configuration, e.g, grid-bot-settings/grid-bot/discord/debug, would contain the settings for the DiscordProvider in Debug mode:

![](https://infrastructure.cdn.arc-cloud.net/share/gb-dev/2024-02-24%2015-34-11.png)

The format of these are normally: grid-bot/{providerName}/{configuration}, and the providerName changes to the lowercase and replaces word splits with dashes (UsersClientSettings -> users-client)

# Pre-JSON execution and pre-Luau

In order to support pre-JSON execution, you must specify the `PRE_JSON_EXECUTION` constant defition:

```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);PRE_JSON_EXECUTION</DefineConstants>
</PropertyGroup>
```

And you must rebuild these libraries: Grid.ProcessManagement and Grid.ProcessManagement.Docker, as they also use this constant defition.

While this supports the old method of raw Lua execution, all features are not guaranteed to work, such as the LuaVM (You can disable the LuaVM via the setting LuaVMEnabled in the Scripts provider).

# Nomad

This project also supports the use of Nomad, and can be ran in a Nomad environment.

The [Nomad](./nomad) directory contains a [job](./nomad/grid-bot.nomad) that can be used to run the bot in a Nomad environment, this file is a template and must be modified to suit your environment.

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
