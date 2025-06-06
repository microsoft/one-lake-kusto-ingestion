using Azure.Core;
using Azure.Storage.Files.DataLake;
using OneLakeKustoIngestionConsole.Storage;

namespace OneLakeKustoIngestionConsole
{
    internal class DiscoveryProcess
    {
        private const int DISCOVERY_REPORT = 100;

        private readonly TokenCredential _credential;
        private readonly string? _suffix;
        private readonly Uri _lakeEndpoint;
        private readonly string _fileSystemName;
        private readonly string _directoryPath;

        #region Constructor
        public DiscoveryProcess(
            TokenCredential credential,
            string fullDirectoryPath,
            string? suffix)
        {
            _credential = credential;
            _suffix = suffix;
            // Parse the OneLake URL components
            if (!Uri.TryCreate(fullDirectoryPath, UriKind.Absolute, out var uri))
            {
                throw new InvalidDataException(
                    $"Invalid OneLake URL format:  {fullDirectoryPath}");
            }

            //  Extract components
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');

            _lakeEndpoint = new Uri($"{uri.Scheme}://{uri.Host}");

            if (pathSegments.Length < 2)
            {
                throw new InvalidDataException(
                    $"URL must contain at least the fileSystem and " +
                    $"directory path segments:  {fullDirectoryPath}");
            }

            _fileSystemName = pathSegments[0];
            _directoryPath = string.Join("/", pathSegments.Skip(1));
        }
        #endregion

        internal async Task RunAsync(RowStorage rowStorage, CancellationToken ct)
        {
            var rowItems = new List<RowItem>();

            Console.WriteLine("Discovering blobs...");
            Console.WriteLine($"  Endpoint: {_lakeEndpoint}");
            Console.WriteLine($"  File System: {_fileSystemName}");
            Console.WriteLine($"  Directory Path: {_directoryPath}");

            var serviceClient = new DataLakeServiceClient(_lakeEndpoint, _credential);
            var fileSystemClient = serviceClient.GetFileSystemClient(_fileSystemName);
            var directoryClient = fileSystemClient.GetDirectoryClient(_directoryPath);
            var blobs = directoryClient.GetPathsAsync(recursive: true);

            //  "Register" each blob
            await foreach (var blob in blobs)
            {
                if (_suffix == null
                    || blob.Name.EndsWith(_suffix, StringComparison.OrdinalIgnoreCase))
                {
                    rowItems.Add(new RowItem(
                        BlobState.Discovered,
                        fileSystemClient.GetFileClient(blob.Name).Uri.ToString(),
                        blob.ContentLength ?? 100000000));
                    if (rowItems.Count() % DISCOVERY_REPORT == 0)
                    {
                        Console.WriteLine($"  Discovered {rowItems.Count()} blobs...");
                    }
                }
            }
            Console.WriteLine($"Total of {rowItems.Count()} blobs discovered");
            await rowStorage.AppendItemsAsync(ct, rowItems);
        }
    }
}
