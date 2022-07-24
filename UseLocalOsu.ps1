# Run this script to use a local copy of osu rather than fetching it from nuget.
# It expects the osu directory to be at the same level as the osu-tools directory

$PROJECTS=@(
    "PerformanceCalculator/PerformanceCalculator.csproj"
    "PerformanceCalculatorGUI/PerformanceCalculatorGUI.csproj"
)

$SLN="osu.Tools.sln"

$DEPENDENCIES=@(
    "..\osu\osu.Game.Rulesets.Catch\osu.Game.Rulesets.Catch.csproj"
    "..\osu\osu.Game.Rulesets.Mania\osu.Game.Rulesets.Mania.csproj"
    "..\osu\osu.Game.Rulesets.Osu\osu.Game.Rulesets.Osu.csproj"
    "..\osu\osu.Game.Rulesets.Taiko\osu.Game.Rulesets.Taiko.csproj"
    "..\osu\osu.Game\osu.Game.csproj"
)

dotnet sln $SLN add $DEPENDENCIES

ForEach ($CSPROJ in $PROJECTS)
{
    dotnet remove $CSPROJ package ppy.osu.Game
    dotnet remove $CSPROJ package ppy.osu.Game.Rulesets.Osu
    dotnet remove $CSPROJ package ppy.osu.Game.Rulesets.Taiko
    dotnet remove $CSPROJ package ppy.osu.Game.Rulesets.Catch
    dotnet remove $CSPROJ package ppy.osu.Game.Rulesets.Mania

    dotnet add $CSPROJ reference $DEPENDENCIES
}
