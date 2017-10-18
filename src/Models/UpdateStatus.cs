using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update.Models
{
    internal class UpdateStatus
    {
        public string ProjectName { get; set; }
        public string PackageId { get; set; }
        public NuGetVersion CurrentVersion { get; set; }
        public NuGetVersion UpdatedVersion { get; set; }
    }
}