using CommandLine;

namespace Hjoellund.DotNet.Cli.Update.Options
{
    internal class PackageOptions
    {
        [Option('v', "verbose", Default = false)]
        public bool Verbose { get; set; }
        [Option('l', "list", Default = false)]
        public bool ShowUpdateList { get; set; }
        [Option('p', "prerelease", Default = false)]
        public bool UsePreRelease { get; set; }
        [Option("version-constraint", Default = VersionConstraint.Minor, HelpText = "One of: Major, Minor, Patch")]
        public VersionConstraint VersionConstraint { get; set; }
        [Value(0, MetaName = "Solution or project file path")]
        public string SolutionOrProjectPath { get; set; }
    }
}