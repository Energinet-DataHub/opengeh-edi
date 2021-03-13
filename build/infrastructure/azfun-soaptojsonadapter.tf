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
module "azfun_soaptojsonadapter" {
  source                                    = "git::https://github.com/Energinet-DataHub/green-energy-hub-core.git//terraform/modules/function-app?ref=1.3.0"
  name                                      = "azfun-soaptojsonadapter-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name                       = data.azurerm_resource_group.main.name
  location                                  = data.azurerm_resource_group.main.location
  storage_account_access_key                = module.azfun_soaptojsonadapter_stor.primary_access_key
  storage_account_name                      = module.azfun_soaptojsonadapter_stor.name
  app_service_plan_id                       = module.azfun_soaptojsonadapter_plan.id
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
    VALIDATION_REPORTS_QUEUE_TOPIC        = data.azurerm_key_vault_secret.VALIDATION_REPORTS_QUEUE_TOPIC.value
    VALIDATION_REPORTS_URL                = data.azurerm_key_vault_secret.VALIDATION_REPORTS_QUEUE_URL.value
    VALIDATION_REPORTS_CONNECTION_STRING  = data.azurerm_key_vault_secret.VALIDATION_REPORTS_CONNECTION_STRING.value
    SYNCHRONOUS_INGESTOR_BASE_URL         = "https://${module.azfun_synchronousingestor.default_hostname}/api"
    SOAP_TO_JSON_STORAGE_ACCOUNT          = module.stor_soaptojsonadapterstorage.primary_connection_string
  }
  dependencies                              = [
    module.appi.dependent_on,
    module.azfun_soaptojsonadapter_plan.dependent_on,
    module.azfun_soaptojsonadapter_stor.dependent_on,
    module.azfun_synchronousingestor.dependent_on,
    module.stor_soaptojsonadapterstorage.dependent_on,
  ]
}

module "azfun_soaptojsonadapter_plan" {
  source              = "git::https://github.com/Energinet-DataHub/green-energy-hub-core.git//terraform/modules/app-service-plan?ref=1.3.0"
  name                = "asp-soaptojsonadapter-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name = data.azurerm_resource_group.main.name
  location            = data.azurerm_resource_group.main.location
  kind                = "FunctionApp"
  sku                 = {
    tier  = "Free"
    size  = "F1"
  }
  tags                = data.azurerm_resource_group.main.tags
}

module "azfun_soaptojsonadapter_stor" {
  source                    = "git::https://github.com/Energinet-DataHub/green-energy-hub-core.git//terraform/modules/storage-account?ref=1.3.0"
  name                      = "stor${random_string.soaptojsonadapter.result}"
  resource_group_name       = data.azurerm_resource_group.main.name
  location                  = data.azurerm_resource_group.main.location
  account_replication_type  = "LRS"
  access_tier               = "Cool"
  account_tier              = "Standard"
  tags                      = data.azurerm_resource_group.main.tags
}

# Since all functions need a storage connected we just generate a random name
resource "random_string" "soaptojsonadapter" {
  length  = 10
  special = false
  upper   = false
}