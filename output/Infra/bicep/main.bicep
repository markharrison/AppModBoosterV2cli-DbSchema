// ============================================================================
// main.bicep — Expense Management System Infrastructure
// Deploys all Azure resources for the Expenses application.
// ============================================================================

@description('Application name prefix used for all resources')
param appName string = 'expenses'

@description('Deployment environment')
@allowed(['staging', 'production'])
param environment string

@description('Azure region for all resources')
param location string = 'uksouth'

@description('SKU for the App Service Plan')
param appServicePlanSku string = 'B1'

@description('Azure AD admin object ID for SQL Server (required by MCAPS policy — AAD-only auth)')
param sqlAadAdminObjectId string

@description('Azure AD admin display name for SQL Server')
param sqlAadAdminName string

// ---------------------------------------------------------------------------
// Variables
// ---------------------------------------------------------------------------
var env = toLower(environment)
var planName = 'plan-${appName}-${env}'
var apiAppName = 'app-${appName}-api-${env}'
var webAppName = 'app-${appName}-web-${env}'
var sqlServerName = 'sql-${appName}-${env}'
var sqlDatabaseName = 'sqldb-${appName}-${env}'
var apiIdentityName = 'id-${appName}-api-${env}'
var webIdentityName = 'id-${appName}-web-${env}'

// ---------------------------------------------------------------------------
// Managed Identities
// ---------------------------------------------------------------------------
module apiIdentity 'managedIdentity.bicep' = {
  name: 'deploy-identity-api'
  params: {
    name: apiIdentityName
    location: location
  }
}

module webIdentity 'managedIdentity.bicep' = {
  name: 'deploy-identity-web'
  params: {
    name: webIdentityName
    location: location
  }
}

// ---------------------------------------------------------------------------
// App Service Plan
// ---------------------------------------------------------------------------
module appServicePlan 'appServicePlan.bicep' = {
  name: 'deploy-app-service-plan'
  params: {
    name: planName
    location: location
    skuName: appServicePlanSku
  }
}

// ---------------------------------------------------------------------------
// SQL Server and Database
// ---------------------------------------------------------------------------
module sqlServer 'sqlServer.bicep' = {
  name: 'deploy-sql-server'
  params: {
    name: sqlServerName
    location: location
    aadAdminObjectId: sqlAadAdminObjectId
    aadAdminName: sqlAadAdminName
  }
}

module sqlDatabase 'sqlDatabase.bicep' = {
  name: 'deploy-sql-database'
  params: {
    name: sqlDatabaseName
    location: location
    sqlServerId: sqlServer.outputs.id
  }
}

// ---------------------------------------------------------------------------
// Web App — API
// ---------------------------------------------------------------------------
var apiConnectionString = 'Server=tcp:${sqlServer.outputs.fqdn},1433;Database=${sqlDatabaseName};Authentication=Active Directory Managed Identity;User Id=${apiIdentity.outputs.clientId};Encrypt=True;TrustServerCertificate=False;'

module apiApp 'webApp.bicep' = {
  name: 'deploy-api-app'
  params: {
    name: apiAppName
    location: location
    appServicePlanId: appServicePlan.outputs.id
    managedIdentityId: apiIdentity.outputs.id
    managedIdentityClientId: apiIdentity.outputs.clientId
    environmentName: environment == 'staging' ? 'Staging' : 'Production'
    connectionStrings: {
      DefaultConnection: apiConnectionString
    }
  }
}

// ---------------------------------------------------------------------------
// Web App — UI
// ---------------------------------------------------------------------------
module webApp 'webApp.bicep' = {
  name: 'deploy-web-app'
  params: {
    name: webAppName
    location: location
    appServicePlanId: appServicePlan.outputs.id
    managedIdentityId: webIdentity.outputs.id
    managedIdentityClientId: webIdentity.outputs.clientId
    environmentName: environment == 'staging' ? 'Staging' : 'Production'
    appSettings: {
      ApiBaseUrl: apiApp.outputs.url
      DisableHttpsRedirect: 'true'
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
@description('URL of the API App')
output apiUrl string = apiApp.outputs.url

@description('URL of the Web App')
output webUrl string = webApp.outputs.url

@description('Fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.outputs.fqdn

@description('Principal ID of the API managed identity')
output apiIdentityPrincipalId string = apiIdentity.outputs.principalId

@description('Principal ID of the Web managed identity')
output webIdentityPrincipalId string = webIdentity.outputs.principalId

@description('Client ID of the API managed identity')
output apiIdentityClientId string = apiIdentity.outputs.clientId

@description('Client ID of the Web managed identity')
output webIdentityClientId string = webIdentity.outputs.clientId
