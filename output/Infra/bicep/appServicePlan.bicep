@description('Name of the App Service Plan')
param name string

@description('Azure region')
param location string

@description('SKU name for the App Service Plan')
param skuName string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  kind: 'linux'
  sku: {
    name: skuName
  }
  properties: {
    reserved: true // Required for Linux
  }
}

@description('Resource ID of the App Service Plan')
output id string = appServicePlan.id
