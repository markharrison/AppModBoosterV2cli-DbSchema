@description('Name of the Web App')
param name string

@description('Azure region')
param location string

@description('Resource ID of the App Service Plan')
param appServicePlanId string

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Client ID of the user-assigned managed identity')
param managedIdentityClientId string

@description('App settings as key-value pairs')
param appSettings object = {}

@description('Connection strings configuration')
param connectionStrings object = {}

@description('Environment name (Staging or Production)')
param environmentName string

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: concat([
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
      ], map(items(appSettings), item => {
        name: item.key
        value: item.value
      }))
      connectionStrings: !empty(connectionStrings) ? map(items(connectionStrings), item => {
        name: item.key
        connectionString: item.value
        type: 'SQLAzure'
      }) : []
    }
  }
}

@description('Default hostname of the Web App')
output defaultHostname string = webApp.properties.defaultHostName

@description('URL of the Web App')
output url string = 'https://${webApp.properties.defaultHostName}'

@description('Resource ID of the Web App')
output id string = webApp.id
