using System;
using System.Threading.Tasks;
using CommandLine;
using Hjoellund.DotNet.Cli.Update.Options;
using Microsoft.DotNet.Cli.Utils;

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
                await Processor.Process(options);
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
    }
}
