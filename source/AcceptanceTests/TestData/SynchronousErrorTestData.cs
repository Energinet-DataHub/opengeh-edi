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

namespace Energinet.DataHub.EDI.AcceptanceTests.TestData;

public static class SynchronousErrorTestData
{
    public static Dictionary<string, string> DefaultEnergySupplierTestData()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", Guid.NewGuid().ToString() },
                { "cim:type", "E74" },
                { "cim:process.processType", "D05" },
                { "cim:businessSector.type", "23" },
                { "cim:sender_MarketParticipant.mRID", "5790000392551" },
                { "cim:sender_MarketParticipant.marketRole.type", "DDQ" },
                { "cim:receiver_MarketParticipant.mRID", "5790001330552" },
                { "cim:receiver_MarketParticipant.marketRole.type", "DGL" },
                { "cim:createdDateTime", "2022-12-17T09:30:47Z" },
            };
    }

    public static Dictionary<string, string> DefaultEnergySupplierSeriesTestData()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", Guid.NewGuid().ToString() },
                { "cim:settlement_Series.version", "D01" },
                { "cim:marketEvaluationPoint.type", "E17" },
                { "cim:marketEvaluationPoint.settlementMethod", "D01" },
                { "cim:start_DateAndOrTime.dateTime", "2022-06-23T22:00:00Z" },
                { "cim:end_DateAndOrTime.dateTime", "2022-07-18T22:00:00Z" },
                { "cim:meteringGridArea_Domain.mRID", "804" },
                { "cim:energySupplier_MarketParticipant.mRID", "5790000392551" },
            };
    }

    public static Dictionary<string, string> WrongSenderMarketParticipantMrid()
    {
        return new Dictionary<string, string>
            {
                { "cim:sender_MarketParticipant.mRID", "5790000701413" },
            };
    }

    public static Dictionary<string, string> SenderRoleTypeNotAuthorized()
    {
        return new Dictionary<string, string>
            {
                { "cim:sender_MarketParticipant.marketRole.type", "DGL" },
            };
    }

    public static Dictionary<string, string> MessageIdIsNotUnique()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", "B6Qhv7Dls6zdnvgna3cQqXu0PAzFqKco8GLc" },
            };
    }

    public static Dictionary<string, string> TransactionIdIsNotUnique()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", "aX5fNO7st0zVIemSRek4GM1FCSRbQ28PMIZO" },
            };
    }

    public static Dictionary<string, string> EmptyMessageId()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", string.Empty },
            };
    }

    public static Dictionary<string, string> SchemaValidationErrorOnWrongBusinessSectorType()
    {
        return new Dictionary<string, string>
            {
                { "cim:businessSector.type", "232" },
            };
    }

    public static Dictionary<string, string> InvalidLengthOfMessageId()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", Guid.NewGuid() + "1" },
            };
    }

    public static Dictionary<string, string> EmptyTransactionId()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", string.Empty },
            };
    }

    public static Dictionary<string, string> InvalidTransactionId()
    {
        return new Dictionary<string, string>
            {
                { "cim:mRID", "invalidId" },
            };
    }

    public static Dictionary<string, string> TypeIsNotSupported()
    {
        return new Dictionary<string, string>
            {
                { "cim:type", "E73" },
            };
    }

    public static Dictionary<string, string> ProcessTypeIsNotSupported()
    {
        return new Dictionary<string, string>
            {
                { "cim:process.processType", "D09" },
            };
    }

    public static Dictionary<string, string> InvalidBusinessType()
    {
        return new Dictionary<string, string>
            {
                { "cim:businessSector.type", "27" },
            };
    }

    public static Dictionary<string, string> InvalidReceiverId()
    {
        return new Dictionary<string, string>
            {
                { "cim:receiver_MarketParticipant.mRID", "5790001330553" },
            };
    }

    public static Dictionary<string, string> InvalidReceiverRole()
    {
        return new Dictionary<string, string>
            {
                { "cim:receiver_MarketParticipant.marketRole.type", "DDZ" },
            };
    }
}
