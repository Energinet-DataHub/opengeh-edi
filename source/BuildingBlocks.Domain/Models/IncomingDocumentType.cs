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

using System;
using System.Linq;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public class IncomingDocumentType : EnumerationType
{
    public static readonly IncomingDocumentType RequestAggregatedMeasureData = new(nameof(RequestAggregatedMeasureData));
    public static readonly IncomingDocumentType B2CRequestAggregatedMeasureData = new(nameof(B2CRequestAggregatedMeasureData));
    public static readonly IncomingDocumentType RequestWholesaleSettlement = new(nameof(RequestWholesaleSettlement));

    public IncomingDocumentType(string name)
        : base(name)
    {
    }

    public static IncomingDocumentType? FromName(string? name)
    {
        if (name == null) return null;
        return GetAll<IncomingDocumentType>()
            .FirstOrDefault(
                br =>
                    br.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
