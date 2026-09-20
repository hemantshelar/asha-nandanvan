param location string
param planName string
param appName string
param appInsightsConnectionString string
param sqlConnectionString string
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
param tags object = {}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
    size: 'F1'
    family: 'F'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: appName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    reserved: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      alwaysOn: false
      netFrameworkVersion: 'v10.0'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
          value: 'true'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqlConnectionString
        }
        {
          name: 'GoogleAuth__ClientId'
          value: googleClientId
        }
        {
          name: 'GoogleAuth__ClientSecret'
          value: googleClientSecret
        }
        {
          name: 'Payment__Provider'
          value: 'Square'
        }
        {
          name: 'Payment__Square__ApplicationId'
          value: squareApplicationId
        }
        {
          name: 'Payment__Square__AccessToken'
          value: squareAccessToken
        }
        {
          name: 'Payment__Square__LocationId'
          value: squareLocationId
        }
        {
          name: 'Payment__Square__UseSandbox'
          value: 'true'
        }
        {
          name: 'Admin__SeedEmail'
          value: adminSeedEmail
        }
      ]
    }
  }
}

output appName string = webApp.name
output url string = 'https://${webApp.properties.defaultHostName}'
output principalId string = webApp.identity.principalId
