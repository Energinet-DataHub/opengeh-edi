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
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model;
using ChargeType = Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model.ChargeType;
using MarketParticipant = Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model.MarketParticipant;

namespace Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Mapping
{
    public static class ChangeOfChargesMapper
    {
        public static Charge MapChangeOfChargesMessageToCharge(
            ChangeOfChargesMessage chargeMessage,
            ChargeType chargeType,
            MarketParticipant chargeTypeOwnerMRid,
            ResolutionType resolutionType,
            VatPayerType vatPayerType)
        {
            if (string.IsNullOrWhiteSpace(chargeMessage.ChargeTypeMRid)) throw new ArgumentException($"{nameof(chargeMessage.ChargeTypeMRid)} must have value");
            if (string.IsNullOrWhiteSpace(chargeMessage.CorrelationId)) throw new ArgumentException($"{nameof(chargeMessage.CorrelationId)} must have value");
            if (string.IsNullOrWhiteSpace(chargeMessage.LastUpdatedBy)) throw new ArgumentException($"{nameof(chargeMessage.LastUpdatedBy)} must have value");
            if (chargeMessage.MktActivityRecord?.ChargeType == null) throw new ArgumentException($"{nameof(chargeMessage.MktActivityRecord.ChargeType)} can't be null");
            if (string.IsNullOrWhiteSpace(chargeMessage.MktActivityRecord.ChargeType.Name)) throw new ArgumentException($"{nameof(chargeMessage.MktActivityRecord.ChargeType.Name)} must have value");
            if (string.IsNullOrWhiteSpace(chargeMessage.MktActivityRecord.ChargeType.Description)) throw new ArgumentException($"{nameof(chargeMessage.MktActivityRecord.ChargeType.Description)} must have value");
            if (chargeMessage.Period == null) throw new ArgumentException($"{nameof(chargeMessage.Period)} can't be null");
            if (chargeMessage.Period.Points == null) throw new ArgumentException($"{nameof(chargeMessage.Period.Points)} can't be null");

            var charge = new Charge
            {
                ChargePrices = new List<ChargePrice>(),
                ChargeType = chargeType,
                ChargeTypeOwner = chargeTypeOwnerMRid,
                Description = chargeMessage.MktActivityRecord.ChargeType.Description,
                LastUpdatedBy = chargeMessage.LastUpdatedBy,
                LastUpdatedByCorrelationId = chargeMessage.CorrelationId,
                LastUpdatedByTransactionId = chargeMessage.Transaction.MRID,
                Name = chargeMessage.MktActivityRecord.ChargeType.Name,
                RequestDateTime = chargeMessage.RequestDate.ToUnixTimeTicks(),
                ResolutionType = resolutionType,
                StartDate = chargeMessage.MktActivityRecord.ValidityStartDate.ToUnixTimeTicks(),
                EndDate = chargeMessage.MktActivityRecord.ValidityEndDate?.ToUnixTimeTicks(),
                Status = (int)chargeMessage.MktActivityRecord.Status,
                TaxIndicator = chargeMessage.MktActivityRecord.ChargeType.TaxIndicator,
                TransparentInvoicing = chargeMessage.MktActivityRecord.ChargeType.TransparentInvoicing,
                VatPayer = vatPayerType,
                MRid = chargeMessage.ChargeTypeMRid,
                Currency = "DKK",
            };

            foreach (var point in chargeMessage.Period.Points)
            {
                var newChargePrice = new ChargePrice
                {
                    Time = point.Time.ToUnixTimeTicks(),
                    Amount = point.PriceAmount,
                    LastUpdatedByCorrelationId = chargeMessage.CorrelationId,
                    LastUpdatedByTransactionId = chargeMessage.Transaction.MRID,
                    LastUpdatedBy = chargeMessage.LastUpdatedBy,
                    RequestDateTime = chargeMessage.RequestDate.ToUnixTimeTicks(),
                };

                charge.ChargePrices.Add(newChargePrice);
            }

            return charge;
        }
    }
}
