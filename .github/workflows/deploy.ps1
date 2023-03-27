param ($subscription, $resourceGroup, $sqlServerName, $namespaceName, $prNumber)

az account set -s $subscription

#CreateDatabase
CreateServiceBusTopics
CreateServiceBusQueues

function CreateDatabase {
    $databaseName = "Test-$prNumber"
    az sql db create --resource-group $resourceGroup --server $sqlServerName --name $databaseName --edition Basic
}

function CreateServiceBusTopics {
    $integrationEventsTopic = "IntegrationEvents-$prNumber"
    $(az servicebus topic create --resource-group $resourceGroup  --namespace-name $namespaceName --name $integrationEventsTopic) | Out-Null
    $(az servicebus topic subscription create --resource-group $resourceGroup --namespace-name $namespaceName --topic-name $integrationEventsTopic --name All-Events) | Out-Null
}

function CreateServiceBusQueues {
    az servicebus queue create --resource-group $resourceGroup --namespace-name $namespaceName --name "Command-$prNumber"
}