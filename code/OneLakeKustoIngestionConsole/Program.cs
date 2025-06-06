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
                    if (string.IsNullOrWhiteSpace(options.Format))
                    {
                        throw new ArgumentException("Format parameter is required and cannot be empty");
                    }

                    var process = await OrchestrationProcess.CreateAsync(
                        options.DirectoryPath,
                        options.Suffix,
                        options.TableUrl,
                        string.IsNullOrWhiteSpace(options.Mapping) ? null : options.Mapping,
                        options.Format,
                        ct);

                    await process.RunAsync(ct);
                });
        }
    }
}
