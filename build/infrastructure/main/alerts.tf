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
resource "azurerm_monitor_action_group" "marketroles" {
  name                = "ag-marketroles-${lower(var.environment_short)}-${lower(var.environment_instance)}"
  resource_group_name = azurerm_resource_group.this.name
  short_name          = "ag-mr-${lower(var.environment_short)}-${lower(var.environment_instance)}"

  email_receiver {
    name                    = "Alerts-MarketRoles-${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}"
    email_address           = "0a494d0d.energinet.onmicrosoft.com@emea.teams.ms"
    use_common_alert_schema = true
  }
}


resource "azurerm_monitor_scheduled_query_rules_alert" "marketroles_alert" {
  name                = "alert-marketroles-${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}"
  location            = azurerm_resource_group.this.location
  resource_group_name = var.shared_resources_resource_group_name

  action {
    action_group           = [azurerm_monitor_action_group.marketroles.id]
  }
  data_source_id = data.azurerm_key_vault_secret.appi_shared_id.value
  description    = "Alert when total results cross threshold"
  enabled        = true
  # Count all requests with server error result code grouped into 5-minute bins
  query       = <<-QUERY
    requests
| where timestamp > ago(10m) and  success == false
| join kind= inner (
exceptions
| where timestamp > ago(10m)
  and (cloud_RoleName == 'func-ingestion--${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}'
  or cloud_RoleName == 'func-outbox--${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}'
  or cloud_RoleName == 'func-localmessagehub--${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}'
  or cloud_RoleName == 'func-processing--${lower(var.domain_name_short)}-${lower(var.environment_short)}-${lower(var.environment_instance)}')
) on operation_Id
| project exceptionType = type, failedMethod = method, requestName = name, requestDuration = duration, function = cloud_RoleName
  QUERY
  severity    = 1
  frequency   = 5
  time_window = 30
  trigger {
    operator  = "GreaterThan"
    threshold = 1
  }
}