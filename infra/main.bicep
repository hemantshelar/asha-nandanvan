targetScope = 'resourceGroup'

@description('Environment suffix used on resource names.')
@allowed(['dev'])
param env string = 'dev'

@description('Azure region for new resources.')
param location string = 'australiaeast'

@description('Existing Azure SQL server name (not the FQDN).')
param sqlServerName string = 'invtation'

@description('Resource group that already hosts the SQL server.')
param sqlServerResourceGroup string = 'invtation-web_group'

@description('Entra user or group login shown as SQL administrator.')
param sqlEntraAdminLogin string = ''

@description('Object ID of the Entra user or group set as SQL administrator.')
param sqlEntraAdminObjectId string = ''

@secure()
param googleClientId string = ''

@secure()
param googleClientSecret string = ''

@secure()
param squareApplicationId string = ''

@secure()
param squareAccessToken string = ''

@secure()
param squareLocationId string = ''

param adminSeedEmail string = ''

var suffix = env
var tags = {
  application: 'ashanandanvan'
  environment: suffix
}

var appName = 'app-ashanandanvan-${suffix}'
var planName = 'plan-ashanandanvan-${suffix}'
var appInsightsName = 'appi-ashanandanvan-${suffix}'
var logAnalyticsName = 'law-ashanandanvan-${suffix}'
var databaseName = 'ashanandanvan-${suffix}'

module sql 'modules/sql.bicep' = {
  name: 'sql-${suffix}'
  scope: resourceGroup(sqlServerResourceGroup)
  params: {
    sqlServerName: sqlServerName
    location: location
    databaseName: databaseName
    entraAdminLogin: sqlEntraAdminLogin
    entraAdminObjectId: sqlEntraAdminObjectId
    tags: tags
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${suffix}'
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    appInsightsName: appInsightsName
    tags: tags
  }
}

module web 'modules/webapp.bicep' = {
  name: 'webapp-${suffix}'
  params: {
    location: location
    planName: planName
    appName: appName
    appInsightsConnectionString: monitoring.outputs.connectionString
    sqlConnectionString: sql.outputs.adoConnectionString
    googleClientId: googleClientId
    googleClientSecret: googleClientSecret
    squareApplicationId: squareApplicationId
    squareAccessToken: squareAccessToken
    squareLocationId: squareLocationId
    adminSeedEmail: adminSeedEmail
    tags: tags
  }
}

output resourceGroupName string = resourceGroup().name
output webAppName string = web.outputs.appName
output webAppUrl string = web.outputs.url
output webAppPrincipalId string = web.outputs.principalId
output sqlServerFqdn string = sql.outputs.fqdn
output databaseName string = sql.outputs.databaseName
