using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update
{
    internal class NuGetReference
    {
        public string PackageId { get; set; }
        public NuGetVersion Version { get; set; }
    }
}