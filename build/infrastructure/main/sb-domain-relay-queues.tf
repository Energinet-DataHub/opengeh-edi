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

# Queue to request change of accounting point characteristics transactions
module "sbq_req_change_acc_point_characteristics" {
  source              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/service-bus-queue?ref=7.0.0"

  name                = "req-change-of-accounting-point-characteristics"
  namespace_id        = data.azurerm_key_vault.sb_domain_relay_namespace_id.value
}

module "kvs_sbq_req_change_acc_point_characteristics" {
  source        = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/key-vault-secret?ref=7.0.0"

  name          = "sbq-req-change-of-acc-point-characteristics"
  value         = module.sbq_req_change_acc_point_characteristics.name
  key_vault_id  = data.azurerm_key_vault.kv_shared_resources.id

  tags          = azurerm_resource_group.this.tags
}