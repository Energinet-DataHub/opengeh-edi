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
module "stor_soaptojsonadapterstorage" {
  source                    = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//storage-account?ref=1.2.0"
  name                      = "storsoaptojsonids${lower(var.organisation)}${lower(var.environment)}"
  resource_group_name       = data.azurerm_resource_group.main.name
  location                  = data.azurerm_resource_group.main.location
  account_replication_type  = "LRS"
  access_tier               = "Hot"
  account_tier              = "Standard"
  tags                      = data.azurerm_resource_group.main.tags
}

module "ast_messagereferenceid" {
  source                = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//table-storage?ref=1.2.0"
  name                  = "astmsgrefid"
  storage_account_name  = module.stor_soaptojsonadapterstorage.name
  dependencies          = [module.stor_soaptojsonadapterstorage]
}

module "ast_transactionid" {
  source                = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//table-storage?ref=1.2.0"
  name                  = "asttransactionid"
  storage_account_name  = module.stor_soaptojsonadapterstorage.name
  dependencies          = [module.stor_soaptojsonadapterstorage]
}

module "ast_headerenergydocumentid" {
  source                = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//table-storage?ref=1.2.0"
  name                  = "asthedid"
  storage_account_name  = module.stor_soaptojsonadapterstorage.name
  dependencies          = [module.stor_soaptojsonadapterstorage]
}
