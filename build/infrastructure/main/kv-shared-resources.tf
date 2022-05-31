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

data "azurerm_key_vault" "kv_shared_resources" {
  name                = var.shared_resources_keyvault_name
  resource_group_name = var.shared_resources_resource_group_name
}

data "azurerm_key_vault_secret" "mssql_data_name" {
  name         = "mssql-data-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_admin_name" {
  name         = "mssql-data-admin-user-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_admin_password" {
  name         = "mssql-data-admin-user-password"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "mssql_data_url" {
  name         = "mssql-data-url"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sbq_event_forwarded_queue" {
  name         = "sbq-market-roles-forward-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "appi_instrumentation_key" {
  name         = "appi-shared-instrumentation-key"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "appi_shared_id" {
  name         = "appi-shared-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sb_domain_relay_listener_connection_string" {
  name         = "sb-domain-relay-listen-connection-string"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sb_domain_relay_sender_connection_string" {
  name         = "sb-domain-relay-send-connection-string"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "st_market_operator_response_primary_connection_string" {
  name         = "st-marketres-primary-connection-string"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "st_market_operator_response_postofficereply_container_name" {
  name         = "st-marketres-postofficereply-container-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sb_domain_relay_transceiver_connection_string" {
  name         = "sb-domain-relay-transceiver-connection-string"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sbq_data_available_name" {
  name         = "sbq-data-available-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sbq_marketroles_name" {
  name         = "sbq-marketroles-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "st_market_operator_logs_primary_connection_string" {
  name         = "st-marketoplogs-primary-connection-string"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "st_market_operator_logs_container_name" {
  name         = "st-marketoplogs-container-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sbq_marketroles_reply_name" {
  name         = "sbq-marketroles-reply-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "sbq_marketroles_dequeue_name" {
  name         = "sbq-marketroles-dequeue-name"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "plan_shared_id" {
  name         = "plan-shared-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "b2c_tenant_id" {
  name         = "b2c-tenant-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "backend_service_app_id" {
  name         = "backend-service-app-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "log_shared_id" {
  name         = "log-shared-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "snet_private_endpoints_id" {
  name         = "snet-private-endpoints-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}

data "azurerm_key_vault_secret" "snet_vnet_integrations_id" {
  name         = "snet-vnet-integrations-id"
  key_vault_id = data.azurerm_key_vault.kv_shared_resources.id
}