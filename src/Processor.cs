using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hjoellund.DotNet.Cli.Update.Models;
using Hjoellund.DotNet.Cli.Update.Options;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Hjoellund.DotNet.Cli.Update
{
    internal class Processor
    {
        public static async Task Process(PackageOptions options)
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
                tasks.Add(VersionChecker.CheckUpdateStatusAsync(reference, options, project.Name, repositories));

            var statuses = await Task.WhenAll(tasks);

            var output = AnsiConsole.GetOutput();
            foreach (var groupedStatus in statuses.GroupBy(s => s.ProjectName))
            {
                output.WriteLine(groupedStatus.Key);

                bool hasUpdates = false;
                foreach (var status in groupedStatus.Where(s => s.UpdatedVersion != null).OrderBy(s => s.ProjectName))
                {
                    hasUpdates = true;
                    output.WriteLine($"\t{AnsiColorExtensions.Bold(status.PackageId)}: {AnsiColorExtensions.White(status.CurrentVersion.ToFullString())} -> {AnsiColorExtensions.White(status.UpdatedVersion.ToFullString())}");
                }

                if(hasUpdates is false)
                    output.WriteLine("\tNo package updates");
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
    }
}