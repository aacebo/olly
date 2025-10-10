@maxLength(20)
@minLength(4)
@description('Used to generate names for all resources in this file')
param resourceBaseName string

@maxLength(42)
param botDisplayName string

@secure()
param botAadAppClientSecret string

param botServiceName string = resourceBaseName
param botServiceSku string = 'F0'
param identityResourceId string
param identityClientId string
param identityTenantId string
param botAadAppClientId string
param botAppDomain string

// Register your web service as a bot with the Bot Framework
resource botService 'Microsoft.BotService/botServices@2021-03-01' = {
  kind: 'azurebot'
  location: 'global'
  name: botServiceName
  properties: {
    displayName: botDisplayName
    endpoint: 'https://${botAppDomain}/api/messages'
    msaAppId: identityClientId
    msaAppMSIResourceId: identityResourceId
    msaAppTenantId: identityTenantId
    msaAppType: 'UserAssignedMSI'
  }
  sku: {
    name: botServiceSku
  }
}

// Connect the bot service to Microsoft Teams
resource botServiceMsTeamsChannel 'Microsoft.BotService/botServices/channels@2021-03-01' = {
  parent: botService
  location: 'global'
  name: 'MsTeamsChannel'
  properties: {
    channelName: 'MsTeamsChannel'
  }
}

resource botServicesGithubConnection 'Microsoft.BotService/botServices/connections@2022-09-15' = {
  parent: botService
  name: 'Github'
  location: 'global'
  properties: {
    serviceProviderDisplayName: 'Github'
    serviceProviderId: 'd05eaacf-1593-4603-9c6c-d4d8fffa46cb'
    clientId: botAadAppClientId
    clientSecret: botAadAppClientSecret
    parameters: [
      {
        key: 'ClientId'
        value: 'Iv23liSuSCDKZJKYlQpO'
      }
      {
        key: 'ClientSecret'
        value: '69821d2a142a76e99ded917b9ab9a8d521776e46'
      }
    ]
  }
}