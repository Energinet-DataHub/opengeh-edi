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
module "sbn_marketroles" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-namespace?ref=1.7.0"
  name                = "sbn-${var.project}-${var.organisation}-${var.environment}"
  resource_group_name = data.azurerm_resource_group.main.name
  location            = data.azurerm_resource_group.main.location
  sku                 = "basic"
  tags                = data.azurerm_resource_group.main.tags
}

module "sbq_marketroles" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-queue?ref=1.7.0"
  name                = "sbq-marketroles"
  namespace_name      = module.sbn_marketroles.name
  resource_group_name = data.azurerm_resource_group.main.name
  dependencies        = [module.sbn_marketroles]
}

module "sbnar_marketroles_listener" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-namespace-auth-rule?ref=1.7.0"
  name                      = "sbnar-marketroles-listener"
  namespace_name            = module.sbn_marketroles.name
  resource_group_name       = data.azurerm_resource_group.main.name
  listen                    = true
  dependencies              = [module.sbq_marketroles]
}

module "sbnar_marketroles_sender" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-namespace-auth-rule?ref=1.7.0"
  name                      = "sbnar-marketroles-sender"
  namespace_name            = module.sbn_marketroles.name
  resource_group_name       = data.azurerm_resource_group.main.name
  send                      = true
  dependencies              = [module.sbq_marketroles]
}