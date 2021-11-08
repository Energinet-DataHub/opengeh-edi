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
module "func_internalcommanddispatcher" {
  source                                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/function-app?ref=5.1.0"

  name                                      = "internalcommanddispatcher"
  project_name                              = var.project_name
  environment_short                         = var.environment_short
  environment_instance                      = var.environment_instance
  resource_group_name                       = data.azurerm_resource_group.this.name
  location                                  = data.azurerm_resource_group.this.location
  app_service_plan_id                       = module.plan_shared.id
  application_insights_instrumentation_key  = data.azurerm_key_vault_secret.appi_instrumentation_key.value
  always_on                                 = true
  app_settings                              = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE         = true
    WEBSITE_RUN_FROM_PACKAGE                = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE     = true
    FUNCTIONS_WORKER_RUNTIME                = "dotnet-isolated"
    # Endregion: Default Values
    "MARKETROLES_QUEUE_CONNECTION_STRING"   = module.sb_marketroles.primary_connection_strings["send"]
    "MARKETROLES_QUEUE_NAME"                = module.sbq_marketroles.name
    "MARKETROLES_CONNECTION_STRING"         = local.MARKETROLES_CONNECTION_STRING
    "DISPATCH_TRIGGER_TIMER"                = "*/10 * * * * *"    
  }
  
  tags                                      = data.azurerm_resource_group.this.tags
}