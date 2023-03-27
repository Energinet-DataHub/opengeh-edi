param(
    [string] $subscription = $(throw "-subscription is required"),    
    [string] $resourceGroup = $(throw "-resourcegroup is required"),
    [string] $sqlServerName = $(throw "-sqlServerName is required"),
    [string] $namespaceName = $(throw "-namespaceName is required"),
    [string] $prNumber = $(throw "-prNumber is required")
)

az account set -s $subscription

#DeleteDatabase
DeleteServiceBusTopics
#DeleteServiceBusQueues

function DeleteDatabase {
    $databaseName = "Test-$prNumber"
    az sql db delete --name $databaseName  --resource-group $resourceGroup --server $sqlServerName | Out-Null
}

function DeleteServiceBusTopics {
    $integrationEventsTopic = "IntegrationEvents-$prNumber"
    az servicebus topic subscription delete --resource-group $resourceGroup --namespace-name $namespaceName --topic-name $integrationEventsTopic --name All-Events | Out-Null
    az servicebus topic delete --resource-group $resourceGroup --namespace-name $namespaceName --name $integrationEventsTopic | Out-Null
}

function DeleteServiceBusQueues {
    az servicebus queue delete --resource-group $resourceGroup --namespace-name $namespaceName --name "Command-$prNumber" | Out-Null
}