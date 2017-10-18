using System;
using Hjoellund.DotNet.Cli.Update.Options;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update
{
    internal class ConstrainedVersionComparer
    {
        public static bool IsNewer(NuGetVersion currentVersion, NuGetVersion latestVersion, VersionConstraint versionConstraint)
        {
            switch (versionConstraint)
            {
                case VersionConstraint.Major:
                    return latestVersion > currentVersion;
                case VersionConstraint.Minor:
                    return latestVersion.Major == currentVersion.Major && latestVersion > currentVersion;
                case VersionConstraint.Patch:
                    return latestVersion.Major == currentVersion.Major && latestVersion.Minor == currentVersion.Minor && latestVersion > currentVersion;
                default:
                    throw new GracefulException($"Unknown version constraint encountered: {versionConstraint}");
            }
        }
    }
}