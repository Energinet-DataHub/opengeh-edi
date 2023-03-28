param(
    [Parameter(Mandatory=$true)]
    [string]$Subscription,
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    [Parameter(Mandatory=$true)]
    [string]$SqlServerName,
    [Parameter(Mandatory=$true)]
    [string]$NamespaceName,
    [Parameter(Mandatory=$true)]
    [string]$PrNumber)

function DeleteDatabase {
    $databaseName = "Test-$prNumber"
    az sql db delete --name $databaseName  --resource-group $ResourceGroup --server $SqlServerName | Out-Null
}

function DeleteServiceBusTopics {
    $integrationEventsTopic = "IntegrationEvents-$PrNumber"
    az servicebus topic subscription delete --resource-group $ResourceGroup --namespace-name $NamespaceName --topic-name $integrationEventsTopic --name All-Events | Out-Null
    az servicebus topic delete --resource-group $ResourceGroup --namespace-name $NamespaceName --name $integrationEventsTopic | Out-Null
}

function DeleteServiceBusQueues {
    az servicebus queue delete --resource-group $ResourceGroup --namespace-name $NamespaceName --name "Command-$PrNumber" | Out-Null
}

az account set -s $Subscription

#DeleteDatabase
DeleteServiceBusTopics
DeleteServiceBusQueues