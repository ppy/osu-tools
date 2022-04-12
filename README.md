# osu!tools [![Build status](https://ci.appveyor.com/api/projects/status/70owdbhaaepp70u5?svg=true)](https://ci.appveyor.com/project/peppy/osu-tools)  [![CodeFactor](https://www.codefactor.io/repository/github/ppy/osu-tools/badge)](https://www.codefactor.io/repository/github/ppy/osu-tools) [![dev chat](https://discordapp.com/api/guilds/188630481301012481/widget.png?style=shield)](https://discord.gg/ppy)

Tools for [osu!](https://osu.ppy.sh).

# Requirements

- A desktop platform with the [.NET 6.0 SDK](https://dotnet.microsoft.com/download) installed.
- When working with the codebase, we recommend using an IDE with intelligent code completion and syntax highlighting, such as the latest version of [Visual Studio](https://visualstudio.microsoft.com/vs/), [JetBrains Rider](https://www.jetbrains.com/rider/) or [Visual Studio Code](https://code.visualstudio.com/).
- These instructions assume you have the the [CLI git client](https://git-scm.com/) installed, but any other GUI client such as GitKraken will suffice.
- Note that there are **[additional requirements for Windows 7 and Windows 8.1](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60#dependencies)** which you may need to manually install if your operating system is not up-to-date.

# Getting Started

## I just want to run it
- Clone the repository including submodules (`git clone --recurse-submodules https://github.com/ppy/osu-tools`)
- Navigate to each tool's directory and follow the instructions listed in the tool's README.
    - [PerformanceCalculator](https://github.com/ppy/osu-tools/blob/master/PerformanceCalculator/README.md) - A tool for calculating the difficulty of beatmaps and the performance of replays.

## I want to make changes
Most relevant code is in the main [ppy/osu](htts://github.com/ppy/osu) repository. To make any meaningful changes you will likely need to edit that as well.

- Clone all relevant repos into the same directory (`git clone https://github.com/ppy/osu-tools`; `git clone https://github.com/ppy/osu`)
- Run the `./UseLocalOsu.ps1` powershell script (or `./UseLocalOsu.sh`) to use your local copy of ppy/osu

# Contributing

Contributions can be made via pull requests to this repository. We hope to credit and reward larger contributions via a [bounty system](https://www.bountysource.com/teams/ppy). If you're unsure of what you can help with, check out the [list of open issues](https://github.com/ppy/osu-tools/issues).

Note that while we already have certain standards in place, nothing is set in stone. If you have an issue with the way code is structured; with any libraries we are using; with any processes involved with contributing, *please* bring it up. I welcome all feedback so we can make contributing to this project as pain-free as possible.

# Licence

The osu! client code, framework, and tools are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Please note that this *does not cover* the usage of the "osu!" or "ppy" branding in any software, resources, advertising or promotion, as this is protected by trademark law.

Please also note that game resources are covered by a separate licence. Please see the [ppy/osu-resources](https://github.com/ppy/osu-resources) repository for clarifications.
