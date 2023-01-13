
param ($username, $subscription)
. "./azure-resource-create-functions.ps1"

Write-Host "Username = $username, Subscription = $subscription"

$resourceGroupName = "rg-batman"
$serviceBusNamespaceName = "servicebus-localtest-batman"

az account set --name $subscription

CreateQueue -queueName $username"-sbq-incoming_change_of_supplier_message_queue" -serviceBusNamespaceName $serviceBusNamespaceName

CreateTopic -topicName $username"-sbt-integration_event_topic" -serviceBusNamespaceName $serviceBusNamespaceName

CreateTopicSubscription -topicName $username"-sbt-integration_event_topic" -topicSubscriptionName $username"-sbts-integration_event_subscription" -serviceBusNamespaceName $serviceBusNamespaceName

CreateTopicSubscription -topicName $username"-sbt-integration_event_topic" -topicSubscriptionName $username"-sbts-market_participant_changed_actor_created" -serviceBusNamespaceName $serviceBusNamespaceName

CreateTopicSubscription -topicName $username"-sbt-integration_event_topic" -topicSubscriptionName $username"-sbts-balance_result_available_event" -serviceBusNamespaceName $serviceBusNamespaceName

