using CommandLine;

namespace OneLakeKustoIngestionConsole
{
    public class CommandLineOptions
    {
        [Option(
            'd',
            "directory",
            Required = true, 
            HelpText = "Full OneLake directory path (e.g., https://mytenant-onelake.dfs.fabric.microsoft.com/filesystem/path)")]
        public string DirectoryPath { get; set; } = string.Empty;

        [Option(
            's',
            "suffix",
            Required = false,
            HelpText = "Suffix filter (e.g. .parquet)")]
        public string? Suffix { get; set; } = string.Empty;

        [Option(
            't',
            "table-uri",
            Required = true,
            HelpText = "Kusto table URI, e.g. https://mycluster.z4.kusto.fabric.microsoft.com/mydb/mytable")]
        public string TableUrl { get; set; } = string.Empty;

        [Option(
            'm',
            "mapping",
            Required = false,
            HelpText = "Data Mapping reference")]
        public string Mapping { get; set; } = string.Empty;

        [Option(
            'f',
            "format",
            Required = true,
            HelpText = "Data format of the blobs")]
        public string Format { get; set; } = string.Empty;
    }
}