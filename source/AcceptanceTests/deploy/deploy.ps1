param ($Subscription, $ResourceGroup, $SqlServerName, $NamespaceName, $PrNumber)

function CreateDatabase {
    $DatabaseName = "Test-$PrNumber"
    az sql db create --resource-group $ResourceGroup --server $SqlServerName --name $DatabaseName --edition Basic
}

function CreateServiceBusTopics {
    $IntegrationEventsTopic = "IntegrationEvents-$PrNumber"
    $(az servicebus topic create --resource-group $ResourceGroup  --namespace-name $NamespaceName --name $IntegrationEventsTopic) | Out-Null
    $(az servicebus topic subscription create --resource-group $ResourceGroup --namespace-name $NamespaceName --topic-name $IntegrationEventsTopic --name All-Events) | Out-Null
}

function CreateServiceBusQueues {
    $(az servicebus queue create --resource-group $ResourceGroup --namespace-name $NamespaceName --name "Command-$PrNumber") | Out-Null
}

az account set -s $Subscription

#CreateDatabase
CreateServiceBusTopics
CreateServiceBusQueues