# Wyam2 Docs
The website for the Wyam2 static content generator: [wyam2.github.io](http://wyam2.github.io)

## Contributing
* Generate a [personal access token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/) from GitHub
* Set the environment variable `GH_ACCESS_TOKEN` with this token

```
PS C:\OSS\wyam2\docs> $env:GH_ACCESS_TOKEN="yourPersonalAccessToken"
PS C:\OSS\wyam2\docs> .\build.ps1
```
or if you need more details and save all build output to a file
```
PS C:\OSS\wyam2\docs> .\build.ps1 -Verbosity diagnostic *>&1 | Out-File 'build.log'
```