#!/bin/bash

# Login to Azure
az login

# You can set your subscription if you have multiple
# az account set --subscription "Your-Subscription-Name-Or-ID"

# Deploy the Bicep template at subscription level (since it creates a resource group)
az deployment sub create \
  --name PoReflexGameDeployment \
  --location eastus \
  --template-file main.bicep \
  --parameters location=eastus \
               resourceGroupName=PoReflexGame-RG \
               storageName=poreflexgamestorage \
               deploymentEnvironment=prod

# Verify deployment status
echo "Deployment complete. Checking resource group..."
az group show --name PoReflexGame-RG --output table

# List storage accounts in the resource group
echo "Checking storage account..."
az storage account list --resource-group PoReflexGame-RG --output table

echo "Deployment complete!"
