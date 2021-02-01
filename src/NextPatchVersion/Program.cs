using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gonkers.NuGetTools.NextPatchVersion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("This tool will retrieve the next available version for a NuGet package from a given feed. This tool can be useful for a CI/CD pipeline.")
            {
                new Option<Uri>(new [] { "--nuget-source-url", "-n" }, $"The URI of the nuget source. Default: {Constants.NugetFeedUrl}"),
                new Option<FileInfo>(new [] { "--project-file", "-p" }, "The path to the project file to read the version and package ID."),
                new Option<FileInfo>(new [] { "--nuspec-file" }, "The path to the project file to read the version and package ID."),
                new Option<bool>(new [] { "--json", "-j" }, () => false, "Output in JSON format.")
            };

            rootCommand.Name = "nuget-version-claimer";
            rootCommand.Handler = CommandHandler.Create<Uri, FileInfo, FileInfo, bool>(CliCommandHandler);
            await rootCommand.InvokeAsync(args);
        }

        static async Task<int> CliCommandHandler(Uri nugetSourceUrl, FileInfo projectFile, FileInfo nuspecFile, bool json)
        {
            INuGetPackageSettingsReader pkgSettingsReader;
            INuGetService nuGetService;

            try
            {
                if (projectFile is not null)
                    pkgSettingsReader = new ProjectFileReader(projectFile);
                else if (nuspecFile is not null)
                    pkgSettingsReader = new NuspecFileReader(nuspecFile);
                else
                {
                    var currentPath = new DirectoryInfo(Directory.GetCurrentDirectory());
                    var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };
                    var projectFiles = projectExtensions.SelectMany(ext => currentPath.GetFiles(ext)).ToArray();
                    var nuspecFiles = currentPath.GetFiles("*.nuspec");

                    if (projectFiles.Length == 1)
                        pkgSettingsReader = new ProjectFileReader(projectFiles[0]);
                    else if (nuspecFiles.Length == 1)
                        pkgSettingsReader = new NuspecFileReader(nuspecFiles[0]);
                    else
                    {
                        Console.Error.WriteLine($"Unable to determine the project file or nuspec file to use for NuGet package information.");
                        return Constants.ExitCodes.SettingsFileMissing;
                    }
                }

                if (nugetSourceUrl is null) nugetSourceUrl = new Uri(Constants.NugetFeedUrl);
                nuGetService = new NuGetService(nugetSourceUrl);

                var nextVersion = await GetNextNuGetVersion(pkgSettingsReader, nuGetService);
                if (json)
                    Console.WriteLine(JsonSerializer.Serialize(nextVersion));
                else
                    Console.WriteLine(nextVersion);

                return 0;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"Unable to determine the project file or nuspec file to use for NuGet package information. Error: {ex.Message}");
                return Constants.ExitCodes.SettingsFileMissing;
            }
            catch (InvalidDataException ex)
            {
                Console.Error.WriteLine($"Unable to read the project file or nuspec file to use for NuGet package information. Error: {ex.Message}");
                return Constants.ExitCodes.SettingsFileCorrupt;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Constants.ExitCodes.UnknownError;
            }
        }

        public static async Task<Version> GetNextNuGetVersion(INuGetPackageSettingsReader settingsReader, INuGetService nuGetService)
        {
            var packageSettings = await settingsReader.GetPackageSettings();
            var matchingNuGetVersions = (await nuGetService.GetPackageVersions(packageSettings.PackageId))
                .Where(v => v.Major == packageSettings.BaseVersion.Major && v.Minor == packageSettings.BaseVersion.Minor);
            var nextPatchVer = matchingNuGetVersions.Any() ? matchingNuGetVersions.Max(v => v.Patch) + 1 : 0;
            return new Version(packageSettings.BaseVersion.Major, packageSettings.BaseVersion.Minor, nextPatchVer);
        }
    }
}
