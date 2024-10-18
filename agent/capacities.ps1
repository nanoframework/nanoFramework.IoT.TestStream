param (
    [bool]$IgnoreAllEnv = $false,
    [bool]$SkipCapabilities = $false
)

if ($IgnoreAllEnv) {
    # Concatenate all environment variable names into a single string separated by commas
    $envVars = Get-ChildItem Env:
    $concatenatedNames = ($envVars | ForEach-Object { $_.Name }) -join ','

    # Set the concatenated string to the VSO_AGENT_IGNORE environment variable
    $env:VSO_AGENT_IGNORE = $concatenatedNames
}

if($SkipCapabilities)
{
    exit
}

# Check if AZP_TOKEN_FILE is not set
if (-not $env:AZP_TOKEN_FILE) {
    # Check if AZP_TOKEN is not set
    if (-not $env:AZP_TOKEN) {
        Write-Error "error: missing AZP_TOKEN environment variable"
        exit 1
    }

    # Set AZP_TOKEN_FILE and write AZP_TOKEN to it
    $env:AZP_TOKEN_FILE = ".token"
    Set-Content -Path $env:AZP_TOKEN_FILE -Value $env:AZP_TOKEN -NoNewline
}

# Unset AZP_TOKEN
Remove-Item -Path Env:AZP_TOKEN

# Read the .agent file and extract the serverUrl and poolName properties
$agentConfig = Get-Content -Raw -Path ".agent" | ConvertFrom-Json
$serverUrl = $agentConfig.serverUrl
$poolName = $agentConfig.poolName
$agentName = $agentConfig.agentName

# setting up the capabilities
$AZP_POOL_AGENTS = Invoke-RestMethod -Uri "$serverUrl/_apis/distributedtask/pools?poolName=$poolName&api-version=7.2-preview.1" -Headers @{Authorization=("Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("user:" + (Get-Content -Path $env:AZP_TOKEN_FILE))))} -Method Get
$AZP_POOL_ID = $AZP_POOL_AGENTS.value[0].id

# URL encode the AZP_AGENT_NAME environment variable
$encoded_name = [System.Web.HttpUtility]::UrlEncode($agentName)

# Print the encoded name
$AZP_POOL_AGENTS = Invoke-RestMethod -Uri "$serverUrl/_apis/distributedtask/pools/$AZP_POOL_ID/agents?agentName=$encoded_name&api-version=7.2-preview.1" -Headers @{Authorization=("Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("user:" + (Get-Content -Path $env:AZP_TOKEN_FILE))))} -Method Get
$AZP_AGENT_ID = $AZP_POOL_AGENTS.value[0].id

# Sending the custom capabilities
$capabilities = Get-Content -Raw -Path "configuration.json" | ConvertFrom-Json | Select-Object -ExpandProperty capabilities | ConvertTo-Json -Compress
$AZP_POOL_AGENTS = Invoke-RestMethod -Uri "$serverUrl/_apis/distributedtask/pools/$AZP_POOL_ID/agents/$AZP_AGENT_ID/usercapabilities?api-version=7.2-preview.1" -Headers @{Authorization=("Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("user:" + (Get-Content -Path $env:AZP_TOKEN_FILE))))} -Method Put -ContentType "application/json" -Body $capabilities
Write-Output "Capabilities set: $capabilities"

# Remove the token file
Remove-Item -Path $env:AZP_TOKEN_FILE
Remove-Item -Path Env:AZP_TOKEN_FILE