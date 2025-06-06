using CommandLine;

namespace OneLakeKustoIngestionConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var result = await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options =>
                {
                    var ct = CancellationToken.None;
                    var process = await OrchestrationProcess.CreateAsync(
                        options.DirectoryPath,
                        options.Suffix,
                        options.TableUrl,
                        options.Mapping,
                        options.Format,
                        ct);

                    await process.RunAsync(ct);
                });
        }
    }
}