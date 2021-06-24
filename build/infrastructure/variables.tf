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
variable "resource_group_name" {
  type = string
}

variable "environment" {
  type          = string
  description   = "Enviroment that the infrastructure code is deployed into"
}

variable "project" {
  type          = string
  description   = "Project that is running the infrastructure code"
}

variable "organisation" {
  type          = string
  description   = "Organisation that is running the infrastructure code"
}

variable "sharedresources_keyvault_name" {
  type          = string
  description   = "Name of the Core keyvault, that contains shared secrets"
}

variable "sharedresources_resource_group_name" {
  type          = string
  description   = "Resource group name of the Core keyvaults location"
}

variable "sharedresources_integrationevents_service_bus_namespace_name" {
  type          = string
  description   = "Namespace of the shared integration event service bus"
}