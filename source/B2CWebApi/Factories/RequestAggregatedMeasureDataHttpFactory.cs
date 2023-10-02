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

using Energinet.DataHub.Edi.Requests;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestAggregatedMeasureDataHttpFactory
{
    public static RequestAggregatedMeasureData Create()
    {
        return new RequestAggregatedMeasureData
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = "5790000701414",
            SenderRole = "DDQ",
            ReceiverId = "5790001330552",
            ReceiverRole = "DGL",
            AuthenticatedUser = "5790000701414",
            AuthenticatedUserRole = "DDQ",
            BusinessReason = "D05",
            MessageType = "E17",
        };
    }
}
