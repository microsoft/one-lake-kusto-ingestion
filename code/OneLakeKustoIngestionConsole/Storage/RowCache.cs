
using System.Collections.Immutable;

namespace OneLakeKustoIngestionConsole.Storage
{
    public class RowCache
    {
        private ImmutableDictionary<string, RowItem> _rowItemCache;

        #region Constructor
        public RowCache()
            : this(ImmutableDictionary<string, RowItem>.Empty)
        {
        }

        private RowCache(ImmutableDictionary<string, RowItem> rowItemCache)
        {
            _rowItemCache = rowItemCache;
        }
        #endregion

        public int Count => _rowItemCache.Count;

        public IImmutableDictionary<string, RowItem> GetItems() => _rowItemCache;

        public RowCache AddItems(params IEnumerable<RowItem> items)
        {
            var builder = _rowItemCache.ToBuilder();

            foreach (var item in items)
            {
                builder[item.BlobUrl] = item;
            }

            return new RowCache(builder.ToImmutable());
        }

        public IEnumerable<RowItem> GetAllItems()
        {
            return _rowItemCache.Values;
        }
    }
}
