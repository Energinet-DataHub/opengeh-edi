param (
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

function CreateDatabase {
    $databaseName = "Test-$PrNumber"
    az sql db create --resource-group $ResourceGroup --server $SqlServerName --name $databaseName --edition Basic
}

function CreateServiceBusTopics {
    $integrationEventsTopic = "IntegrationEvents-$PrNumber"
    $(az servicebus topic create --resource-group $ResourceGroup  --namespace-name $NamespaceName --name $integrationEventsTopic) | Out-Null
    $(az servicebus topic subscription create --resource-group $ResourceGroup --namespace-name $NamespaceName --topic-name $integrationEventsTopic --name All-Events) | Out-Null
}

function CreateServiceBusQueues {
    $(az servicebus queue create --resource-group $ResourceGroup --namespace-name $NamespaceName --name "Command-$PrNumber") | Out-Null
}

az account set -s $Subscription

#CreateDatabase
CreateServiceBusTopics
CreateServiceBusQueues