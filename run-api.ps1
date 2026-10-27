$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
dotnet run --project "$PSScriptRoot\PainelSaida.Api.csproj"
