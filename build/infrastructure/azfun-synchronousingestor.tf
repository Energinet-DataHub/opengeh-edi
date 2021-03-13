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
module "azfun_synchronousingestor" {
  source                                    = "git::https://github.com/Energinet-DataHub/green-energy-hub-core.git//terraform/modules/function-app?ref=1.3.0"
  name                                      = "azfun-synchronousingestor-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name                       = data.azurerm_resource_group.main.name
  location                                  = data.azurerm_resource_group.main.location
  storage_account_access_key                = module.azfun_synchronousingestor_stor.primary_access_key
  storage_account_name                      = module.azfun_synchronousingestor_stor.name
  app_service_plan_id                       = module.azfun_synchronousingestor_plan.id
  application_insights_instrumentation_key  = module.appi.instrumentation_key
  tags                                      = data.azurerm_resource_group.main.tags
  app_settings                              = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE       = true
    WEBSITE_RUN_FROM_PACKAGE              = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE   = true
    FUNCTIONS_WORKER_RUNTIME              = "dotnet"
    # Endregion: Default Values
    KAFKA_SECURITY_PROTOCOL               = "SaslSsl"
    KAFKA_SASL_MECHANISM                  = "Plain"
    KAFKA_SSL_CA_LOCATION                 = "C:\\cacert\\cacert.pem"
    KAFKA_USERNAME                        = "$ConnectionString"
    KAFKA_MESSAGE_SEND_MAX_RETRIES        = 5
    KAFKA_MESSAGE_TIMEOUT_MS              = 1000
    REQUEST_QUEUE_TOPIC                   = module.evh_requestqueue.name
    REQUEST_QUEUE_URL                     = "${module.evhnm_requestqueue.name}.servicebus.windows.net:9093"
    REQUEST_QUEUE_CONNECTION_STRING       = module.evhar_requestqueue_sender.primary_connection_string
    VALIDATION_REPORTS_QUEUE_TOPIC        = data.azurerm_key_vault_secret.VALIDATION_REPORTS_QUEUE_TOPIC.value
    VALIDATION_REPORTS_URL                = data.azurerm_key_vault_secret.VALIDATION_REPORTS_QUEUE_URL.value
    VALIDATION_REPORTS_CONNECTION_STRING  = data.azurerm_key_vault_secret.VALIDATION_REPORTS_CONNECTION_STRING.value
  }
  dependencies                              = [
    module.appi.dependent_on,
    module.azfun_synchronousingestor_plan.dependent_on,
    module.azfun_synchronousingestor_stor.dependent_on,
    module.evh_requestqueue.dependent_on,
    module.evhnm_requestqueue.dependent_on,
    module.evhar_requestqueue_sender.dependent_on,
  ]
}

module "azfun_synchronousingestor_plan" {
  source              = "git::https://github.com/Energinet-DataHub/green-energy-hub-core.git//terraform/modules/app-service-plan?ref=1.3.0"
  name                = "asp-synchronousingestor-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name = data.azurerm_resource_group.main.name
  location            = data.azurerm_resource_group.main.location
  kind                = "FunctionApp"
  sku                 = {
    tier  = "Free"
    size  = "F1"
  }
  tags                = data.azurerm_resource_group.main.tags
}

module "azfun_synchronousingestor_stor" {
  source                    = "git::https://github.com/Energinet-DataHub/green-energy-hub-core.git//terraform/modules/storage-account?ref=1.3.0"
  name                      = "stor${random_string.synchronousingestor.result}"
  resource_group_name       = data.azurerm_resource_group.main.name
  location                  = data.azurerm_resource_group.main.location
  account_replication_type  = "LRS"
  access_tier               = "Cool"
  account_tier              = "Standard"
  tags                      = data.azurerm_resource_group.main.tags
}

# Since all functions need a storage connected we just generate a random name
resource "random_string" "synchronousingestor" {
  length  = 10
  special = false
  upper   = false
}