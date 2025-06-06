using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneLakeKustoIngestionConsole.Storage
{
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = [typeof(JsonStringEnumConverter)])]
    [JsonSerializable(typeof(RowItem))]
    [JsonSerializable(typeof(BlobState))]
    internal partial class RowItemJsonContext : JsonSerializerContext
    {
    }
}