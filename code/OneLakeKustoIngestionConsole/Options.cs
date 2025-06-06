using CommandLine;

namespace OneLakeKustoIngestionConsole
{
    public class Options
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
        public string DatabaseUrl { get; set; } = string.Empty;
    }
}