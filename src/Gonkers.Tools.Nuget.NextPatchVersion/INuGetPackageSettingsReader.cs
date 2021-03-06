using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gonkers.NuGetTools.NextPatchVersion
{
    public interface INuGetPackageSettingsReader
    {
        Task<NuGetPackageSettings> GetPackageSettings();
    }

    public class ProjectFileReader : INuGetPackageSettingsReader
    {
        private readonly FileInfo _projectFile;
        private readonly Stream _projectFileStream;

        public ProjectFileReader(FileInfo projectFile)
        {
            _projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
            if (!projectFile.Exists) throw new FileNotFoundException("The project file was not found.", projectFile.FullName);
        }

        public ProjectFileReader(Stream projectFileStream)
        {
            _projectFileStream = projectFileStream ?? throw new ArgumentNullException(nameof(projectFileStream));
        }

        public async Task<NuGetPackageSettings> GetPackageSettings()
        {
            var stream = _projectFileStream ?? _projectFile.OpenRead();

            try
            {
                var xmlDoc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                var packageId = xmlDoc.Descendants("PropertyGroup")
                    .SelectMany(pg => pg.Descendants())
                    .FirstOrDefault(e => e.Name.LocalName.Equals("PackageId", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (string.IsNullOrWhiteSpace(packageId))
                    throw new InvalidDataException($"Unable to determine the NuGet package ID from the project file provided.");

                var ver = xmlDoc.Descendants("PropertyGroup")
                    .SelectMany(pg => pg.Descendants())
                    .FirstOrDefault(e => e.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (string.IsNullOrWhiteSpace(ver))
                    ver = "0.0.0";

                return new NuGetPackageSettings
                {
                    PackageId = packageId,
                    BaseVersion = new Version(ver)
                };
            }
            finally
            {
                if (_projectFileStream is null) // If we own the Stream, dispose it
                    await stream.DisposeAsync();
            }
        }
    }

    public class NuspecFileReader : INuGetPackageSettingsReader
    {
        public NuspecFileReader(FileInfo nuspecFile)
        {

        }

        public Task<NuGetPackageSettings> GetPackageSettings()
        {
            throw new NotImplementedException();
        }
    }
}
