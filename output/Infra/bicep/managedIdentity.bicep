@description('Name of the managed identity')
param name string

@description('Azure region')
param location string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
}

@description('Resource ID of the managed identity')
output id string = managedIdentity.id

@description('Principal ID of the managed identity')
output principalId string = managedIdentity.properties.principalId

@description('Client ID of the managed identity')
output clientId string = managedIdentity.properties.clientId
