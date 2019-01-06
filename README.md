# osu!tools [![Build status](https://ci.appveyor.com/api/projects/status/70owdbhaaepp70u5?svg=true)](https://ci.appveyor.com/project/peppy/osu-tools)  [![CodeFactor](https://www.codefactor.io/repository/github/ppy/osu-tools/badge)](https://www.codefactor.io/repository/github/ppy/osu-tools) [![dev chat](https://discordapp.com/api/guilds/188630481301012481/widget.png?style=shield)](https://discord.gg/ppy)

Tools for [osu!](https://osu.ppy.sh).

For more detailed information see per-tool READMEs.

- [PerformanceCalculator](https://github.com/ppy/osu-tools/blob/master/PerformanceCalculator/README.md) - A CLI tool for calculating the difficulty of beatmaps and the performance of replays.

# Requirements

- A desktop platform that can compile .NET 4.7.1. We recommend using [Visual Studio Community Edition](https://www.visualstudio.com/) (Windows), [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) (macOS) or [MonoDevelop](http://www.monodevelop.com/download/) (Linux), all of which are free. [Visual Studio Code](https://code.visualstudio.com/) may also be used but requires further setup steps which are not covered here.

# Getting Started
- Clone the repository including submodules (`git clone --recurse-submodules https://github.com/ppy/osu-tools`)
- Build in your IDE of choice (recommended IDEs automatically restore nuget packages; if you are using an alternative make sure to `nuget restore`)

# Contributing

Contributions can be made via pull requests to this repository. We hope to credit and reward larger contributions via a [bounty system](https://www.bountysource.com/teams/ppy). If you're unsure of what you can help with, check out the [list of open issues](https://github.com/ppy/osu-tools/issues).

Note that while we already have certain standards in place, nothing is set in stone. If you have an issue with the way code is structured; with any libraries we are using; with any processes involved with contributing, *please* bring it up. I welcome all feedback so we can make contributing to this project as pain-free as possible.

# Licence

The osu! client code, framework, and tools are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

Please note that this *does not cover* the usage of the "osu!" or "ppy" branding in any software, resources, advertising or promotion, as this is protected by trademark law.

Please also note that game resources are covered by a separate licence. Please see the [ppy/osu-resources](https://github.com/ppy/osu-resources) repository for clarifications.
