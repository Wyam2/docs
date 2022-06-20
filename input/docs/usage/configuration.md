Title: Configuration
Description: Describes the format of the configuration file.
Order: 3
RedirectFrom: getting-started/configuration
---
The command line Wyam application reads a configuration file typically named `config.wyam` (though you can change that with an argument) that sets up the environment and configures the pipelines. Preprocessor directives can appear anywhere in the configuration file, though they are always evaluated before processing the rest of the file (by convention they're usually at the top of the file).

The configuration file is evaluated as C# code, so you can make use of the full C# language and the entire .NET ecosystem. However, it's not necessary to know C# to write Wyam configuration files. The syntax has been carefully crafted to be usable by anyone no matter their level of programming experience. Some extra pre-processing is also done to the file to make certain code easier to write (which actually makes the syntax a superset of C#, though this extra magic is entirely optional).

A configuration file typically looks like this:

```
// Preprocessor directives

// Body code
// ...
```

When Wyam starts, the configuration file is compiled by Roslyn (a C# compiler) and starting with Wyam 1.0 a cached copy of the resulting compilation is stored on disk at `config.wyam.dll` by default. A hash of the config file is also stored at `config.wyam.hash`. These two files improve startup performance by allowing Wyam to skip the compilation phase if the config file has previously been processed and its contents haven't changed.

# Preprocessor Directives

Preprocessor directives establish the Wyam environment and get evaluated before the rest of the configuration file. They're typically responsible for declaring things like NuGet packages and assemblies. Every preprocessor directive starts with `#` at the beginning of a line and extend for the rest of the line. The following directives are available (the current set of directives can always be seen by calling `wyam help --directives`).

```
Available preprocessor directives:

#nuget-source, #ns
Specifies an additional package source to use.

#recipe, #r
    -i, --ignore-known-packages    Ignores (does not add) packages for
                                   known recipes.
    <recipe>                       The recipe to use.
Specifies a recipe to use.

#assembly, #a
Adds an assembly reference by name, file name, or globbing pattern.

#theme, #t
    -i, --ignore-known-packages    Ignores (does not add) packages for
                                   known themes.
    <theme>                        The theme to use.
Specifies a theme to use.

#nuget, #n
    -p, --prerelease         Specifies that prerelease packages are
                             allowed.
    -u, --unlisted           Specifies that unlisted packages are
                             allowed.
    -v, --version <arg>      Specifies the version range of the package
                             to use.
    -l, --latest             Specifies that the latest available version
                             of the package should be used (this will
                             always trigger a request to the sources).
    -s, --source <arg>...    Specifies the package source(s) to get the
                             package from.
    -e, --exclusive          Indicates that only the specified package
                             source(s) should be used to find the
                             package.
    <package>                The package to install.
Adds a NuGet package (downloading and installing it if needed).
```

## NuGet Packages

Any NuGet packages you specify in preprocessor directives are installed and then scanned for modules which are made available to the main configuration body. By default, Wyam will attempt to match requested packages with those on disk and will use the disk-based package when available. To force it to use a specific version or version range (and download and install it if necessary), use the `--version <version>` flag. To force it to always download and install the latest available version on the package feed, use the `--latest` flag, which will always result in a call to the configured source(s).

Wyam follows the same conventions as NuGet with regards to [specifying version ranges](https://docs.nuget.org/create/versioning#specifying-version-ranges-in-.nuspec-files). Specifically, the following summarizes how to specify version ranges:

```
1.0  = 1.0 ≤ x
(,1.0]  = x ≤ 1.0
(,1.0)  = x < 1.0
[1.0] = x == 1.0
(1.0) = invalid
(1.0,) = 1.0 < x
(1.0,2.0) = 1.0 < x < 2.0
[1.0,2.0] = 1.0 ≤ x ≤ 2.0
```

Note that many modules require their package to be installed before they can be used. For example, to make use of the [Markdown](/modules/markdown) module, you must install the `Wyam2.Markdown` package. To do this, you would add the following to your configuration file:

```
#n Wyam.Markdown
``` 

You can also specify the special `Wyam2.All` package which will download all of the official Wyam module packages at once:

```
#n Wyam.All
```

All NuGet packages you specify, along with their dependencies, are located and downloaded at startup. Walking the full dependency tree can be time consuming since queries have to be issued to the NuGet server(s) for each individual package. To improve performance, the full set of packages and their dependencies is cached in a `packages.xml` file. If Wyam has already calculated the dependency tree for a given package and version, that entire tree will be stored in this cache file and it won't need to be rewalked on the next run. This means that the first execution of Wyam may take longer as the dependency cache is populated but subsiquent executions should be much faster.

## Assemblies

In addition to NuGet packages you can also load assemblies. All assemblies are loaded with the `#assembly` or `#a` directive. You can load all the assemblies in a directory by using a [globbing pattern](/docs/concepts/io#globbing), or by specifying a relative or absolute path to the assembly. You can also load assemblies by name. If you specify a short name, Wyam will attempt to resolve the assembly with the same version as the currently loaded framework. Keep in mind that non-framework assemblies located in the GAC *must* be loaded by full name (including the version, public key token, etc.).

By default, the following assemblies are already loaded so you don't need to explicitly specify them:
* `System`
* `System.Collections.Generic`
* `System.Linq`
* `System.Core`
* `Microsoft.CSharp`
* `System.IO`
* `System.Diagnostics`

# Declarations

The code in your configuration is executed inside the context of an "invisible" class and method. Note that any declarations such as classes or methods will be automatically "lifted" into an outer scope. In this way, the configuration file is more like a C# script than a normal code file. Any classes that are declared in the configuration file will be placed in the global scope. Any methods will be placed in the wrapping class outside the default wrapping method that contains the rest of the configuration code.

```
// "using" statements are placed in the outer scope
// and are available throughout the script
using System.IO;

// classes are also placed in the outer scope
public static class Helpers
{
    public static string GetWriteExtension()
    {
        return ".html";
    }
}

// normal code is placed in a wrapper class and method
// that gets called when the script is evaluated
Pipelines.Add("Markdown",
    ReadFiles("*.md"),
    FrontMatter(Yaml()),
    Markdown(),
    WriteFiles(Helpers.GetWriteExtension())
);
```

Note that namespaces for all found modules as well as the following namespaces are automatically brought into scope for every configuration script so you won't need to explicitly add them:

* `System`
* `System.Collections.Generic`
* `System.Linq`
* `System.IO`
* `System.Diagnostics`

# Pipelines

Configuring a pipeline is easy, and Wyam configuration files are designed to be simple and straightforward:
```
Pipelines.Add(
    ReadFiles("*.md"),
    Markdown(),
    WriteFiles(".html")
);
```
or
```
 //Disable API doc generation while in development
if(inDev)
{
    Pipelines.Remove(Docs.RenderApi);
    Pipelines.Remove(Docs.ApiIndex);
    Pipelines.Remove(Docs.ApiSearchIndex);
}
```
or
```
//Insert a new pipeline called "Test" that dumps the pipeline docs using a custom json serialization to the log file
Pipelines.InsertAfter("Reference", "Test",
    Documents(Docs.Api),
    ForEach(
        Trace(DocToJson)
    )
 );
```


However, don't let the simplicity fool you. Wyam configuration files are C# scripts and as such can make use of the full C# language and the entire .NET ecosystem (including the built-in support for NuGet and other assemblies as explained above). One of the core modules even lets you write a delegate right in your configuration file for extreme flexibility.

## Pipeline Names

Pipelines should be given names, which makes them easier to identify in trace messages and also makes them easier to refer to within templates (for example, to get all the documents generated by a previous pipeline). Just pass in the name of a pipeline as the first parameter to the `Add()` method. If no name is provided (as above), then pipelines will implicitly be given the names `Pipeline 1`, `Pipeline 2`, etc.

```
Pipelines.Add("Markdown",
    ReadFiles("*.md"),
    FrontMatter(Yaml()),
    Markdown(),
    WriteFiles(".html")
);
```

## Child Modules

Some modules also accept child modules as part of their processing. For example, the `FrontMatter` module accepts a child module to handle parsing whatever front matter content is found. This way, the same module can be used to recognize front matter without it having to worry about what kind of content the front matter contains (such as YAML or JSON). For example:

```
Pipelines.Add("Markdown",
    ReadFiles("*.md"),
    FrontMatter(Yaml()),
    Markdown(),
    WriteFiles(".html")
);
```

## Fluent Methods

Some modules allow method chaining to extend or modify their behavior. For example, the `OrderBy` module allows you to chain a call to `ThenBy()` if you would like to sort by multiple inputs. Or, you chain a call to `Descending()` if you would like to reverse the order.

```
Pipelines.Add("Markdown",
    ReadFiles("*.md"),
    FrontMatter(Yaml()),
    OrderBy(@doc.Get<DateTime>("Published"))
        .ThenBy(@doc["Title"])
        .Descending(),
    WriteFiles(".html")
);
```

## Skipping Previously Processed Documents

If you know that a given pipeline doesn't use data from other pipelines and you'd like to prevent reprocessing of documents after the first pass, you can set the `ProcessDocumentsOnce` flag. Under the hood, this looks for the first occurrence of a given `IDocument.Source` and then caches all final result documents that have the same source. On subsequent executions, if a document with a previously seen `IDocument.Source` is found *and it has the same content*, that document is removed from the module output and therefore won't get passed to the next module. At the end of the pipeline, all the documents from the first pass that have the same source as the removed one are added back to the result set (so later pipelines can still access them in the documents collection if needed). The `ProcessDocumentsOnce` flag can be set when creating a pipeline:

```
Pipelines.Add("Markdown",
    ReadFiles("*.md"),
    FrontMatter(Yaml()),
    Markdown(),
    WriteFiles(".html")
).WithProcessDocumentsOnce();
```

## Explicit Module Instantiation

Note that you supply the pipeline with new instances of each module. An astute reader will notice that in the example above, modules are being specified with what look like global methods. These methods are just shorthand for the actual module class constructors and this convention can be used for any module within the configuration script. The example configuration above could also have been written as:
```
Pipelines.Add("Markdown",
    new ReadFiles("*.md"),
    new Markdown(),
    new WriteFiles(".html")
);
```

## Automatic Lambda Generation

Many modules accept functions so that you can use information about the current `IExecutionContext` and/or `IDocument` when executing the module. For example, you may want to write files to disk in different locations depending on some value in each document's metadata. To make this easier in simple cases, and to assist users who may not be familiar with the [C# lambda expression syntax](https://msdn.microsoft.com/en-us/library/bb397687.aspx), the configuration file will automatically generate lambda expressions when using a special syntax. This generation will only happen for module constructors and fluent configuration methods. Any other method you use that requires a function will still have to specify it explicitly.

If the module or fluent configuration method has a `ContextConfig` delegate argument, you can instead use any variable name that starts with `@ctx`. For example:

```
Foo(@ctx2.InputFolder)
```
will be expanded to:

```
Foo(@ctx2 => @ctx2.InputFolder)
```

Likewise, any variable name that starts with `@doc` will be expanded to a `DocumentConfig` delegate. For example:

```
Foo(@doc["SomeMetadataValue"])
```
will be expanded to:

```
Foo((@doc, _) => @doc["SomeMetadataValue"])
```

If you use both `@ctx` and `@doc`, a `DocumentConfig` delegate will be generated that uses both values. For example:

```
Foo(@doc[@ctx.InputFolder])
```
will be expanded to:

```
Foo((@doc, @ctx) => @doc[@ctx.InputFolder])
```

If you need to use the `ContextConfig` delegate argument but do not need to access `@doc` or `@ctx` then you will need to specify the lambda. For example, one of the overrides for the `WriteFiles` module is a string to let you specify a file extension. However, we might want to override the output and write to a single file (with minified and combined CSS for example). To accomplish this, we will need to pass in the lambda, even thought we won't be using it.

```
WriteFiles((doc, ctx) => "css/style.css")
```

# Execution Ordering

Be aware that the configuration file only *configures* the pipelines. Each pipeline is executed in the order in which they were first added after the entire configuration file is evaluated. This means that you can't declare one pipeline, then declare another, and then add a new module to the first pipeline expecting it to reflect what happened in the second one. The second pipeline won't execute until the entire first pipeline is complete, including any modules that were added to it after the second one was declared. If you need to run some modules, switch to a different pipeline, and the perform additional processing on the first set of documents, look into the [Documents](/modules/documents) module.
