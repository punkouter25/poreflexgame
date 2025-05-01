[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "PoReflexGame-RG",
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    [Parameter(Mandatory=$false)]
    [string]$StorageName = "poreflexgamestorage",
    [Parameter(Mandatory=$false)]
    [string]$Environment = "prod",
    [Parameter(Mandatory=$false)]
    [int]$MaxRetryAttempts = 3
)

# Initialize logging
$logFile = "deployment_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$ErrorActionPreference = "Stop"

function Write-Log {
    param($Message)
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'): $Message"
    Write-Host $logMessage
    Add-Content -Path $logFile -Value $logMessage
}

Write-Log "Starting deployment for PoReflexGame..."

# Login to Azure
Write-Log "Checking Azure connection..."
$context = Get-AzContext
if (!$context) {
    Write-Log "Not connected to Azure. Initiating login..."
    Connect-AzAccount
    $context = Get-AzContext
    if (!$context) {
        throw "Failed to connect to Azure"
    }
}
Write-Log "Connected to Azure subscription: $($context.Subscription.Name)"

# Deploy the Bicep template at subscription level (since it creates a resource group)
Write-Log "Deploying Bicep template to create resource group and resources..."
$deploymentName = "PoReflexGameDeployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-AzDeployment -Name $deploymentName `
                -Location $Location `
                -TemplateFile "main.bicep" `
                -resourceGroupName $ResourceGroupName `
                -storageName $StorageName `
                -deploymentEnvironment $Environment `
                -maxRetryAttempts $MaxRetryAttempts

# Verify deployment status
Write-Log "Deployment complete. Checking resource group..."
Get-AzResourceGroup -Name $ResourceGroupName

# List storage accounts in the resource group
Write-Log "Checking storage account..."
Get-AzStorageAccount -ResourceGroupName $ResourceGroupName | Format-Table

Write-Log "Deployment complete! All resources have been provisioned."
