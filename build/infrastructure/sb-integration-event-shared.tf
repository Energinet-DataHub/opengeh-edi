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
data "azurerm_servicebus_namespace" "integrationevents" {
  name                = var.sharedresources_integrationevents_service_bus_namespace_name
  resource_group_name = data.azurerm_resource_group.shared_resources.name
}

module "sbt_energy_supplier_changed" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//service-bus-topic?ref=1.2.0"
  name                = "sbt-energy-supplier-changed"
  namespace_name      = data.azurerm_servicebus_namespace.integrationevents.name
  resource_group_name = data.azurerm_resource_group.shared_resources.name
}