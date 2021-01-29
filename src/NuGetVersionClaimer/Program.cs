using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGetVersionClaimer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("This tool will retrieve the next available version for a NuGet package from a given feed. This tool can be useful for a CI/CD pipeline.")
            {
                new Option<Uri>(new [] { "--nuget-source-url", "-n" }, $"The URI of the nuget source. Default: {Constants.NugetFeedUrl}"),
                new Option<FileInfo>(new [] { "--project-file", "-p" }, "The path to the project file to read the version and package ID."),
                new Option<bool>(new [] { "--json", "-j" }, () => false, "Output in JSON format.")
            };

            rootCommand.Name = "nuget-version-claimer";
            rootCommand.Handler = CommandHandler.Create<Uri, FileInfo, bool>(PrintNextNugetVersion);
            await rootCommand.InvokeAsync(args);
        }
        static async Task PrintNextNugetVersion(Uri nugetSourceUrl, FileInfo projectFile, bool json)
        {
            try
            {
                if (projectFile is null)
                {
                    var path = new DirectoryInfo(Directory.GetCurrentDirectory());
                    var projectFiles = path.GetFiles("*.csproj");
                    if (projectFiles.Length != 1)
                    {
                        Console.Error.WriteLine($"Unable to determine the project file to use for NuGet package information.");
                        Environment.Exit(Constants.ExitCodes.ProjectFileMissing);
                    }

                    projectFile = projectFiles[0];
                }

                var (packageId, version) = ExtractProjectData(projectFile);
                if (!NuGetVersion.TryParse(version, out var projectVersion)) projectVersion = new NuGetVersion("0.0.0");
                var nugetVersions = (await GetNugetVersions(packageId, nugetSourceUrl)).Where(v => v.Major == projectVersion.Major && v.Minor == projectVersion.Minor);
                var nextPatchVer = nugetVersions.Any() ? nugetVersions.Max(v => v.Patch) + 1 : 0;
                var newPackageVersion = new NuGetVersion(projectVersion.Major, projectVersion.Minor, nextPatchVer);

                if (json)
                    Console.WriteLine(JsonSerializer.Serialize(newPackageVersion));
                else
                    Console.WriteLine(newPackageVersion);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Environment.Exit(Constants.ExitCodes.UnknownError);
            }
        }

        static (string packageId, string version) ExtractProjectData(FileInfo projFile)
        {
            try
            {
                if (!projFile.Exists)
                {
                    Console.Error.WriteLine($"The project file '{projFile.FullName}' was not found.");
                    Environment.Exit(Constants.ExitCodes.ProjectFileMissing);
                }

                var xmlDoc = XDocument.Load(projFile.FullName);

                var packageId = xmlDoc.Descendants("PropertyGroup")
                    .SelectMany(pg => pg.Descendants())
                    .FirstOrDefault(e => e.Name.LocalName.Equals("PackageId", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    Console.Error.WriteLine($"Unable to determine the NuGet package ID from the project file '{projFile}'.");
                    Environment.Exit(Constants.ExitCodes.MissingPackageId);
                }

                var ver = xmlDoc.Descendants("PropertyGroup")
                    .SelectMany(pg => pg.Descendants())
                    .FirstOrDefault(e => e.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (string.IsNullOrWhiteSpace(ver))
                    ver = "0.0.0";

                return (packageId, ver);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to read the project file '{projFile.FullName}'. Error: {ex.Message}");
                Console.Error.WriteLine(ex);
                Environment.Exit(Constants.ExitCodes.ProjectFileCorrupt);
                return (null, null);
            }
        }

        static async Task<IEnumerable<NuGetVersion>> GetNugetVersions(string packageId, Uri nugetSourceUrl)
        {
            var repository = Repository.Factory.GetCoreV3(nugetSourceUrl?.AbsoluteUri ?? Constants.NugetFeedUrl);
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            var versions = await resource.GetAllVersionsAsync(packageId, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);
            return versions;
        }
    }
}
