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
output "kv_market_roles_db_connection_string_secret_name" {
  description = "Name of the secret in the keyvault containing the connection string for the market roles sql db"
  value = module.kvs_marketroles_db_connection_string.name
}

output "kv_market_roles_name" {
  description = "Name of the keyvault"
  value = module.kv_marketroles.name
}
