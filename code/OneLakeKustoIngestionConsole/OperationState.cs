using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole
{
    internal enum OperationState
    {
        InProgress,
        Completed,
        Failed,
        PartiallySucceeded,
        Abandoned,
        BadInput,
        Scheduled,
        Throttled,
        Canceled,
        Skipped
    }
}