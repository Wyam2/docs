// The following environment variables need to be set for Publish target:
// GH_ACCESS_TOKEN

#tool "nuget:https://api.nuget.org/v3/index.json?package=Wyam2&version=3.0.0&prerelease"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Wyam2&version=3.0.0&prerelease"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Octokit"

using Octokit;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var releaseDir = Directory("./release");
var sourceDir = releaseDir + Directory("repo");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("CleanSource")
    .Does(() =>
{
    if(DirectoryExists(sourceDir))
    {
        Information($"Deleting {sourceDir}");
        DeleteDirectory(sourceDir, new DeleteDirectorySettings
        {
            Force = true,
            Recursive = true
        });
    }  

    var subDirs = GetSubDirectories(releaseDir); 
    if(subDirs != null && subDirs.Count > 0)
    {
        foreach (var subDir in subDirs)
        {
            Information($"Deleting {subDir}");
            DeleteDirectory(subDir, new DeleteDirectorySettings
            {
                Force = true,
                Recursive = true
            });
        }
    }
});

Task("GetSource")
    .IsDependentOn("CleanSource")
    .Does(() =>
    {
        var githubToken = EnvironmentVariable("GH_ACCESS_TOKEN") ?? "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";
        GitHubClient github = new GitHubClient(new ProductHeaderValue("Wyam2-wyam-Docs"))
        {
            Credentials = new Credentials(githubToken)
        };
	    // The GitHub releases API returns Not Found for Wyam2. Better use tags
        var tag = github.Repository.GetAllTags("Wyam2", "wyam").Result.First();
	    FilePath releaseZip = DownloadFile(tag.ZipballUrl);
        Unzip(releaseZip, releaseDir);
        
        // Need to rename the container directory in the zip file to something consistent
        var containerDir = GetDirectories(releaseDir.Path.FullPath + "/*").First(x => x.GetDirectoryName().StartsWith("Wyam2"));
        MoveDirectory(containerDir, sourceDir);
        Information($"Downloaded and unzipped { GetFiles(sourceDir.Path.FullPath + "/**/*").Count } files in { GetSubDirectories(sourceDir).Count } directories");
    });
    
Task("Generate-Themes")
    .Does(() =>
    {
        // Clean the output directory
        var output = Directory("./output");
        if(DirectoryExists(output))
        {
            CleanDirectory(output);
        }

        // Clean/create the scaffold directory
        var scaffold = Directory("./scaffold");
        if(!DirectoryExists(scaffold))
        {
            CreateDirectory(scaffold);
        }

        // Iterate the recipes
        foreach(DirectoryPath recipe in GetDirectories("./input/recipes/*"))
        {
            // Scaffold the recipe into a temporary directory
            CleanDirectory(scaffold);
            Wyam(new WyamSettings
            {
                Recipe = recipe.GetDirectoryName(),
                RootPath = scaffold,
                ArgumentCustomization = args => args.Prepend("new") 
            });

            // Iterate the themes
            foreach(FilePath theme in GetFiles(recipe.FullPath + "/themes/*.md"))
            {
                // See if this is a built-in theme by checking for "Preview" metadata
                if(!Context.FileSystem
                    .GetFile(theme)
                    .ReadLines(Encoding.UTF8)
                    .TakeWhile(x => !x.StartsWith("---"))
                    .Any(x => x.StartsWith("Preview:")))
                {
                    // Build the theme preview
                    string linkRoot = "/recipes/" + recipe.GetDirectoryName() + "/themes/preview/" + theme.GetFilenameWithoutExtension().FullPath;
                    Wyam(new WyamSettings
                    {
                        Recipe = recipe.GetDirectoryName(),
                        RootPath = scaffold,
                        Theme = theme.GetFilenameWithoutExtension().FullPath,
                        OutputPath = MakeAbsolute(Directory("./output" + linkRoot)).FullPath,
                        Settings = new Dictionary<string, object>
                        {
                            { "LinkRoot", linkRoot }
                        }
                    });
                }
            }
        }
        CleanDirectory(scaffold);
        DeleteDirectory(scaffold, new DeleteDirectorySettings
        {
            Force = true,
            Recursive = true
        });
    });

Task("Build")
    .IsDependentOn("GetSource")
    .IsDependentOn("Generate-Themes")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            NoClean = true,  // Cleaned in Generate-Themes task
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = true
        });        
    });
    
Task("Preview")
    .IsDependentOn("Generate-Themes")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            NoClean = true,  // Cleaned in Generate-Themes task
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = true,
            Preview = true            
        });
    });

Task("Debug")
    .Does(() =>
    {
        DotNetCoreBuild("../Wyam/tests/integration/Wyam.Examples.Tests/Wyam.Examples.Tests.csproj");        
        DotNetCoreExecute("../Wyam/tests/integration/Wyam.Examples.Tests/bin/Debug/netcoreapp2.1/Wyam.dll",
            "-a \"../Wyam/tests/integration/Wyam.Examples.Tests/bin/Debug/netcoreapp2.1/**/*.dll\" -r \"docs -i\" -t \"../Wyam/themes/Docs/Samson\" -p");
    });

Task("Deploy")
    .Does(() => { Information("Ran Deploy target"); });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");
    
Task("BuildServer")
    .IsDependentOn("Build")
    .IsDependentOn("Deploy");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
