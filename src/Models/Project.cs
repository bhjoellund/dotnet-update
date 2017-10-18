using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Versioning;

namespace Hjoellund.DotNet.Cli.Update.Models
{
    internal class Project
    {
        public string Name { get; set; }
        public IEnumerable<PackageReference> Packages { get; private set; }

        public static async Task<Project> FromPathAsync(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                return new Project
                {
                    Name = Path.GetFileName(path),
                    Packages = (from reference in document.Descendants("PackageReference")
                                select new PackageReference
                                {
                                    PackageId = reference.Attribute("Include").Value,
                                    Version = NuGetVersion.Parse(reference.Attribute("Version").Value)
                                }).ToArray()
                };
            }
        }
    }
}