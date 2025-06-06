using Azure.Core;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using OneLakeKustoIngestionConsole.Storage;
using System.Buffers;
using System.Collections.Immutable;

namespace OneLakeKustoIngestionConsole
{
    internal class KustoGateway
    {
        private readonly ICslAdminProvider _commandProvider;
        private readonly string _databaseName;
        private readonly string _tableName;

        #region Constructor
        public KustoGateway(TokenCredential credential, string tableUrl, string appNameForTracing)
        {
            if (!Uri.TryCreate(tableUrl, UriKind.Absolute, out var tableUri))
            {
                throw new InvalidDataException($"Invalid table url:  {tableUrl}");
            }

            var uriBuilder = new UriBuilder(tableUri);
            var path = uriBuilder.Path;
            var pathParts = uriBuilder.Path.Trim().Trim('/').Split('/');

            //  Remove path to keep only cluster URI
            uriBuilder.Path = string.Empty;
            if (pathParts.Length != 2)
            {
                throw new InvalidDataException($"Invalid table uri:  {tableUri}");
            }

            var builder = new KustoConnectionStringBuilder(uriBuilder.Uri.ToString())
                .WithAadAzureTokenCredentialsAuthentication(credential);

            builder.ApplicationNameForTracing = appNameForTracing;
            _commandProvider = KustoClientFactory.CreateCslAdminProvider(builder);
            _databaseName = pathParts[0];
            _tableName = pathParts[1];
        }
        #endregion

        public async Task<long> FetchIngestionCapacityAsync(CancellationToken ct)
        {
            var commandText = @"
.show capacity ingestions
| project Total
";
            var reader = await _commandProvider.ExecuteControlCommandAsync(
                _databaseName,
                commandText);

            return reader.ToSingleValue<long>();
        }

        public async Task<string> IngestBlobsAsync(
            IEnumerable<string> urls,
            string format,
            string? mapping,
            CancellationToken ct)
        {
            var mappingPart = !string.IsNullOrWhiteSpace(mapping) 
                ? $", ingestionMappingReference='{mapping}'" 
                : string.Empty;
            var withClause = $"with (format='{format}'{mappingPart})";

            var urlList = string.Join(",\n  ", urls.Select(u => $"'{u};impersonate'"));
            var commandText = $@"
.ingest async into table {_tableName}(
  {urlList}
)
{withClause}
";
            var reader = await _commandProvider.ExecuteControlCommandAsync(
                _databaseName,
                commandText);
            var operationId = reader.ToSingleValue<Guid>();

            return operationId.ToString();
        }

        public async Task<IImmutableList<OperationStatus>> ShowOperationsAsync(
            IEnumerable<string> operationIds,
            CancellationToken ct)
        {
            var commandText = $@"
.show operations (
    {string.Join(", ", operationIds)}
)
";
            var reader = await _commandProvider.ExecuteControlCommandAsync(
                _databaseName,
                commandText);
            var statuses = reader.ToRows(r => new OperationStatus(
                ((Guid)r["OperationId"]).ToString(),
                Enum.Parse<OperationState>((string)r["State"]),
                (string)r["Status"],
                (bool)r["ShouldRetry"]));

            return statuses.ToImmutableArray();
        }
    }
}
