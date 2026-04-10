@description('Name of the SQL Database')
param name string

@description('Azure region')
param location string

@description('Resource ID of the parent SQL Server')
param sqlServerId string

// Extract server name from server resource ID
var serverName = last(split(sqlServerId, '/'))

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: '${serverName}/${name}'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB
  }
}

@description('Resource ID of the SQL Database')
output id string = sqlDatabase.id
