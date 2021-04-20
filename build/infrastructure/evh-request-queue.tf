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
module "evhnm_requestqueue" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//event-hub-namespace?ref=1.2.0"
  name                      = "evhnm-request-queue-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name       = data.azurerm_resource_group.main.name
  location                  = data.azurerm_resource_group.main.location
  sku                       = "Standard"
  capacity                  = 1
  tags                      = data.azurerm_resource_group.main.tags
}

module "evh_requestqueue" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//event-hub?ref=1.2.0"
  name                      = "evh-request-queue"
  namespace_name            = module.evhnm_requestqueue.name
  resource_group_name       = data.azurerm_resource_group.main.name
  partition_count           = 32
  message_retention         = 1
  dependencies              = [module.evhnm_requestqueue]
}

module "evhar_requestqueue_sender" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//event-hub-auth-rule?ref=1.2.0"
  name                      = "evhar-request-sender"
  namespace_name            = module.evhnm_requestqueue.name
  eventhub_name             = module.evh_requestqueue.name
  resource_group_name       = data.azurerm_resource_group.main.name
  send                      = true
  dependencies              = [module.evh_requestqueue]
}

module "evhar_requestqueue_listener" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//event-hub-auth-rule?ref=1.2.0"
  name                      = "evhar-request-listener"
  namespace_name            = module.evhnm_requestqueue.name
  eventhub_name             = module.evh_requestqueue.name
  resource_group_name       = data.azurerm_resource_group.main.name
  listen                    = true
  dependencies              = [module.evh_requestqueue]
}