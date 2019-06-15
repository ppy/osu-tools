#addin "nuget:?package=CodeFileSanity&version=0.0.21"
#addin "nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2019.1.1"
#tool "nuget:?package=NVika.MSBuild&version=1.0.1"
var nVikaToolPath = GetFiles("./tools/NVika.MSBuild.*/tools/NVika.exe").First();

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");

var rootDirectory = new DirectoryPath("..");
var solution = rootDirectory.CombineWithFilePath("osu.Tools.sln");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Compile")
    .Does(() => {
        DotNetCoreBuild(solution.FullPath, new DotNetCoreBuildSettings {
            Configuration = configuration,
        });
    });

// windows only because both inspectcore and nvika depend on net45
Task("InspectCode")
    .WithCriteria(IsRunningOnWindows())
    .IsDependentOn("Compile")
    .Does(() => {
        InspectCode(solution, new InspectCodeSettings {
            CachesHome = "inspectcode",
            OutputFile = "inspectcodereport.xml",
        });

        int returnCode = StartProcess(nVikaToolPath, $@"parsereport ""inspectcodereport.xml"" --treatwarningsaserrors");
        if (returnCode != 0)
            throw new Exception($"inspectcode failed with return code {returnCode}");
    });

Task("CodeFileSanity")
    .Does(() => {
        ValidateCodeSanity(new ValidateCodeSanitySettings {
            RootDirectory = rootDirectory.FullPath,
            IsAppveyorBuild = AppVeyor.IsRunningOnAppVeyor
        });
    });

Task("Build")
    .IsDependentOn("CodeFileSanity")
    .IsDependentOn("InspectCode");

RunTarget(target);
