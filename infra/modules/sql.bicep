@description('Existing Azure SQL logical server name.')
param sqlServerName string

@description('Azure region of the existing SQL server.')
param location string = 'australiaeast'

@description('New database name on the existing server.')
param databaseName string

param entraAdminLogin string = ''
param entraAdminObjectId string = ''
param tags object = {}

resource server 'Microsoft.Sql/servers@2023-08-01-preview' existing = {
  name: sqlServerName
}

resource entraAdmin 'Microsoft.Sql/servers/administrators@2023-08-01-preview' = if (!empty(entraAdminObjectId) && !empty(entraAdminLogin)) {
  parent: server
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: entraAdminLogin
    sid: entraAdminObjectId
    tenantId: subscription().tenantId
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: server
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: server
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    zoneRedundant: false
  }
}

output databaseName string = database.name
output fqdn string = server.properties.fullyQualifiedDomainName
output adoConnectionString string = 'Server=tcp:${server.properties.fullyQualifiedDomainName},1433;Initial Catalog=${database.name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;'
