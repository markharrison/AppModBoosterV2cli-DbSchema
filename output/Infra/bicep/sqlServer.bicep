@description('Name of the SQL Server')
param name string

@description('Azure region')
param location string

@description('Azure AD admin object ID (required by policy)')
param aadAdminObjectId string

@description('Azure AD admin display name')
param aadAdminName string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: name
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: aadAdminName
      sid: aadAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// Enable Azure AD-only authentication
resource aadOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-08-01-preview' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
}

// Allow Azure services to access the SQL Server
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

@description('Fully qualified domain name of the SQL Server')
output fqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Resource ID of the SQL Server')
output id string = sqlServer.id
