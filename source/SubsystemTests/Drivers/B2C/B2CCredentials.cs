﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;

public class B2CCredentials(string username, string password, Guid actorId)
{
    public string Username { get; } = !string.IsNullOrWhiteSpace(username)
        ? username
        : throw new ArgumentNullException(nameof(username));

    public string Password { get; } = !string.IsNullOrWhiteSpace(password)
        ? password
        : throw new ArgumentNullException(nameof(password));

    public Guid ActorId { get; } = actorId;
}
