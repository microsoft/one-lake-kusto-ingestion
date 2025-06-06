using Azure.Core;
using Azure.Identity;
using OneLakeKustoIngestionConsole.Storage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole
{
    internal class OrchestrationProcess
    {
        private readonly TokenCredential _credential = new AzureCliCredential();
            //new DefaultAzureCredential();
        private readonly RowStorage _rowStorage;
        private readonly DiscoveryProcess _discoveryProcess;
        private readonly ImporterProcess _importerProcess;

        #region Constructor
        public static async Task<OrchestrationProcess> CreateAsync(
            string fullDirectoryPath,
            string? suffix,
            string databaseUrl,
            string? mapping,
            string format,
            CancellationToken ct)
        {
            var rowStorage = await RowStorage.LoadAsync(ct);

            return new OrchestrationProcess(
                fullDirectoryPath,
                suffix,
                databaseUrl,
                mapping,
                format,
                rowStorage);
        }

        private OrchestrationProcess(
            string fullDirectoryPath,
            string? suffix,
            string databaseUrl,
            string? mapping,
            string format,
            RowStorage rowStorage)
        {
            _rowStorage = rowStorage;
            _discoveryProcess = new DiscoveryProcess(
                _credential,
                fullDirectoryPath,
                suffix);
            _importerProcess = new ImporterProcess(
                _credential,
                databaseUrl,
                rowStorage,
                mapping,
                format);
        }
        #endregion

        public async Task RunAsync(CancellationToken ct)
        {
            if (_rowStorage.Cache.Count == 0)
            {
                Console.WriteLine("No blobs detected in checkpoint");
                await _discoveryProcess.RunAsync(_rowStorage, ct);
            }

            Console.WriteLine("Start ingestion...");
            await _importerProcess.RunAsync(ct);
            Console.WriteLine("Completed ingestion...");
        }
    }
}
