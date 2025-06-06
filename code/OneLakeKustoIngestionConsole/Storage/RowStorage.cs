using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole.Storage
{
    internal class RowStorage
    {
        private const string FILE_PATH = "checkpoint.json";

        #region Constructors
        public static async Task<RowStorage> LoadAsync(CancellationToken ct)
        {
            var cache = await ReadAllRowItemsAsync(ct);

            return new RowStorage(cache);
        }

        private RowStorage(RowCache cache)
        {
            Cache = cache;
        }

        private static async Task<RowCache> ReadAllRowItemsAsync(
            CancellationToken ct)
        {
            if (!File.Exists(FILE_PATH))
            {
                return new RowCache();
            }
            using (var streamReader = new StreamReader(FILE_PATH))
            {
                var cache = new RowCache();
                string? line;

                while ((line = await streamReader.ReadLineAsync(ct)) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var item = JsonSerializer.Deserialize(
                            line,
                            RowItemJsonContext.Default.RowItem);

                        if (item != null)
                        {
                            cache = cache.AddItems(item);
                        }
                    }
                }

                return cache;
            }
        }
        #endregion

        public RowCache Cache { get; private set; }

        public async Task AppendItemsAsync(
            CancellationToken ct,
            params IEnumerable<RowItem> items)
        {
            var materializedItems = items.ToImmutableArray();

            using (var stream = new MemoryStream())
            {
                foreach (var item in materializedItems)
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        item,
                        RowItemJsonContext.Default.RowItem);
                    stream.WriteByte((byte)'\n');
                }
                await File.AppendAllBytesAsync(FILE_PATH, stream.ToArray(), ct);
            }
            Cache = Cache.AddItems(materializedItems);
        }
    }
}
