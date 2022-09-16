// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Messaging.Api
{
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Property name should match environment name")]
    public class RuntimeEnvironment
    {
        public static RuntimeEnvironment Default => new();

        public virtual string? MOVE_IN_REQUEST_ENDPOINT => GetEnvironmentVariable(nameof(MOVE_IN_REQUEST_ENDPOINT));

        public virtual string? DB_CONNECTION_STRING => GetEnvironmentVariable(nameof(DB_CONNECTION_STRING));

        public virtual string? INCOMING_MESSAGE_QUEUE_MANAGE_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(INCOMING_MESSAGE_QUEUE_MANAGE_CONNECTION_STRING));

        public virtual string? INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING));

        public virtual string? INCOMING_MESSAGE_QUEUE_NAME => GetEnvironmentVariable(nameof(INCOMING_MESSAGE_QUEUE_NAME));

        public virtual string? MESSAGE_REQUEST_QUEUE => GetEnvironmentVariable(nameof(MESSAGE_REQUEST_QUEUE));

        public virtual string? REQUEST_RESPONSE_LOGGING_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(REQUEST_RESPONSE_LOGGING_CONNECTION_STRING));

        public virtual string? REQUEST_RESPONSE_LOGGING_CONTAINER_NAME =>
            GetEnvironmentVariable(nameof(REQUEST_RESPONSE_LOGGING_CONTAINER_NAME));

        public virtual string? AZURE_FUNCTIONS_ENVIRONMENT =>
            GetEnvironmentVariable(nameof(AZURE_FUNCTIONS_ENVIRONMENT));

        public virtual string? MESSAGEHUB_STORAGE_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(MESSAGEHUB_STORAGE_CONNECTION_STRING));

        public virtual string? MESSAGEHUB_STORAGE_CONTAINER_NAME =>
            GetEnvironmentVariable(nameof(MESSAGEHUB_STORAGE_CONTAINER_NAME));

        public virtual string?MESSAGEHUB_QUEUE_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(MESSAGEHUB_QUEUE_CONNECTION_STRING));

        public virtual string? MESSAGEHUB_DATA_AVAILABLE_QUEUE =>
            GetEnvironmentVariable(nameof(MESSAGEHUB_DATA_AVAILABLE_QUEUE));

        public virtual string? MESSAGEHUB_DOMAIN_REPLY_QUEUE =>
            GetEnvironmentVariable(nameof(MESSAGEHUB_DOMAIN_REPLY_QUEUE));

        public virtual string? MASTER_DATA_REQUEST_QUEUE_NAME =>
            GetEnvironmentVariable(nameof(MASTER_DATA_REQUEST_QUEUE_NAME));

        public virtual string? SHARED_SERVICE_BUS_SEND_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(SHARED_SERVICE_BUS_SEND_CONNECTION_STRING));

        public virtual string? CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME =>
            GetEnvironmentVariable(nameof(CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME));

        public virtual string? CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME =>
            GetEnvironmentVariable(nameof(CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME));

        public string? ENERGY_SUPPLYING_SERVICE_BUS_SEND_CONNECTION_STRING =>
            GetEnvironmentVariable(nameof(ENERGY_SUPPLYING_SERVICE_BUS_SEND_CONNECTION_STRING));

        public virtual bool IsRunningLocally()
        {
            return AZURE_FUNCTIONS_ENVIRONMENT == "Development";
        }

        protected virtual string? GetEnvironmentVariable(string variable)
            => Environment.GetEnvironmentVariable(variable);
    }
}
