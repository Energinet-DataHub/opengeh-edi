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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

/// <summary>
/// Represents a serie of an incoming message
/// </summary>
public interface IIncomingMessageSerie
{
    /// <summary>
    /// Id of the incoming message serie
    /// </summary>
    public string TransactionId { get; }

    /// <summary>
    /// Start Date and Time of the incoming message serie
    /// </summary>
    public string StartDateTime { get; }

    /// <summary>
    /// End Date and Time of the incoming message serie
    /// </summary>
    public string? EndDateTime { get; }
}
