# Performance Calculator

A CLI tool for calculating the difficulty of beatmaps and the performance of replays.

## Tweaking

Difficulty and performance calculators for all rulesets may be modified to tweak the output of the calculator. These exist in the following directories:

```
../osu/osu.Game.Rulesets.Osu/Difficulty
../osu/osu.Game.Rulesets.Taiko/Difficulty
../osu/osu.Game.Rulesets.Catch/Difficulty
../osu/osu.Game.Rulesets.Mania/Difficulty
```

## Usage

### Help
```
> dotnet PerformanceCalculator.dll

Usage: dotnet PerformanceCalculator.dll [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  difficulty    Computes the difficulty of a beatmap.
  performance   Computes the performance (pp) of replays on a beatmap.

Run 'dotnet PerformanceCalculator.dll [command] --help' for more information about a command.
```

### Difficulty
```
> dotnet PerformanceCalculator.dll difficulty --help

Computes the difficulty of a beatmap.

Usage: dotnet PerformanceCalculator.dll difficulty [arguments] [options]

Arguments:
  beatmap                    Required. The beatmap (.osu) to compute the difficulty for.

Options:
  -?|-h|--help               Show help information
  -r|--ruleset:<ruleset-id>  Optional. The ruleset to compute the beatmap difficulty for, if it's a convertible beatmap.
                             Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania
  -m|--m <mod>               One for each mod. The mods to compute the difficulty with.Values: hr, dt, hd, fl, ez, 4k, 5k, etc...
```

Computes the difficulty attributes of a beatmap. These attributes are used in performance calculation.

Example output of osu!-mode difficulty:
```
Aim            : 2.43558005796213
Speed          : 2.09240115454506
stars          : 4.69957066421573
```

### Performance
```
> dotnet PerformanceCalculator.dll performance --help

Computes the performance (pp) of replays on a beatmap.

Usage: dotnet PerformanceCalculator.dll performance [arguments] [options]

Arguments:
  beatmap             Required. The beatmap file corresponding to the replays.

Options:
  -?|-h|--help        Show help information
  -r|--replay <file>  One for each replay. The replay file.
```

Computes the performance of one or more replays on a beatmap. The provided output includes raw performance attributes alongside the un-weighted pp value.

```
Aim            : 123.614719845539
Speed          : 44.7315288123673
Accuracy       : 61.9354071284508
pp             : 235.580094436267
```

### Profile
```
> dotnet PerformanceCalculator.dll profile --help

Computes the total performance (pp) of a profile.

Usage: dotnet PerformanceCalculator.dll profile [arguments] [options]

Arguments:
  profile name         Required. Username of the osu account to be checked (not user id)
  api key              Required. API Key, which you can get from here: https://osu.ppy.sh/p/api
  path                 Required. Path to an open directory. Will create a txt file in that directory called ProfileCalculator.txt that will take up a few KB.

Options:
  -?|-h|--help         Show help information
  -b|--bonus <number>  Optional. Whether or not Bonus PP should be included. 1 is included, 0 is not included. Default is 0.
```

Computes the performance of a user profile's performance. Takes 100 top plays of a user on Bancho and recalculates and reorders them in order of the performance calculator's calculated performance.
```
1.Beatmap      : ARCIEN - Future Son (Mishima Yurara) [N A S Y A'S OK DAD]
Mods           : HD, DT
old/new pp     : 338.971 / 353.035079202098
2.Beatmap      : 07th Expansion - rog-unlimitation (AngelHoney) [AngelHoney]
Mods           : None
old/new pp     : 221.547 / 252.188414571819
...
100.Beatmap    : Feint - Vagrant (feat. Veela) (Aia) [Still Alive]
Mods           : None
old/new pp     : 144.025 / 145.889050437811
Top 100 Listed Above. Old/New Net PP: 3879.37586477693 / 4234.28970113726
```
