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
using System.Collections.Generic;
using System.Linq;

namespace Messaging.Infrastructure.Configuration.InternalCommands
{
    public class InternalCommandMapper
    {
        private readonly HashSet<CommandMetadata> _values = new();

        public void Add(string eventName, Type eventType)
        {
            _values.Add(new CommandMetadata(eventName, eventType));
        }

        public CommandMetadata GetByName(string commandName)
        {
            return _values
                .FirstOrDefault(metadata => metadata.CommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException(
                $"No event metadata is registered for event {commandName}");
        }

        public CommandMetadata GetByType(Type commandType)
        {
            if (commandType == null) throw new ArgumentNullException(nameof(commandType));
            return _values
                       .FirstOrDefault(metadata => metadata.CommandType == commandType) ??
                   throw new InvalidOperationException(
                       $"No command metadata is registered for type {commandType.FullName}");
        }
    }

    public record CommandMetadata(string CommandName, Type CommandType);
}
