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
module "st_postoffice" {
    source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/storage-account?ref=5.1.0"

    name                      = "tmp"
    project_name              = var.project_name
    environment_short         = var.environment_short
    environment_instance      = var.environment_instance
    resource_group_name       = azurerm_resource_group.this.name
    location                  = azurerm_resource_group.this.location
    account_replication_type  = "LRS"
    account_tier              = "Standard"
    
    tags    = azurerm_resource_group.this.tags
}

resource "azurerm_storage_share" "postoffice" {
  name                        = "temppostoffice"
  storage_account_name        = module.st_postoffice.name
  quota                       = 50
}