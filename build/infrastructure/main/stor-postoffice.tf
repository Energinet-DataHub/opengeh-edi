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
module "stor_postoffice" {
    source = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//storage-account?ref=1.7.0"
    name = "stor${var.project}tmp${var.environment}"
    resource_group_name = data.azurerm_resource_group.main.name
    location = data.azurerm_resource_group.main.location
    account_replication_type = "LRS"
    account_tier = "Standard"
    tags = data.azurerm_resource_group.main.tags
}

resource "azurerm_storage_share" "postoffice" {
  name                 = "temppostoffice"
  storage_account_name = module.stor_postoffice.name
  quota                = 50
}