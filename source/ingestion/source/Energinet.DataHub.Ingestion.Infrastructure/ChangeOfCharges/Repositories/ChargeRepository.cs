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
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model;
using Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Mapping;
using Microsoft.EntityFrameworkCore;
using ChargeType = Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model.ChargeType;
using MarketParticipant = Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Context.Model.MarketParticipant;

namespace Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges.Repositories
{
    public class ChargeRepository : IChargeRepository
    {
        private readonly IChargesDatabaseContext _chargesDatabaseContext;

        public ChargeRepository(IChargesDatabaseContext chargesDatabaseContext)
        {
            _chargesDatabaseContext = chargesDatabaseContext;
        }

        public async Task<ChargeStorageStatus> StoreChargeAsync(ChangeOfChargesMessage chargeMessage)
        {
            var chargeType = await GetChargeTypeAsync(chargeMessage).ConfigureAwait(false);
            if (chargeType == null) return ChargeStorageStatus.CreateFailure($"No charge type for {chargeMessage.Type}");

            var resolutionType = await GetResolutionTypeAsync(chargeMessage).ConfigureAwait(false);
            if (resolutionType == null) return ChargeStorageStatus.CreateFailure($"No resolution type for {chargeMessage.Period?.Resolution}");

            var vatPayerType = await GetVatPayerTypeAsync(chargeMessage).ConfigureAwait(false);
            if (vatPayerType == null) return ChargeStorageStatus.CreateFailure($"No VAT payer type for {chargeMessage.MktActivityRecord?.ChargeType?.VATPayer}");

            var chargeTypeOwnerMRid = await GetChargeTypeOwnerMRidAsync(chargeMessage) !.ConfigureAwait(false);
            if (chargeTypeOwnerMRid == null) return ChargeStorageStatus.CreateFailure($"No market participant for {chargeMessage.ChargeTypeOwnerMRid}");

            var charge = ChangeOfChargesMapper.MapChangeOfChargesMessageToCharge(chargeMessage, chargeType, chargeTypeOwnerMRid, resolutionType, vatPayerType);

            await _chargesDatabaseContext.Charge.AddAsync(charge);
            await _chargesDatabaseContext.SaveChangesAsync().ConfigureAwait(false);
            return ChargeStorageStatus.CreateSuccess();
        }

        private async Task<MarketParticipant?> GetChargeTypeOwnerMRidAsync(ChangeOfChargesMessage chargeMessage)
        {
            return string.IsNullOrWhiteSpace(chargeMessage.ChargeTypeOwnerMRid)
                ? throw new ArgumentException($"Fails as {nameof(chargeMessage.ChargeTypeOwnerMRid)} is invalid")
                : await _chargesDatabaseContext.MarketParticipant.SingleOrDefaultAsync(type =>
                type.MRid == chargeMessage.ChargeTypeOwnerMRid);
        }

        private async Task<VatPayerType?> GetVatPayerTypeAsync(ChangeOfChargesMessage chargeMessage)
        {
            return string.IsNullOrWhiteSpace(chargeMessage.MktActivityRecord?.ChargeType?.VATPayer)
                ? throw new ArgumentException($"Fails as {nameof(chargeMessage.MktActivityRecord.ChargeType.VATPayer)} is invalid")
                : await _chargesDatabaseContext.VatPayerType.SingleOrDefaultAsync(type =>
                type.Name == chargeMessage.MktActivityRecord.ChargeType.VATPayer);
        }

        private async Task<ResolutionType?> GetResolutionTypeAsync(ChangeOfChargesMessage chargeMessage)
        {
            return string.IsNullOrWhiteSpace(chargeMessage.Period?.Resolution)
                ? throw new ArgumentException($"Fails as {nameof(chargeMessage.Period.Resolution)} is invalid")
                : await _chargesDatabaseContext.ResolutionType.SingleOrDefaultAsync(type => type.Name == chargeMessage.Period.Resolution);
        }

        private async Task<ChargeType?> GetChargeTypeAsync(ChangeOfChargesMessage chargeMessage)
        {
            return string.IsNullOrWhiteSpace(chargeMessage.Type)
                ? throw new ArgumentException($"Fails as {nameof(chargeMessage.Type)} is invalid")
                : await _chargesDatabaseContext.ChargeType.SingleOrDefaultAsync(type => type.Code == chargeMessage.Type);
        }
    }
}
