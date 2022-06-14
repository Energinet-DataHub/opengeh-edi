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
module "func_receiver" {
  source                                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/function-app?ref=6.0.0"

  name                                      = "api"
  project_name                              = var.domain_name_short
  environment_short                         = var.environment_short
  environment_instance                      = var.environment_instance
  resource_group_name                       = azurerm_resource_group.this.name
  location                                  = azurerm_resource_group.this.location
  app_service_plan_id                       = data.azurerm_key_vault_secret.plan_shared_id.value
  application_insights_instrumentation_key  = data.azurerm_key_vault_secret.appi_instrumentation_key.value
  log_analytics_workspace_id                = data.azurerm_key_vault_secret.log_shared_id.value
  vnet_integration_subnet_id                = data.azurerm_key_vault_secret.snet_vnet_integrations_id.value
  private_endpoint_subnet_id                = data.azurerm_key_vault_secret.snet_private_endpoints_id.value
  always_on                                 = true
  health_check_path                         = "/api/monitor/ready"
  app_settings                              = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE                               = true
    WEBSITE_RUN_FROM_PACKAGE                                      = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE                           = true
    FUNCTIONS_WORKER_RUNTIME                                      = "dotnet-isolated"
    # Endregion: Default Values
    # Shared resources logging
    REQUEST_RESPONSE_LOGGING_CONNECTION_STRING                    = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=st-marketoplogs-primary-connection-string)",
    REQUEST_RESPONSE_LOGGING_CONTAINER_NAME                       = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=st-marketoplogs-container-name)",
    B2C_TENANT_ID                                                 = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=b2c-tenant-id)",
    BACKEND_SERVICE_APP_ID                                        = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=backend-service-app-id)",
    # Endregion: Default Values
    MARKET_DATA_QUEUE_URL                                         = "${module.sb_marketroles.name}.servicebus.windows.net:9093",
    INCOMING_MESSAGE_QUEUE_MANAGE_CONNECTION_STRING               = module.sb_marketroles.primary_connection_strings["manage"]
    INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING               = module.sb_marketroles.primary_connection_strings["send"]
    INCOMING_MESSAGE_QUEUE_LISTENER_CONNECTION_STRING             = module.sb_marketroles.primary_connection_strings["listen"]
    DB_CONNECTION_STRING                                          = local.MS_MARKETROLES_CONNECTION_STRING
    INCOMING_MESSAGE_QUEUE_NAME                                   = "incomingmessagequeue"
    RAISE_TIME_HAS_PASSED_EVENT_SCHEDULE                          = "*/10 * * * * *"
    MESSAGEHUB_QUEUE_CONNECTION_STRING                            = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=sb-domain-relay-transceiver-connection-string)",
    MESSAGEHUB_DATA_AVAILABLE_QUEUE                               = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=sbq-data-available-name)",
    MESSAGEHUB_DOMAIN_REPLY_QUEUE                                 = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=sbq-marketroles-reply-name)",
    MESSAGEHUB_STORAGE_CONTAINER_NAME                             = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=st-marketres-postofficereply-container-name)",
    MESSAGEHUB_STORAGE_CONNECTION_STRING                          = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=st-marketres-primary-connection-string)",
    MESSAGE_REQUEST_QUEUE                                         = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=sbq-marketroles-name)",
    MOVE_IN_REQUEST_ENDPOINT                                      = "https://func-processing-${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}.azurewebsites.net/api/MoveIn"
    SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS_LISTENER = "@Microsoft.KeyVault(VaultName=${var.shared_resources_keyvault_name};SecretName=sb-domain-relay-listen-connection-string)",
    METERING_POINT_MASTER_DATA_RESPONSE_QUEUE_NAME                = data.azurerm_key_vault_secret.sbq_metering_point_master_data_response_name.value
  }

  tags = azurerm_resource_group.this.tags
}
