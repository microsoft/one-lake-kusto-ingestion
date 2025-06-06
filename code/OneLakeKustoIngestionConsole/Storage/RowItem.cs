using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole.Storage
{
    public record RowItem(
        BlobState State,
        string BlobUrl,
        long BlobSize,
        string? OperationId = null);
}