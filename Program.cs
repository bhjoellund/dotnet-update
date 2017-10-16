using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var (usePrerelease, path) = ParseArguments(args);

                var settings = Settings.LoadDefaultSettings(Path.GetDirectoryName(path));
                var packageSourceProvider = new PackageSourceProvider(settings);
                var packageSources = packageSourceProvider.LoadPackageSources();
                var resourceProviders = Repository.Provider.GetCoreV3();
                var repositories = packageSources.Select(ps => Repository.CreateSource(resourceProviders, ps)).ToList();

                foreach(var reference in await GetNuGetReferences(path))
                    await GetAvailableVersions(reference.PackageId, reference.Version, usePrerelease, repositories);
            }
            catch(GracefulException gex)
            {
                var errorConsole = Microsoft.DotNet.Cli.Utils.AnsiConsole.GetError();
                errorConsole.WriteLine(AnsiColorExtensions.Red(gex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static (bool, string) ParseArguments(string[] args)
        {
            bool prerelease = false;
            string path = Directory.GetCurrentDirectory();

            foreach(var arg in args)
            {
                switch(arg)
                {
                    case "--prerelease":
                        prerelease = true;
                        break;
                }
            }

            if(args.Length > 0)
            {
                var last = args[args.Length - 1];
                path = Path.Combine(path, last);
            }

            if(Microsoft.DotNet.Tools.Common.PathUtility.IsDirectory(path))
                path = GetProjectOrSolutionPath(path);

            return (prerelease, path);
        }

        private static string GetProjectOrSolutionPath(string path)
        {
            var files = Directory.GetFiles(path, "*.sln").Concat(Directory.GetFiles(path, "*proj")).ToArray();

            if(files.Length == 0)
                throw new GracefulException($"Could not find a solution or project file in {path}");
            
            if(files.Length != 1)
                throw new GracefulException($"More than one solution and/or project file found in {path}");
            
            return files[0];
        }

        private static async Task<IEnumerable<NuGetReference>> GetNuGetReferences(string projectPath)
        {
            using(var stream = File.OpenRead(projectPath))
            {
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                return from reference in document.Descendants("PackageReference")
                       select new NuGetReference
                       {
                           PackageId = reference.Attribute("Include").Value,
                           Version = NuGetVersion.Parse(reference.Attribute("Version").Value)
                       };
            }
        }

        private static async Task GetAvailableVersions(string packageId, NuGetVersion version, bool includePrerelease, IEnumerable<SourceRepository> repositories)
        {
            ILogger logger = NuGet.Common.NullLogger.Instance;

            foreach(var repository in repositories)
            {
                var metadataResource = await repository.GetResourceAsync<MetadataResource>();
                var latestVersion = await metadataResource.GetLatestVersion(packageId, includePrerelease, false, NullLogger.Instance, CancellationToken.None);

                if(latestVersion is null)
                    continue;

                if(latestVersion > version)
                    Console.WriteLine($"{packageId}: New version available ({latestVersion})");
                else
                    Console.WriteLine($"{packageId}: No new version found");

                break;
            }
        }
    }
}
