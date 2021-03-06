# Gonkers.Tools
This is a repository for .NET global tools that I've made and find useful. 

## Tool: next-patch-version
This is a .NET 5 global tool that will retrieve the next available NuGet package patch version on a given feed. As 
an example, if you own a package that has the versions 1.2.0, 1.2.1, 1.2.2 then the `next-patch-version` tool
will search nuget.org and return version 1.2.3 as the next available version. This can be useful for auto incrementing
NuGet package versions in a CI/CD pipeline.

### Install
```shell
dotnet build
dotnet tool install -g Gonkers.NuGetTools.NextPatchVersion --add-source=.\nupkg
```

There is more documentation and examples to come...
