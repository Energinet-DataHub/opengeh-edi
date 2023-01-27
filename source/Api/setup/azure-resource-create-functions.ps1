function CreateQueue(
  [string]$queueName,
  [bool]$enablesession = $false)
{
  Write-Host "Creating Queue '$queueName'" -ForegroundColor Cyan
  $messageTimeToLive = "PT15M"
  $maxSize = 1024
  az servicebus queue create --resource-group $resourceGroupName --namespace-name $serviceBusNamespaceName --name $queueName --max-size $maxSize --default-message-time-to-live $messageTimeToLive --enable-session $enablesession
  WriteErrorAndExitIfLastCommandReturnError "Error creating Queue"
}

function CreateTopic
{
  [CmdletBinding()]
  Param(
    [Parameter(Mandatory = $true)]
    [string]$topicName,
    [Parameter(Mandatory = $false)]
    [string]$serviceBusNamespaceName
  )

  Write-Host "Creating Topic '$topicName'" -ForegroundColor Cyan
  $maxSize = 1024
  $enableOrdering = "false"
  $enableBatchedOperations = "false"
  az servicebus topic create --resource-group $resourceGroupName --namespace-name $serviceBusNamespaceName --name $topicName --max-size $maxSize --enable-ordering $enableOrdering --enable-batched-operations $enableBatchedOperations
  WriteErrorAndExitIfLastCommandReturnError "Error creating Topic"
}

function CreateTopicSubscription
{
  [CmdletBinding()]
  Param(
    [Parameter(Mandatory = $true)]
    [string]$topicName,
    [Parameter(Mandatory = $true)]
    [string]$topicSubscriptionName,
    [Parameter(Mandatory = $false)]
    [string]$serviceBusNamespaceName
  )

  Write-Host "Creating Topic Subscription '$topicSubscriptionName'" -ForegroundColor Cyan
  $enableBatchedOperations = "false"
  az servicebus topic subscription create --resource-group $resourceGroupName --namespace-name $serviceBusNamespaceName --topic-name $topicName --name $topicSubscriptionName --enable-batched-operations $enableBatchedOperations
  WriteErrorAndExitIfLastCommandReturnError "Error creating Topic Subscription"
}

function WriteErrorAndExitIfLastCommandReturnError {
    param(
        [Parameter(Mandatory=$true)]
        [string]$context
    )

    if ($LASTEXITCODE -ne 0) {
        $failureDescription = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
        Write-Error "The last command returned an error in the '$context' context ($failureDescription). Exiting."
        exit
    }
}

