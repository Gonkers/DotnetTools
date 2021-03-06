using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gonkers.NuGetTools.NextPatchVersion
{
    public interface INuGetService
    {
        Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageId);
    }

    public class NuGetService : INuGetService
    {
        private readonly SourceRepository _sourceRepository;
        
        public NuGetService(Uri nugetFeedUri)
        {
            if (nugetFeedUri is null) throw new ArgumentNullException(nameof(nugetFeedUri));
            _sourceRepository = Repository.Factory.GetCoreV3(nugetFeedUri.AbsoluteUri);
        }

        public async Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageId)
        {
            var resource = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>();
            var versions = await resource.GetAllVersionsAsync(packageId, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);
            return versions;
        }
    }
}
