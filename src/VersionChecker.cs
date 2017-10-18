using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hjoellund.DotNet.Cli.Update.Models;
using Hjoellund.DotNet.Cli.Update.Options;
using NuGet.Common;
using NuGet.Protocol.Core.Types;

namespace Hjoellund.DotNet.Cli.Update
{
    internal class VersionChecker
    {
        public static async Task<UpdateStatus> CheckUpdateStatusAsync(PackageReference reference, PackageOptions options, string projectName, IEnumerable<SourceRepository> repositories)
        {
            ILogger logger = options.Verbose ? ConsoleLogger.Instance : NullLogger.Instance;

            foreach (var repository in repositories)
            {
                var metadataResource = await repository.GetResourceAsync<MetadataResource>();
                var latestVersion = await metadataResource.GetLatestVersion(reference.PackageId, options.UsePreRelease, false, logger, CancellationToken.None);

                if (latestVersion is null)
                    continue;

                if (ConstrainedVersionComparer.IsNewer(reference.Version, latestVersion, options.VersionConstraint))
                    return new UpdateStatus
                    {
                        ProjectName = projectName,
                        PackageId = reference.PackageId,
                        CurrentVersion = reference.Version,
                        UpdatedVersion = latestVersion
                    };

                return new UpdateStatus
                {
                    ProjectName = projectName,
                    PackageId = reference.PackageId,
                    CurrentVersion = reference.Version
                };
            }

            return new UpdateStatus
            {
                ProjectName = projectName,
                PackageId = reference.PackageId,
                CurrentVersion = reference.Version
            };
        }
    }
}