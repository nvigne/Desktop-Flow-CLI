# Desktop Flow CLI

Desktop Flow CLI lists desktop flows in a Dataverse environment, sorts them by size, and writes the results to a CSV file.

## Requirements

- .NET 8 SDK
- Access to a Dataverse environment

## Build

```bash
dotnet restore Desktop-Flow-CLI.slnx
dotnet build Desktop-Flow-CLI.slnx
```

## Run

```bash
dotnet run --project DesktopFlowCLI/DesktopFlowCLI.csproj -- list \
  --service-uri "https://your-environment.crm.dynamics.com" \
  --min-size 1000000 \
  --path "desktopflow.csv" \
  --page-size 10
```

The command opens an interactive Dataverse login prompt and writes the CSV output to the path provided with `--path`.
