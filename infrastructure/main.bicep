param location string = 'eastus'
param storageName string = 'poreflexgamestorage'
param maxRetryAttempts int = 3
param deploymentEnvironment string = 'prod'
param resourceGroupName string = 'PoReflexGame-RG'

// Create the resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// Validate storage name
var validStorageName = replace(toLower(storageName), '-', '')
var sanitizedStorageName = length(validStorageName) > 24 ? substring(validStorageName, 0, 24) : validStorageName

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: sanitizedStorageName
  location: location
  scope: resourceGroup
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    cors: {
      corsRules: [
        {
          allowedHeaders: ['*']
          allowedMethods: ['GET', 'POST']
          allowedOrigins: ['*']
          exposedHeaders: ['*']
          maxAgeInSeconds: 3600
        }
      ]
    }
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2021-08-01' = {
  parent: storageAccount
  name: 'default'
}

resource highScoresTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2021-08-01' = {
  parent: tableService
  name: 'highscores'
}

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output resourceGroupName string = resourceGroup.name
output deploymentSuccess bool = true
