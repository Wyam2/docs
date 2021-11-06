Title: AppVeyor for Continuous Integration
Description: How to use GitHub Pages and AppVeyor to enable a free continuous integration environment for your site.
RedirectFrom: knowledgebase/continuous-integration
---
It's relatively easy to achieve site deployment nirvana with Wyam, GitHub Pages, and AppVeyor. Follow these instructions to get up and running:

1. Create a repository for your web site code if you haven't already done so. You should typically structure this with a default `input` folder under the root where your web site content will go, but you can adjust these instructions and the call to Wyam if you want to use a different folder name.

2. You'll probably want to add a `.gitignore` file that excludes the `output` folder and `packages.xml`, that way you can execute Wyam locally for testing without checking in the results.

```
output/
```

3. Set up GitHub Pages for your repository by adding an "orphaned" `gh-pages` branch ([instructions here](https://help.github.com/articles/creating-project-pages-manually/)). You'll need to add an initial file to the new orphaned branch before it'll push to GitHub, a mostly empty text file will work fine.

4. Switch back to the `master` branch and add the following `appveyor.yml` build script. This tells AppVeyor how to download the latest version of Wyam, use it to build your site, switch to the new `gh-pages` branch, and push the results of the build.

```
branches:
  only:
    - master
    
environment:
  access_token:
    # EDIT the encrypted version of your GitHub access token
    secure: ABCDEFG...

install:
  - git submodule update --init --recursive
  - mkdir ..\Wyam
  - mkdir ..\output
  # Fetch the latest version of Wyam2 
  - "curl -s https://raw.githubusercontent.com/Wyam2/Wyam/master/RELEASE -o ..\\Wyam\\wyamversion.txt"
  - set /P WYAMVERSION=< ..\Wyam\wyamversion.txt
  - echo %WYAMVERSION%
  # Get and unzip the latest version of Wyam
  - ps: Start-FileDownload "https://github.com/Wyam2/Wyam/releases/download/$env:WYAMVERSION/Wyam2-$env:WYAMVERSION.zip" -FileName "..\Wyam\Wyam2.zip"
  - 7z x ..\Wyam\Wyam2.zip -o..\Wyam -r

build_script:
  - dotnet ..\Wyam\Wyam.dll --output ..\output

on_success:
  # Switch branches to gh-pages, clean the folder, copy everything in from the Wyam2 output, and commit/push
  # See http://www.appveyor.com/docs/how-to/git-push for more info
  - git config --global credential.helper store
  # EDIT your Git email and name
  - git config --global user.email $env:op_build_user_email
  - git config --global user.name $env:op_build_user
  - ps: Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:access_token):x-oauth-basic@github.com`n"
  - git checkout gh-pages
  - git rm -rf .
  - xcopy ..\output . /E
  # EDIT your domain name or remove if not using a custom domain
  - echo wyam2.github.io > CNAME
  # EDIT the origin of your repository - have to reset it here because AppVeyor pulls from SSH, but GitHub won't accept SSH pushes
  - git remote set-url origin https://github.com/Wyam2/docs.git
  - git add -A
  - git commit -a -m "Commit from AppVeyor"
  - git push

```

Note that you will need to replace the `secure` value with your own GitHub access token as [described here](http://www.appveyor.com/docs/how-to/git-push). You will also need to encrypt your token in [AppVeyor encrypt tool](https://ci.appveyor.com/tools/encrypt). Also look for the other `EDIT` comments in the file and change as appropriate.

5. Create an account on AppVeyor if you haven't done so and add the GitHub repository as a new project. AppVeyor will pick up commits to the repository, run the build script above, and publish your new site on every commit!

While this guide is for GitHub Pages and AppVeyor, the same process should work pretty much the same way for alternate hosts and/or alternate build servers.
