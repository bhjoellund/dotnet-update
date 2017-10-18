using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Hjoellund.DotNet.Cli.Update.Models;
using Hjoellund.DotNet.Cli.Update.Options;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var parser = new Parser(ps =>
            {
                ps.CaseInsensitiveEnumValues = true;
                ps.CaseSensitive = true;
                ps.HelpWriter = Console.Error;
                ps.IgnoreUnknownArguments = false;
            });
            switch (parser.ParseArguments<PackageOptions>(args))
            {
                case NotParsed<PackageOptions> notParsed:
                    CommandLine.Text.HelpText.AutoBuild(notParsed);
                    break;
                case Parsed<PackageOptions> parsed:
                    await CheckForUpdates(parsed.Value);
                    break;
            }
        }

        private static async Task CheckForUpdates(PackageOptions options)
        {
            try
            {
                await Process(options);
            }
            catch (GracefulException gex)
            {
                var errorConsole = Microsoft.DotNet.Cli.Utils.AnsiConsole.GetError();
                errorConsole.WriteLine(AnsiColorExtensions.Red(gex.Message));
            }
            catch (Exception ex)
            {
                var errorConsole = Microsoft.DotNet.Cli.Utils.AnsiConsole.GetError();
                errorConsole.WriteLine(AnsiColorExtensions.Yellow(ex.ToString()));
            }
        }

        private static async Task Process(PackageOptions options)
        {
            if (options.ShowUpdateList is false)
                throw new GracefulException("Only list is supported for now");

            string path = GetSearchPath(options);

            if (Microsoft.DotNet.Tools.Common.PathUtility.IsDirectory(path))
                path = GetSolutionOrProjectPath(path);

            var repositories = GetSourceRepositories(path);
            var projects = await GetProjectsAsync(path);
            var tasks = new List<Task<UpdateStatus>>();

            foreach (var project in projects)
                foreach (var reference in project.Packages)
                    tasks.Add(GetPackageUpdateStatus(reference, options, project.Name, repositories));

            var statuses = await Task.WhenAll(tasks);

            var output = AnsiConsole.GetOutput();
            foreach (var groupedStatus in statuses.GroupBy(s => s.ProjectName))
            {
                output.WriteLine(groupedStatus.Key);

                foreach (var status in groupedStatus.Where(s => s.UpdatedVersion != null).OrderBy(s => s.ProjectName))
                    output.WriteLine($"\t{AnsiColorExtensions.Bold(status.PackageId)}: {status.CurrentVersion} -> {AnsiColorExtensions.White(status.UpdatedVersion.ToFullString())}");
            }
        }

        private static string GetSearchPath(PackageOptions options)
        {
            if (options.SolutionOrProjectPath is null)
                return Directory.GetCurrentDirectory();

            return Path.Combine(Directory.GetCurrentDirectory(), options.SolutionOrProjectPath);
        }

        private static string GetSolutionOrProjectPath(string path)
        {
            var files = Directory.GetFiles(path, "*.sln").Concat(Directory.GetFiles(path, "*proj")).ToArray();

            if (files.Length == 0)
                throw new GracefulException($"Could not find a solution or project file in {path}");

            if (files.Length != 1)
                throw new GracefulException($"More than one solution and/or project file found in {path}");

            return files[0];
        }

        private static List<SourceRepository> GetSourceRepositories(string path)
        {
            var settings = Settings.LoadDefaultSettings(Path.GetDirectoryName(path));
            var packageSourceProvider = new PackageSourceProvider(settings);
            var packageSources = packageSourceProvider.LoadPackageSources();
            var resourceProviders = Repository.Provider.GetCoreV3();

            return packageSources.Select(ps => Repository.CreateSource(resourceProviders, ps)).ToList();
        }

        private static async Task<IEnumerable<Project>> GetProjectsAsync(string path)
        {
            if (path.EndsWith(".sln"))
                return (await Solution.FromPathAsync(path)).Projects;

            return new[] { await Project.FromPathAsync(path) };
        }

        private static async Task<UpdateStatus> GetPackageUpdateStatus(PackageReference reference, PackageOptions options, string projectName, IEnumerable<SourceRepository> repositories)
        {
            ILogger logger = options.Verbose ? ConsoleLogger.Instance : NullLogger.Instance;

            foreach (var repository in repositories)
            {
                var metadataResource = await repository.GetResourceAsync<MetadataResource>();
                var latestVersion = await metadataResource.GetLatestVersion(reference.PackageId, options.UsePreRelease, false, logger, CancellationToken.None);

                if (latestVersion is null)
                    continue;

                if (IsNewerWithConstraint(reference.Version, latestVersion, options.VersionConstraint))
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

        private static bool IsNewerWithConstraint(NuGetVersion currentVersion, NuGetVersion latestVersion, VersionConstraint versionConstraint)
        {
            switch (versionConstraint)
            {
                case VersionConstraint.Major:
                    return latestVersion > currentVersion;
                case VersionConstraint.Minor:
                    return latestVersion.Major == currentVersion.Major && latestVersion > currentVersion;
                case VersionConstraint.Patch:
                    return latestVersion.Major == currentVersion.Major && latestVersion.Minor == latestVersion.Minor && latestVersion > currentVersion;
                default:
                    throw new GracefulException($"Unknown version constraint encountered: {versionConstraint}");
            }
        }
    }
}
