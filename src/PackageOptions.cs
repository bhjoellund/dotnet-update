using CommandLine;

namespace Hjoellund.DotNet.Cli.Update
{
    internal class PackageOptions
    {
        [Option("prerelease", Default = false)]
        public bool UsePreRelease { get; set; }
        [Option("list", Default = false)]
        public bool ShowUpdateList { get; set; }
        [Option('v', "version-constraint", Default = VersionConstraint.Minor)]
        public VersionConstraint VersionConstraint { get; set; }
        [Value(0, MetaName = "Solution or project file path")]
        public string SolutionOrProjectPath { get; set; }
    }
}