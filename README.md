# one-lake-kusto-ingestion (olki)

Stop gap to perform an historical load from Microsoft OneLake to Eventhouse / Azure Data Explorer

##  What is this solution?

Olki is a Command-Line-Interface (CLI) tool enabling the ingestion of arbitrarily large amount of blobs from [Fabric OneLake](https://learn.microsoft.com/en-us/fabric/onelake/onelake-overview) into a Kusto cluster, either [Fabric Eventhouse](https://learn.microsoft.com/en-us/fabric/real-time-intelligence/eventhouse) or [Azure Data Explorer](https://learn.microsoft.com/en-us/azure/data-explorer/data-explorer-overview).

This is a temporary / stop gap solution until massive ingestion from OneLake is supported.  As such its **feature set is limited**.

##  Getting started

### Azure CLI authentication

You can [download the CLI here](https://github.com/microsoft/one-lake-kusto-ingestion/releases).  It is a single-file executable available for Linux, Windows & MacOS platform.

Olki's authentication uses Azure CLI authentication.  You need to [install Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) and authenticate against the OneLake's tenant using [az login](https://learn.microsoft.com/en-us/cli/azure/authenticate-azure-cli).

### OneLake root folder

You need to find the root folder containing the blobs you want to ingest.

If your files are located in a Fabric Lakehouse, you can choose one of the files you which to ingest:

![File Properties](documentation/media/Files-Properties.png)

and then copy its URL:

![File Properties](documentation/media/Blob-Url.png)

From the blob URL, removing the actual name of the blob, you should have the folder URL.

Similarly, if you want to ingest the blobs from one of the Delta Tables, you can select properties:

![File Properties](documentation/media/Table-Properties.png)

And copy its URL.

### Eventhouse URI & Database

Now that you have the source information, i.e. the OneLake folder, let's move to the destination, i.e. Eventhouse.

![Eventhouse details](documentation/media/Eventhouse-Details.png)



### Eventhouse Table

### Invoking the CLI