# Wyam2 Docs
The website for the Wyam2 static content generator: [wyam2.github.io](http://wyam2.github.io)

## Contributing
* Install latest version of Powershell (for `pwsh` to be available in command line)
    * SUSE and openSUSE users can install the powershell version for [RedHat](https://packages.microsoft.com/rhel/7/prod/) and ignoring dependency to openssl-libs ( `libopenssl1_0_0` must be installed)
* Generate a [personal access token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/) from GitHub
* Set the environment variable `WYAM_GITHUB_TOKEN` with this token

```
PS C:\OSS\wyam2\docs> $env:WYAM_GITHUB_TOKEN="yourPersonalAccessToken"
PS C:\OSS\wyam2\docs> .\build.ps1
```
or if you need more details and save all build output to a file
```
PS C:\OSS\wyam2\docs> .\build.ps1 -Verbosity diagnostic | Out-File 'build.log'
```