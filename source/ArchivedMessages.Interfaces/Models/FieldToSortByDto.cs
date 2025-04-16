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

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;

public readonly record struct FieldToSortByDto
{
    public static readonly FieldToSortByDto MessageId = new("MessageId");
    public static readonly FieldToSortByDto DocumentType = new("DocumentType");
    public static readonly FieldToSortByDto SenderNumber = new("SenderNumber");
    public static readonly FieldToSortByDto ReceiverNumber = new("ReceiverNumber");
    public static readonly FieldToSortByDto CreatedAt = new("CreatedAt");

    private FieldToSortByDto(string identifier)
    {
        Identifier = identifier;
    }

    public string Identifier { get; }
}
