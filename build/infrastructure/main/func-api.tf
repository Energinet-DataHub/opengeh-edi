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

  name                                     = "api"
  project_name                             = var.domain_name_short
  environment_short                        = var.environment_short
  environment_instance                     = var.environment_instance
  resource_group_name                      = azurerm_resource_group.this.name
  location                                 = azurerm_resource_group.this.location
  app_service_plan_id                      = data.azurerm_key_vault_secret.plan_shared_id.value
  application_insights_instrumentation_key = data.azurerm_key_vault_secret.appi_instrumentation_key.value
  log_analytics_workspace_id               = data.azurerm_key_vault_secret.log_shared_id.value
  vnet_integration_subnet_id                = data.azurerm_key_vault_secret.snet_vnet_integrations_id.value
  private_endpoint_subnet_id                = data.azurerm_key_vault_secret.snet_private_endpoints_id.value
  always_on                                = true
  health_check_path                         = "/api/monitor/ready"
  app_settings                             = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE                   = true
    WEBSITE_RUN_FROM_PACKAGE                          = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE               = true
    FUNCTIONS_WORKER_RUNTIME                          = "dotnet-isolated"
    # Endregion: Default Values
    # Shared resources logging
    REQUEST_RESPONSE_LOGGING_CONNECTION_STRING        = data.azurerm_key_vault_secret.st_market_operator_logs_primary_connection_string.value
    REQUEST_RESPONSE_LOGGING_CONTAINER_NAME           = data.azurerm_key_vault_secret.st_market_operator_logs_container_name.value
    B2C_TENANT_ID                                     = data.azurerm_key_vault_secret.b2c_tenant_id.value
    BACKEND_SERVICE_APP_ID                            = data.azurerm_key_vault_secret.backend_service_app_id.value
    # Endregion: Default Values
    MARKET_DATA_QUEUE_URL                             = "${module.sb_marketroles.name}.servicebus.windows.net:9093",
    INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING   = module.sb_marketroles.primary_connection_strings["manage"]
    INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING   = module.sb_marketroles.primary_connection_strings["send"]
    INCOMING_MESSAGE_QUEUE_LISTENER_CONNECTION_STRING = module.sb_marketroles.primary_connection_strings["listen"]
    DB_CONNECTION_STRING                              = local.MS_MARKETROLES_CONNECTION_STRING
    INCOMING_MESSAGE_QUEUE_NAME                       = "incomingmessagequeue"
    RAISE_TIME_HAS_PASSED_EVENT_SCHEDULE              = "*/10 * * * * *"
    MESSAGEHUB_QUEUE_CONNECTION_STRING                = data.azurerm_key_vault_secret.sb_domain_relay_transceiver_connection_string.value
    MESSAGEHUB_DATA_AVAILABLE_QUEUE                   = data.azurerm_key_vault_secret.sbq_data_available_name.value
    MESSAGEHUB_DOMAIN_REPLY_QUEUE                     = data.azurerm_key_vault_secret.sbq_marketroles_reply_name.value
    MESSAGEHUB_STORAGE_CONTAINER_NAME                 = data.azurerm_key_vault_secret.st_market_operator_response_postofficereply_container_name.value
    MESSAGEHUB_STORAGE_CONNECTION_STRING              = data.azurerm_key_vault_secret.st_market_operator_response_primary_connection_string.value
    MESSAGE_REQUEST_QUEUE                             = data.azurerm_key_vault_secret.sbq_marketroles_name.value
    MOVE_IN_REQUEST_ENDPOINT                          = "https://func-processing-${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}.azurewebsites.net/api/MoveIn"
  }

  tags = azurerm_resource_group.this.tags
}
