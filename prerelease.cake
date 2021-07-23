#tool "nuget:https://www.myget.org/F/wyam?package=Wyam&prerelease&version=2.1.1-build-20181217-3"
#addin "nuget:https://www.myget.org/F/wyam?package=Cake.Wyam&prerelease&version=2.1.1-build-20181217-3"
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
        DeleteDirectory(sourceDir, new DeleteDirectorySettings
        {
            Force = true,
            Recursive = true
        });
    }    
});

Task("GetSource")
    .IsDependentOn("CleanSource")
    .Does(() =>
    {
        var githubToken = EnvironmentVariable("WYAM_GITHUB_TOKEN");
        GitHubClient github = new GitHubClient(new ProductHeaderValue("WyamDocs"))
        {
            Credentials = new Credentials(githubToken)
        };
	    // The GitHub releases API returns Not Found if all are pre-release, so need workaround below
        //Release release = github.Repository.Release.GetLatest("Wyamio", "Wyam").Result;        
	    Release release = github.Repository.Release.GetAll("Wyam2", "Wyam").Result.First();
	    FilePath releaseZip = DownloadFile(release.ZipballUrl);
        Unzip(releaseZip, releaseDir);
        
        // Need to rename the container directory in the zip file to something consistent
        var containerDir = GetDirectories(releaseDir.Path.FullPath + "/*").First(x => x.GetDirectoryName().StartsWith("Wyam2"));
        MoveDirectory(containerDir, sourceDir);
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
                Recipe = recipe.GetDirectoryName() + " -i",
                RootPath = scaffold,
                ArgumentCustomization = args => args.Prepend("new"),
                NuGetPackages = new []
                {
                    "Wyam2." + recipe.GetDirectoryName(),
                    "Wyam2.Markdown",
                    "Wyam2.Razor",
                    "Wyam2.Yaml",
                    "Wyam2.CodeAnalysis",
                    "Wyam2.Less",
                    "Wyam2.Html"
                }.Select(x => new NuGetSettings
                {
                    Prerelease = true,
                    Source = new [] { "https://www.myget.org/F/wyam/api/v3/index.json" },
                    Package = x
                }),
                UpdatePackages = true
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
                        RootPath = scaffold,
                        Theme = theme.GetFilenameWithoutExtension().FullPath + " -i",
                        OutputPath = MakeAbsolute(Directory("./output" + linkRoot)).FullPath,
                        Settings = new Dictionary<string, object>
                        {
                            { "LinkRoot", linkRoot }
                        },
                        NuGetPackages = new []
                        {
                            "Wyam2." + recipe.GetDirectoryName(),
                            "Wyam2." + recipe.GetDirectoryName() + "." + theme.GetFilenameWithoutExtension().FullPath,
                            "Wyam2.Markdown",
                            "Wyam2.Razor",
                            "Wyam2.Yaml",
                            "Wyam2.CodeAnalysis",
                            "Wyam2.Less",
                            "Wyam2.Html"
                        }.Select(x => new NuGetSettings
                        {
                            Prerelease = true,
                            Source = new [] { "https://www.myget.org/F/wyam/api/v3/index.json" },
                            Package = x
                        }),
                        UpdatePackages = true
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
    
Task("Preview")
    .IsDependentOn("GetSource")
    //.IsDependentOn("Generate-Themes")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            //NoClean = true,  // Cleaned in Generate-Themes task
            Recipe = "Docs -i",
            Theme = "Samson -i",
            NuGetPackages = new []
            {
                "Wyam2.Docs",
                "Wyam2.Docs.Samson",
                "Wyam2.Markdown",
                "Wyam2.Razor",
                "Wyam2.Yaml",
                "Wyam2.CodeAnalysis",
                "Wyam2.Less",
                "Wyam2.Html"
            }.Select(x => new NuGetSettings
            {
                Prerelease = true,
                Source = new [] { "https://www.myget.org/F/wyam/api/v3/index.json" },
                Package = x
            }),
            UpdatePackages = true,
            Preview = true,
            Watch = true
        });
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Preview");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
