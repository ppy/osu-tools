# Performance Calculator

A tool for calculating the difficulty of beatmaps and the performance of replays.

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
> dotnet run -- --help

Usage: dotnet PerformanceCalculator.dll [command] [options]

Options:
  -?|-h|--help  Show help information.

Commands:
  difficulty    Computes the difficulty of a beatmap.
  leaderboard   Computes the performance (pp) for every player in a part of the leaderboard.
  performance   Computes the performance (pp) of replays on a beatmap.
  profile       Computes the total performance (pp) of a profile.
  simulate      Computes the performance (pp) of a simulated play.

Run 'dotnet PerformanceCalculator.dll [command] -?|-h|--help' for more information about a command.
```

### Difficulty
```
> dotnet run -- difficulty --help

Computes the difficulty of a beatmap.

Usage: dotnet PerformanceCalculator.dll difficulty [arguments] [options]

Arguments:
  path                       Required. A beatmap file (.osu), beatmap ID, or a folder containing .osu files to compute the difficulty for.

Options:
  -?|-h|--help               Show help information
  -r|--ruleset:<ruleset-id>  Optional. The ruleset to compute the beatmap difficulty for, if it's a convertible beatmap.
                             Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania
  -m|--m <mod>               One for each mod. The mods to compute the difficulty with.Values: hr, dt, hd, fl, ez, 4k, 5k, etc...
  -o|--output <file.txt>     Output results to text file.
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
> dotnet run -- performance --help

Computes the performance (pp) of replays on a beatmap.

Usage: dotnet PerformanceCalculator.dll performance [arguments] [options]

Arguments:
  beatmap                 Required. A beatmap file (.osu) or beatmap ID corresponding to the replays.

Options:
  -?|-h|--help            Show help information
  -r|--replay <file>      One for each replay. The replay file.
  -o|--output <file.txt>  Output results to text file.
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
> dotnet run -- profile --help

Computes the total performance (pp) of a profile.

Usage: dotnet PerformanceCalculator.dll profile [arguments] [options]

Arguments:
  user                       User ID is preferred, but username should also work.
  api client ID              API Client ID, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application
  api client secret          API Client Secret, which you can get from here: https://osu.ppy.sh/home/account/edit#new-oauth-application

Options:
  -?|-h|--help               Show help information
  -r|--ruleset:<ruleset-id>  The ruleset to compute the profile for. 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania. Defaults to osu!.
  -j|--json                  Output results as JSON.
  -o|--output <file.txt>     Output results to text file.
```

Computes the performance of a user profile's performance. Takes 100 top plays of a user and recalculates and reorders them in order of the performance calculator's calculated performance.

```
User:     peppy
Live PP:  830.2 (including 125.5pp from playcount)
Local PP: 830.2 (-)

╔══╤═════════════════════════════════════════════════════════════════════════════════════════════════╤═════════╤════════╤══════╤════╤═══════╤════════╤═════════╤═══════════════╗
║# │beatmap                                                                                          │max combo│accuracy│misses│mods│live pp│local pp│pp change│position change║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║1 │22423 - Lix - Tori no Uta -Ethereal House Mix- (James) [Hard]                                    │ 450/450x│    100%│     0│  PF│   70.2│    70.2│      0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║2 │266885 - Owl City - When Can I See You Again? (Aleks719) [Next year!]                            │ 712/713x│  98.48%│     0│None│   55.8│    55.8│      0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║3 │258467 - Global Deejays - The Sound of San Francisco (Sey) [San Francisco]                       │ 349/349x│    100%│     0│None│   53.3│    53.3│      0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║4 │197337 - Sakamoto Maaya - Platinum (TV Size) (Flask) [Insane]                                    │ 388/426x│  94.31%│     3│None│   51.4│    51.4│     -0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║5 │119488 - Nakamura Mamechiyo - Kare Kano Kanon (mjw5150) [Hard]                                   │ 589/589x│  99.12%│     0│None│   48.9│    48.9│     -0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║6 │417911 - yanaginagi - Tokohana (TV Size) (Sharlo) [Fycho's Hard]                                 │ 359/359x│  98.75%│     0│None│   43.9│    43.9│     -0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║7 │8708 - Chatmonchy - Make Up! Make Up! (peppy) [Hard]                                             │ 447/447x│  99.76%│     0│None│   40.9│    40.9│     -0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║8 │134511 - livetune feat. Hatsune Miku - Tell Your World (KanaRin) [Karen]                         │ 228/265x│  97.56%│     0│None│   39.5│    39.5│      0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║9 │252488 - Y&Co. feat. karin - Sweet Rain (nanda2009) [Hard]                                       │ 436/460x│  98.36%│     2│None│   36.7│    36.7│      0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
║10│80 - Scatman John - Scatman (Extor) [Insane]                                                     │ 417/466x│  96.38%│     2│None│   36.4│    36.4│      0.0│       -       ║
╟──┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────┼────────┼──────┼────┼───────┼────────┼─────────┼───────────────╢
...
```

### Simulate
```
> dotnet run -- simulate --help

Computes the performance (pp) of a simulated play.

Usage: dotnet PerformanceCalculator.dll simulate [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  mania         Computes the performance (pp) of a simulated osu!mania play.
  osu           Computes the performance (pp) of a simulated osu! play.
  taiko         Computes the performance (pp) of a simulated osu!taiko play.

Run 'simulate [command] --help' for more information about a command.
```

Computes the performance of a simulated play on a beatmap. The provided output includes raw performance attributes and pp value.

#### osu!
```
> dotnet run -- simulate osu --help

Computes the performance (pp) of a simulated osu! play.

Usage: dotnet PerformanceCalculator.dll simulate osu <beatmap> [options]

Arguments:
  beatmap                     Required. Can be either a path to beatmap file (.osu) or beatmap ID.

Options:
  -?|-h|--help                Show help information.
  -a|--accuracy <accuracy>    Accuracy. Enter as decimal 0-100. Defaults to 100. Scales hit results as well and is rounded to the nearest possible value for the beatmap.
  -c|--combo <combo>          Maximum combo during play. Defaults to beatmap maximum.
  -C|--percent-combo <combo>  Percentage of beatmap maximum combo achieved. Alternative to combo option. Enter as decimal 0-100.
  -m|--mod <mod>              One for each mod. The mods to compute the performance with. Values: hr, dt, hd, fl, ez, etc...
  -X|--misses <misses>        Number of misses. Defaults to 0.
  -M|--mehs <mehs>            Number of mehs. Will override accuracy if used. Otherwise is automatically calculated.
  -G|--goods <goods>          Number of goods. Will override accuracy if used. Otherwise is automatically calculated.
  -j|--json                   Output results as JSON.
  -o|--output <file.txt>      Output results to text file.
```

#### osu!taiko
```
> dotnet run -- simulate taiko --help

Computes the performance (pp) of a simulated osu!taiko play.

Usage: dotnet PerformanceCalculator.dll simulate taiko <beatmap> [options]

Arguments:
  beatmap                     Required. Can be either a path to beatmap file (.osu) or beatmap ID.

Options:
  -?|-h|--help                Show help information.
  -a|--accuracy <accuracy>    Accuracy. Enter as decimal 0-100. Defaults to 100. Scales hit results as well and is rounded to the nearest possible value for the beatmap.
  -c|--combo <combo>          Maximum combo during play. Defaults to beatmap maximum.
  -C|--percent-combo <combo>  Percentage of beatmap maximum combo achieved. Alternative to combo option. Enter as decimal 0-100.
  -m|--mod <mod>              One for each mod. The mods to compute the performance with. Values: hr, dt, hd, fl, ez, etc...
  -X|--misses <misses>        Number of misses. Defaults to 0.
  -G|--goods <goods>          Number of goods. Will override accuracy if used. Otherwise is automatically calculated.
  -j|--json                   Output results as JSON.
  -o|--output <file.txt>      Output results to text file.
```

#### osu!catch
```
> dotnet run -- simulate catch --help

Computes the performance (pp) of a simulated osu!catch play.

Usage: dotnet PerformanceCalculator.dll simulate catch <beatmap> [options]

Arguments:
  beatmap                     Required. Can be either a path to beatmap file (.osu) or beatmap ID.

Options:
  -?|-h|--help                Show help information.
  -a|--accuracy <accuracy>    Accuracy. Enter as decimal 0-100. Defaults to 100. Scales hit results as well and is rounded to the nearest possible value for the beatmap.
  -c|--combo <combo>          Maximum combo during play. Defaults to beatmap maximum.
  -C|--percent-combo <combo>  Percentage of beatmap maximum combo achieved. Alternative to combo option. Enter as decimal 0-100.
  -m|--mod <mod>              One for each mod. The mods to compute the performance with. Values: hr, dt, hd, fl, ez, etc...
  -X|--misses <misses>        Number of misses. Defaults to 0.
  -T|--tiny-droplets <tinys>  Number of tiny droplets hit. Will override accuracy if used. Otherwise is automatically calculated.
  -D|--droplets <droplets>    Number of droplets hit. Will override accuracy if used. Otherwise is automatically calculated.
  -j|--json                   Output results as JSON.
  -o|--output <file.txt>      Output results to text file.
```


#### osu!mania
```
> dotnet run -- simulate mania --help

Computes the performance (pp) of a simulated osu!mania play.

Usage: dotnet PerformanceCalculator.dll simulate mania <beatmap> [options]

Arguments:
  beatmap                 Required. Can be either a path to beatmap file (.osu) or beatmap ID.

Options:
  -?|-h|--help            Show help information.
  -s|--score <score>      Score. An integer 0-1000000.
  -m|--mod <mod>          One for each mod. The mods to compute the performance with. Values: hr, dt, fl, 4k, 5k, etc...
  -j|--json               Output results as JSON.
  -o|--output <file.txt>  Output results to text file.
```

### Leaderboard
```
> dotnet run -- leaderboard --help

Computes the performance (pp) for every player in a part of the leaderboard.

Usage: dotnet PerformanceCalculator.dll leaderboard [options] <client id> <client secret>

Arguments:
  client id                       API Client ID, which you can get from here:
                                  https://osu.ppy.sh/home/account/edit#new-oauth-application
  client secret                   API Client Secret, which you can get from here:
                                  https://osu.ppy.sh/home/account/edit#new-oauth-application

Options:
  -?|-h|--help                    Show help information.
  -r|--ruleset <ruleset-id>       The ruleset to compute the leaderboard for.
                                  Values: 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania
                                  Allowed values are: 0, 1, 2, 3.
  -l|--limit <amount-of-players>  How many players to compute (max. 50)
  -p|--page <page-number>         Leaderboard page number.
  -j|--json                       Output results as JSON.
  -o|--output <file.txt>          Output results to text file.
```