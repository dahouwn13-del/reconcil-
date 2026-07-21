name: Build CIEL Windows EXE
on:
  workflow_dispatch:
  push:
    branches: [ main ]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore CIEL_Reconciliation_Native.sln
      - name: Publish portable single EXE
        run: dotnet publish CIEL.Reconciliation/CIEL.Reconciliation.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
      - uses: actions/upload-artifact@v4
        with:
          name: CIEL-Reconciliation-Windows
          path: publish/CIEL_Reconciliation.exe
