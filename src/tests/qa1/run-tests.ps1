$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Test-Path "node_modules/newman")){
    npm install
}

npm test
exit $LASTEXITCODE