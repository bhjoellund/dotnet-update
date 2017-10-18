using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hjoellund.DotNet.Cli.Update.Models
{
    internal class Solution
    {
        public IEnumerable<Project> Projects { get; private set; }

        public static async Task<Solution> FromPathAsync(string path)
        {
            var projectPaths = await GetProjectPathsFromSolutionFileAsync(path);

            return new Solution
            {
                Projects = projectPaths.Select(p => Project.FromPathAsync(p).Result).ToArray()
            };
        }

        private static async Task<IEnumerable<string>> GetProjectPathsFromSolutionFileAsync(string path)
        {
            var projectPaths = new List<string>();
            var lines = await File.ReadAllLinesAsync(path);

            foreach (var line in lines.Where(l => l.StartsWith("Project(")))
            {
                switch (Regex.Match(line, "(?<=\")[\\w\\\\ ._-]+proj(?=\")"))
                {
                    case Match match when match.Success:
                        projectPaths.Add(Path.Combine(Path.GetDirectoryName(path), match.Value.Replace("\\", "/")));
                        break;
                }
            }

            return projectPaths;
        }
    }
}