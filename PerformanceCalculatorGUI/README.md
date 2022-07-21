# Performance Calculator GUI

A GUI tool for calculating the difficulty of beatmaps, changes in profile scores and leaderboards.

## Tweaking

Difficulty and performance calculators for all rulesets may be modified to tweak the output of the calculator. These exist in the following directories:

```
../osu/osu.Game.Rulesets.Osu/Difficulty
../osu/osu.Game.Rulesets.Taiko/Difficulty
../osu/osu.Game.Rulesets.Catch/Difficulty
../osu/osu.Game.Rulesets.Mania/Difficulty
```

If you run the tool using `dotnet watch` the calculations will update in realtime where possible.

## Usage

`dotnet run`