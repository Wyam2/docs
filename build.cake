// The following environment variables need to be set for Publish target:
// WYAM_GITHUB_TOKEN

#addin "nuget:https://api.nuget.org/v3/index.json?package=Octokit&version=0.50.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=LibGit2Sharp&version=0.27.0-preview-0116&prerelease"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Kudu&version=1.0.1"

#tool "nuget:https://api.nuget.org/v3/index.json?package=KuduSync.NET&version=1.5.3"

using Octokit;
using LibGit2Sharp;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

bool buildWyam = Argument<bool>("buildWyam", true);
var localWyam = Argument<string>("localWyam", string.Empty);
var localWyamSite = Argument<string>("localWyamSite", string.Empty);

string gitTag = EnvironmentVariable("GITHUB_TAG") ?? Argument<string>("tag", string.Empty);
string gitRef = EnvironmentVariable("GITHUB_REF") ?? Argument<string>("ref", string.Empty);
string gitSha = EnvironmentVariable("GITHUB_SHA") ?? Argument<string>("sha", string.Empty);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
bool useLocalWyam = !string.IsNullOrEmpty(localWyam) && DirectoryExists(localWyam);
bool useLocalSite = !string.IsNullOrEmpty(localWyamSite);

// Define directories.
var releaseDir = Directory("./release");
var sourceDir = useLocalWyam ? Directory(localWyam) : releaseDir + Directory("repo");
var destDir = useLocalSite ? Directory(localWyamSite) : releaseDir + Directory("site");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean-Source")
    .WithCriteria(() => !useLocalWyam)
    .Does(() =>
    {
        if(DirectoryExists(sourceDir))
        {
            DeleteDirectory(sourceDir, new DeleteDirectorySettings
            {
                Force = true,
                Recursive = true
            });
        }    
    });

Task("Get-Source")
    .IsDependentOn("Clean-Source")
    .WithCriteria(() => !useLocalWyam)
    .Does(() =>
    {
        LibGit2Sharp.Repository.Init(sourceDir);
        using (var repository = new LibGit2Sharp.Repository(sourceDir))
        {
            string logMessage = string.Empty;

            repository.Config.Set("protocol.version", "2");
            repository.Network.Remotes.Add("origin", "https://github.com/Wyam2/wyam.git");

            var remote = repository.Network.Remotes["origin"];
            var refSpecs = remote.FetchRefSpecs;
            repository.Network.Fetch("origin",
                                    refSpecs.Select(x => x.Specification),
                                    new FetchOptions { Prune = true },
                                    logMessage);
            Information(logMessage);    
            Commands.Checkout(repository,
                                string.IsNullOrEmpty(gitRef) ? "refs/remotes/origin/main" : gitRef,
                                new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
        }
    });

Task("Build-Source")
    .IsDependentOn("Get-Source")
    .WithCriteria(() => useLocalWyam)
    .WithCriteria(() => buildWyam)
    .Does((ctx) => {
        var procSettings = new ProcessSettings{ 
            Arguments = ProcessArgumentBuilder.FromString($@"./build.ps1 -Target Package -ScriptArgs ('--tag={gitTag}')"),
            WorkingDirectory = sourceDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using(var process = StartAndReturnProcess("pwsh", procSettings))
        {
            process.WaitForExit();
            int exitCode = process.GetExitCode();
            if(exitCode != 0)
            {
                throw new CakeException($"Wyam2 build: pwsh {procSettings.Arguments.RenderSafe()} returned ({exitCode}) because {process.GetStandardError()}");
            }
        }
        Information("Wyam2 was succesfully build");
    });

Task("Load-WyamAddin")
    .Does(ctx =>{
        var filename = (new FilePath(".local.cake")).FullPath;
        string[] lines = new string[2]{
            $"#addin nuget:file://{sourceDir}/build/nuget/?package=Cake.Wyam2",
            $"#tool nuget:file://{sourceDir}/build/nuget/?package=Wyam2"
        };

        System.IO.File.WriteAllLines(filename, lines);        
    });

Task("Build-Docs")
    .IsDependentOn("Load-WyamAddin")
    .Does((ctx) => {
        var procSettings = new ProcessSettings{ 
            Arguments = ProcessArgumentBuilder.FromString($@"./build.ps1 -ToolsProj "".\tools\docs.csproj"" -Script docs.cake -Target Generate-Docs -ScriptArgs ('--tag={gitTag} --wyam-path=""{sourceDir}/build/nuget""')"),
            WorkingDirectory = sourceDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using(var process = StartAndReturnProcess("pwsh", procSettings))
        {
            process.WaitForExit();
            int exitCode = process.GetExitCode();
            if(exitCode != 0)
            {
                throw new CakeException($"Wyam2 build: pwsh {procSettings.Arguments.RenderSafe()} returned ({exitCode}) because {process.GetStandardError()}");
            }
        }
        Information("Wyam2 was succesfully build");
    });

Task("Debug")
    .IsDependentOn("Build-Source")
    .Does(() =>
    {
        DotNetCoreExecute($"{sourceDir}/tests/integration/Wyam.Examples.Tests/bin/Debug/netcoreapp2.1/Wyam.dll",
            $"-a \"{sourceDir}/tests/integration/Wyam.Examples.Tests/bin/Debug/netcoreapp2.1/**/*.dll\" -r \"docs -i\" -t \"{sourceDir}/themes/Docs/Samson\" -p");
    });

Task("Deploy")
    .Does(() =>
    {
        
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build-Source")
    .IsDependentOn("Build-Docs")
    .Does(() =>{
        Information("Default");
    });
    
Task("BuildServer")
    .IsDependentOn("Build-Source")
    .IsDependentOn("Build-Docs")
    .IsDependentOn("Deploy");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
