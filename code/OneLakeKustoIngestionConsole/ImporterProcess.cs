using Azure.Core;
using OneLakeKustoIngestionConsole.Storage;
using System;
using System.Collections.Immutable;

namespace OneLakeKustoIngestionConsole
{
    internal class ImporterProcess
    {
        //private const long BATCH_SIZE = 1000000;
        private const long BATCH_SIZE = 200000000;
        private static readonly TimeSpan CAPACITY_CACHE_DURATION = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan WAIT_BETWEEN_CHECK = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan WAIT_BETWEEN_REPORT = TimeSpan.FromSeconds(20);

        private readonly RowStorage _rowStorage;
        private readonly KustoGateway _kustoGateway;
        private readonly string? _mapping;
        private readonly string _format;

        public ImporterProcess(
            TokenCredential credential,
            string tableUrl,
            RowStorage rowStorage,
            string? mapping,
            string format,
            string appNameForTracing)
        {
            _kustoGateway = new KustoGateway(credential, tableUrl, appNameForTracing);
            _rowStorage = rowStorage;
            _mapping = mapping;
            _format = format;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var discoveredBatches = AggregateDiscoveredItems();
            var ingestingBatches = ReconstructIngestingBatches().ToDictionary();
            var ingestionCapacityExpiry = DateTime.Now.Subtract(TimeSpan.FromHours(1));
            var ingestionCapacity = (long)0;
            var reportExpiry = ingestionCapacityExpiry;

            while (discoveredBatches.Any() || ingestingBatches.Any())
            {
                if (ingestionCapacityExpiry < DateTime.Now)
                {   //  Refresh total capacity
                    ingestionCapacity = await _kustoGateway.FetchIngestionCapacityAsync(ct);
                    ingestionCapacityExpiry = DateTime.Now.Add(CAPACITY_CACHE_DURATION);
                }
                await StartIngestDataAsync(
                    ingestionCapacity,
                    discoveredBatches,
                    ingestingBatches,
                    ct);
                if (reportExpiry < DateTime.Now)
                {
                    ReportProgress();
                    reportExpiry = DateTime.Now.Add(WAIT_BETWEEN_REPORT);
                }
                await Task.Delay(WAIT_BETWEEN_CHECK, ct);
                await EndIngestDataAsync(
                    ingestingBatches,
                    discoveredBatches,
                    ct);
            }
        }

        private void ReportProgress()
        {
            var discoveredCount = _rowStorage.Cache.GetAllItems()
                                    .Where(r => r.State == BlobState.Discovered)
                                    .Count();
            var ingestingCount = _rowStorage.Cache.GetAllItems()
                .Where(r => r.State == BlobState.Ingesting)
                .Count();
            var ingestedCount = _rowStorage.Cache.GetAllItems()
                .Where(r => r.State == BlobState.Ingested)
                .Count();

            Console.WriteLine($"  Discovered ({discoveredCount}), " +
                $"Ingesting ({ingestingCount}), Ingested ({ingestedCount})");
        }

        private async Task EndIngestDataAsync(
            Dictionary<string, IImmutableList<RowItem>> ingestingBatches,
            Stack<IImmutableList<RowItem>> discoveredBatches,
            CancellationToken ct)
        {
            var statuses = await _kustoGateway.ShowOperationsAsync(ingestingBatches.Keys, ct);

            foreach (var s in statuses)
            {
                if (!ingestingBatches.ContainsKey(s.OperationId))
                {
                    throw new InvalidOperationException($"Extra operation ID:  {s.OperationId}");
                }
                var batch = ingestingBatches[s.OperationId];

                if (s.State == OperationState.InProgress
                    || s.State == OperationState.Scheduled)
                {   //  Nothing to do
                }
                else if (s.State == OperationState.Completed
                    || s.State == OperationState.PartiallySucceeded)
                {
                    var newBatch = batch
                        .Select(r => r with
                        {
                            State = BlobState.Ingested,
                            OperationId = null
                        });

                    await _rowStorage.AppendItemsAsync(ct, newBatch);
                    ingestingBatches.Remove(s.OperationId);
                }
                else if (s.State == OperationState.Failed
                    || s.State == OperationState.Abandoned
                    || s.State == OperationState.Throttled
                    || s.State == OperationState.BadInput
                    || s.State == OperationState.Canceled
                    || s.State == OperationState.Skipped)
                {
                    if (s.ShouldRetry)
                    {
                        var newBatch = batch
                            .Select(r => r with
                            {
                                State = BlobState.Discovered,
                                OperationId = null
                            })
                            .ToImmutableArray();

                        await _rowStorage.AppendItemsAsync(ct, newBatch);
                        ingestingBatches.Remove(s.OperationId);
                        discoveredBatches.Push(newBatch);
                    }
                    else
                    {
                        Console.WriteLine($"Operation {s.OperationId} is {s.State} with" +
                            $"status '{s.Status}'");
                    }
                }
                else
                {
                    throw new NotSupportedException($"State:  {s.State}");
                }
            }
        }

        private async Task StartIngestDataAsync(
            long ingestionCapacity,
            Stack<IImmutableList<RowItem>> discoveredBatches,
            Dictionary<string, IImmutableList<RowItem>> ingestingBatches,
            CancellationToken ct)
        {
            while (ingestionCapacity > ingestingBatches.Count && discoveredBatches.Any())
            {   //  There is capacity available:  let's ingest
                var batch = discoveredBatches.Pop();
                var operationId = await _kustoGateway.IngestBlobsAsync(
                    batch.Select(r => r.BlobUrl),
                    _format,
                    _mapping,
                    ct);
                var ingestingBatch = batch
                    .Select(r => r with
                    {
                        State = BlobState.Ingesting,
                        OperationId = operationId
                    })
                    .ToImmutableArray();

                await _rowStorage.AppendItemsAsync(ct, ingestingBatch);
                ingestingBatches.Add(operationId, ingestingBatch);
            }
        }

        private Stack<IImmutableList<RowItem>> AggregateDiscoveredItems()
        {
            var items = _rowStorage.Cache.GetAllItems()
                .Where(r => r.State == BlobState.Discovered)
                .OrderBy(i => i.BlobUrl);
            var batches = new List<IImmutableList<RowItem>>();
            var currentBatch = new List<RowItem>();
            var currentSize = (long)0;

            foreach (var item in items)
            {
                if (currentSize > 0 && currentSize + item.BlobSize > BATCH_SIZE)
                {   //  Seal batch
                    batches.Add(currentBatch.ToImmutableArray());
                    currentBatch.Clear();
                    currentSize = 0;
                }
                else
                {
                    currentSize += item.BlobSize;
                    currentBatch.Add(item);
                }
            }
            if (currentBatch.Any())
            {
                batches.Add(currentBatch.ToImmutableArray());
            }

            return new Stack<IImmutableList<RowItem>>(batches);
        }

        private IImmutableDictionary<string, IImmutableList<RowItem>>
            ReconstructIngestingBatches()
        {
            var items = _rowStorage.Cache.GetAllItems()
                .Where(r => r.State == BlobState.Ingesting);
            var batches = items
                .GroupBy(r => r.OperationId!)
                .ToImmutableDictionary(
                g => g.Key,
                g => (IImmutableList<RowItem>)g.ToImmutableArray());

            return batches;
        }
    }
}
