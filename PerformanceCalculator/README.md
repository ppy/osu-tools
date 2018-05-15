# Performance Calculator

A CLI tool for calculating the difficulty of beatmaps and the performance of replays.

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