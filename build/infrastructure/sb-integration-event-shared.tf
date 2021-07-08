# Copyright 2020 Energinet DataHub A/S
#
# Licensed under the Apache License, Version 2.0 (the "License2");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Reference to get the namespace for the shared service bus for integration events
data "azurerm_servicebus_namespace" "integrationevents" {
  name                = var.sharedresources_integrationevents_service_bus_namespace_name
  resource_group_name = data.azurerm_resource_group.shared_resources.name
}

# Reference to get the listener connection string from the shared integration event service bus
data "azurerm_key_vault_secret" "INTEGRATION_EVENTS_LISTENER_CONNECTION_STRING" {
  name         = "INTEGRATION-EVENTS-LISTENER-CONNECTION-STRING"
  key_vault_id = data.azurerm_key_vault.kv_sharedresources.id
}

# Reference to get the sender connection string from the shared integration event service bus
data "azurerm_key_vault_secret" "INTEGRATION_EVENTS_SENDER_CONNECTION_STRING" {
  name         = "INTEGRATION-EVENTS-SENDER-CONNECTION-STRING"
  key_vault_id = data.azurerm_key_vault.kv_sharedresources.id
}

#Topics
module "sbt_energy_supplier_changed" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-topic?ref=1.2.0"
  name                = "sbt-energy-supplier-changed"
  namespace_name      = data.azurerm_servicebus_namespace.integrationevents.name
  resource_group_name = data.azurerm_servicebus_namespace.integrationevents.resource_group_name
}

#Subscriptions
module "sbt_energy_supplier_changed_subscription" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-subscription?ref=1.2.0"
  name                = "sbt_energy_supplier_changed_subscription"
  namespace_name      = data.azurerm_servicebus_namespace.integrationevents.name
  resource_group_name = data.azurerm_servicebus_namespace.integrationevents.resource_group_name  
  topic_name          = module.sbt_energy_supplier_changed.name
  max_delivery_count  = 10
  forward_to          = module.sbq_market_roles_forwarded_queue.name
  dependencies        = [module.sbq_market_roles_forwarded_queue]
}

#Queue to forward subscriptions to
module "sbq_market_roles_forwarded_queue" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-queue?ref=1.2.0"
  name                = "sbq-market_roles_forwarded_queue"
  namespace_name      = data.azurerm_servicebus_namespace.integrationevents.name
  resource_group_name = data.azurerm_servicebus_namespace.integrationevents.resource_group_name
}