Title: Creating A Theme
Description: Explains how to create and distribute your own theme.
---
Creating a theme is easy. One of the most helpful things to keep in mind is that there is no behind-the-scenes magic at work. The built-in themes use the exact same mechanisms and conventions you have access to.

A theme [is just a collection of input files](/docs/concepts/themes). They work by being in an input folder at a lower precedence than the main input folder. You can even start working on your theme using files in your normal `input` path and then package them up later.

Most themes are tied to a specific [recipe](/recipes) since each recipe acts like an independent site generator with different conventions, types of output, and so on. The easiest way to get started writing a theme for one of the built-in recipes is to copy one of the [existing themes](https://github.com/Wyam2/wyam/tree/develop/themes) into your `input` folder and then start editing it from there. If you're developing a theme for a built-in recipe, also note that as the Wyam and recipe version increments, so will the theme files and thus old theme files may not work anymore due to changes in the recipe. In this case you'll need to examine the differences (if any) between the theme files you started with and the most recent versions and then port those changes into your own theme to maintain compatibility with an updated recipe version.

Once you've got the input files exactly as you like them, package them up as a [NuGet package with content files](http://blog.nuget.org/20160126/nuget-contentFiles-demystified.html). When such a package is included in a Wyam2 generation (either by using the `--nuget` [command line option](/docs/usage/command-line) or by using the `#nuget` preprocessor directive in a [configuration file](/docs/usage/configuration)), it will automatically make the files in the content folder of the package available as a low precedence input path.

Note that the built-in themes rely on a lookup table to map the theme name such as "CleanBlog" to the matching NuGet package (in this case [Wyam2.Blog.CleanBlog](https://www.nuget.org/packages/Wyam2.Blog.CleanBlog/)). That's why you can use the `--theme` command line option with a simple theme name for built-in themes. While this lookup table is currently limited only to built-in themes, the net effect is exactly the same as if including the theme package using the NuGet command line option or preprocessor directive. To let others use your theme, just advertise the NuGet package and they can include it via NuGet.

# Notes

## Links

A theme needs to support generating relative links that may (or may not be) under a [virtual directory](/docs/deployment/serving-from-a-subdirectory). The `IExecutionContext` has an extension method that helps resolve links to their appropriate output path. In [Razor](/modules/razor) the call looks like:

```
@Context.GetLink("/assets/js/some-script.js")
```

Use this anywhere you would normally put a relative path in your theme. For example, if you have a file in your input folder at "input/assets/js/some-script.js" and it's getting copied directly to the output folder, a link will be generated to "/assets/js/some-script.js" if no virtual directory is being used, but the generated link will be "/virtual/assets/js/some-script.js" if a virtual directory of "virtual" is used.