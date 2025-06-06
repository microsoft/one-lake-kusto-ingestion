using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole
{
    internal record OperationStatus(
        string OperationId,
        OperationState State,
        string Status,
        bool ShouldRetry);
}