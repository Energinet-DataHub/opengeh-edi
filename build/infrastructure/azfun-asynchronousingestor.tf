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
module "azfun_asynchronousingestor" {
  source                                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//function-app?ref=1.0.0"
  name                                      = "azfun-asynchronousingestor-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name                       = data.azurerm_resource_group.main.name
  location                                  = data.azurerm_resource_group.main.location
  storage_account_access_key                = module.azfun_asynchronousingestor_stor.primary_access_key
  storage_account_name                      = module.azfun_asynchronousingestor_stor.name
  app_service_plan_id                       = module.azfun_asynchronousingestor_plan.id
  application_insights_instrumentation_key  = module.appi.instrumentation_key
  tags                                      = data.azurerm_resource_group.main.tags
  always_on                                 = true
  app_settings                              = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE       = true
    WEBSITE_RUN_FROM_PACKAGE              = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE   = true
    FUNCTIONS_WORKER_RUNTIME              = "dotnet"
    # Endregion: Default Values
    KAFKA_SECURITY_PROTOCOL               = "SaslSsl"
    KAFKA_SASL_MECHANISM                  = "Plain"
    KAFKA_MESSAGE_SEND_MAX_RETRIES        = 5
    KAFKA_MESSAGE_TIMEOUT_MS              = 1000
    KAFKA_USERNAME                        = "$ConnectionString"
    KAFKA_SSL_CA_LOCATION                 = "C:\\cacert\\cacert.pem"
    MARKET_DATA_URL                       = "${module.sbn_marketroles.name}.servicebus.windows.net:9093"
    MARKET_DATA_QUEUE_CONNECTION_STRING   = module.sbnar_marketroles_sender.primary_connection_string
    MARKET_DATA_QUEUE_NAME                = module.sbq_marketroles.name
    MARKET_DATA_DATABASE_CONNECTION_STRING= module.kvs_marketroles_db_connection_string.value
    CHARGE_QUEUE_CONNECTION_STRING        = "TO_BE_REMOVED"
    CHARGE_QUEUE_NAME                     = "TO_BE_REMOVED"
    TIMESERIES_QUEUE_TOPIC                = "TO_BE_REMOVED"
    TIMESERIES_QUEUE_URL                  = "TO_BE_REMOVED"
    TIMESERIES_QUEUE_CONNECTION_STRING    = "TO_BE_REMOVED"
    REQUEST_QUEUE_CONNECTION_STRING       = module.evhar_requestqueue_listener.primary_connection_string
    REQUEST_QUEUE_NAME                    = module.evh_requestqueue.name
    HUB_MRID                              = local.HUB_MRID
    LOCAL_TIMEZONENAME                    = local.LOCAL_TIMEZONENAME
    VALIDATION_REPORTS_QUEUE_TOPIC        = data.azurerm_key_vault_secret.VALIDATION_REPORTS_QUEUE_TOPIC.value
    VALIDATION_REPORTS_URL                = data.azurerm_key_vault_secret.VALIDATION_REPORTS_QUEUE_URL.value
    VALIDATION_REPORTS_CONNECTION_STRING  = data.azurerm_key_vault_secret.VALIDATION_REPORTS_CONNECTION_STRING.value
  }
  dependencies                              = [
    module.appi.dependent_on,
    module.azfun_asynchronousingestor_plan.dependent_on,
    module.azfun_asynchronousingestor_stor.dependent_on,
    module.sbnar_marketroles_sender.dependent_on,
    module.sbq_marketroles.dependent_on,
    module.evhar_requestqueue_listener.dependent_on,
    module.evh_requestqueue.dependent_on,
  ]
}

module "azfun_asynchronousingestor_plan" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//app-service-plan?ref=1.0.0"
  name                = "asp-asynchronousingestor-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name = data.azurerm_resource_group.main.name
  location            = data.azurerm_resource_group.main.location
  kind                = "FunctionApp"
  sku                 = {
    tier  = "Basic"
    size  = "B1"
  }
  tags                = data.azurerm_resource_group.main.tags
}

module "azfun_asynchronousingestor_stor" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//storage-account?ref=1.0.0"
  name                      = "stor${random_string.asynchronousingestor.result}"
  resource_group_name       = data.azurerm_resource_group.main.name
  location                  = data.azurerm_resource_group.main.location
  account_replication_type  = "LRS"
  access_tier               = "Cool"
  account_tier              = "Standard"
  tags                      = data.azurerm_resource_group.main.tags
}

# Since all functions need a storage connected we just generate a random name
resource "random_string" "asynchronousingestor" {
  length  = 10
  special = false
  upper   = false
}