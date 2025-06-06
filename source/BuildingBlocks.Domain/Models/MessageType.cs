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

using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// Represent the Message Type
/// Mapping of the Message Type code to the Message Type name is comming from this section "Datadefinitioner for DocumentNameCodeType" document:
/// https://energinet.dk/media/4v0nfpec/edi-transaktioner-for-det-danske-elmarked.pdf
/// </summary>
public sealed class MessageType : DataHubType<MessageType>
{
    public static readonly MessageType ValidatedMeteredData = new(nameof(ValidatedMeteredData), "E66");
    public static readonly MessageType RequestAggregatedMeteredData = new(nameof(RequestAggregatedMeteredData), "E74");
    public static readonly MessageType RequestForAggregatedBillingInformation = new(nameof(RequestForAggregatedBillingInformation), "D21");
    public static readonly MessageType RequestMeasurements = new(nameof(RequestMeasurements), "E73");

    [JsonConstructor]
    private MessageType(string name, string code)
        : base(name, code) { }
}
