using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update.Models
{
    internal class PackageReference
    {
        public string PackageId { get; set; }
        public NuGetVersion Version { get; set; }
    }
}