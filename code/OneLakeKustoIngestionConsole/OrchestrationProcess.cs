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
        private readonly TokenCredential _credential = new DefaultAzureCredential();
        private readonly string _fullDirectoryPath;
        private readonly string? _suffix;
        private readonly string _databaseUrl;
        private readonly RowStorage _rowStorage;

        #region Constructor
        public static async Task<OrchestrationProcess> CreateAsync(
            string fullDirectoryPath,
            string? suffix,
            string databaseUrl,
            CancellationToken ct)
        {
            var rowStorage = await RowStorage.LoadAsync(ct);

            return new OrchestrationProcess(
                fullDirectoryPath,
                suffix,
                databaseUrl,
                rowStorage);
        }

        private OrchestrationProcess(
            string fullDirectoryPath,
            string? suffix,
            string databaseUrl,
            RowStorage rowStorage)
        {
            _fullDirectoryPath = fullDirectoryPath;
            _suffix = suffix;
            _databaseUrl = databaseUrl;
            _rowStorage = rowStorage;
        }
        #endregion

        public async Task RunAsync(CancellationToken ct)
        {
            if (_rowStorage.Cache.Count == 0)
            {
                var discoveryProcess = new DiscoveryProcess(
                    _credential,
                    _fullDirectoryPath,
                    _suffix);

                Console.WriteLine("No blobs detected in checkpoint");
                await discoveryProcess.RunAsync(_rowStorage, ct);
            }

            var importerProcess = new ImporterProcess(_credential, _databaseUrl, _rowStorage);

            await importerProcess.RunAsync(ct);
        }
    }
}