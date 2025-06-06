using CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace OneLakeKustoIngestionConsole
{
    internal class Program
    {
        //  This attributes is there to prevent an error when compiling with trimming
        //  since Options is accessed by reflection
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandLineOptions))]
        static async Task Main(string[] args)
        {
            var result = await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(async options =>
                {
                    var ct = CancellationToken.None;
                    if (string.IsNullOrWhiteSpace(options.Format))
                    {
                        throw new ArgumentException(
                            "Format parameter is required and cannot be empty");
                    }

                    var process = await OrchestrationProcess.CreateAsync(
                        options.DirectoryPath,
                        options.Suffix,
                        options.TableUrl,
                        string.IsNullOrWhiteSpace(options.Mapping) ? null : options.Mapping,
                        options.Format,
                        Constants.APP_NAME_FOR_TRACING,
                        ct);

                    await process.RunAsync(ct);
                });
        }
    }
}
