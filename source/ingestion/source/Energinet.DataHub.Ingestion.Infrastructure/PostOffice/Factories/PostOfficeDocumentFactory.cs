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
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Domain.Common;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.Common;
using NodaTime;
using ChargeType = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges.ChargeType;
using MarketDocument = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.Common.MarketDocument;
using MarketParticipant = Energinet.DataHub.Ingestion.Domain.Common.MarketParticipant;
using MktActivityRecord = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges.MktActivityRecord;
using MktActivityRecordStatus = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges.MktActivityRecordStatus;
using Point = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges.Point;
using ProcessType = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.Common.ProcessType;
using ServiceCategoryKind = Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.Common.ServiceCategoryKind;

namespace Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Factories
{
    public class PostOfficeDocumentFactory : IPostOfficeDocumentFactory
    {
        private readonly IClock _clock;
        private readonly IPostOfficeDocumentFactorySettings _settings;

        public PostOfficeDocumentFactory(IClock clock, IPostOfficeDocumentFactorySettings settings)
        {
            _clock = clock;
            _settings = settings;
        }

        public IEnumerable<ChargeChangeNotificationDocument> Create(
            IEnumerable<MarketParticipant> energySuppliers,
            ChangeOfChargesMessage message)
        {
            foreach (var energySupplier in energySuppliers)
            {
                yield return CreateDocument(energySupplier!.MRid!, message);
            }
        }

        private ChargeChangeNotificationDocument CreateDocument(string receiverMRid, ChangeOfChargesMessage message)
        {
            return new ChargeChangeNotificationDocument
            {
                Recipient = receiverMRid,
                EffectuationDate = message.MktActivityRecord!.ValidityStartDate,
                Content = CreateDocumentContent(receiverMRid, message),
            };
        }

        private ChargeChangeNotificationContent CreateDocumentContent(string receiverMRid, ChangeOfChargesMessage message)
        {
            return new ChargeChangeNotificationContent
            {
                MarketDocument = MapMarketDocument(receiverMRid),
                MktActivityRecord = MapMktActivityRecord(message),
                TimeSeriesPeriod = MapTimeSeriesPeriod(message!.Period!),
            };
        }

        private MarketDocument MapMarketDocument(string receiverMRid)
        {
            return new MarketDocument
            {
                MRid = Guid.NewGuid().ToString(),
                ProcessType = ProcessType.UpdateChargeInformation,
                CreatedDateTime = _clock.GetCurrentInstant(),
                ReceiverMarketParticipant = MapMarketParticipant(receiverMRid, MarketRoleType.EnergySupplier),
                SenderMarketParticipant = MapMarketParticipant(
                    _settings.GetHubMRid(),
                    MarketRoleType.TransmissionSystemOperator),
                MarketServiceCategoryKind = ServiceCategoryKind.Electricity,
            };
        }

        private Dtos.Common.MarketParticipant MapOwnerMarketParticipant(ChangeOfChargesMessage message)
        {
            return new Dtos.Common.MarketParticipant
            {
                MRid = message.ChargeTypeOwnerMRid,
                MarketRoleType = MapMarketParticipantRole(message.MarketDocument!.SenderMarketParticipant!.Role!.Value),
            };
        }

        private Dtos.Common.MarketParticipant MapMarketParticipant(string marketParticipantMRid, MarketRoleType role)
        {
            return new Dtos.Common.MarketParticipant
            {
                MRid = marketParticipantMRid,
                MarketRoleType = role,
            };
        }

        private MarketRoleType MapMarketParticipantRole(MarketParticipantRole role)
        {
            return role switch
            {
                MarketParticipantRole.EnergySupplier => MarketRoleType.EnergySupplier,
                _ => throw new NotImplementedException(role.ToString()),
            };
        }

        private MktActivityRecord MapMktActivityRecord(ChangeOfChargesMessage message)
        {
            return new MktActivityRecord
            {
                MRid = message.MktActivityRecord!.MRid,
                Status = MapActivityRecordStatus(message.MktActivityRecord!.Status),
                ValidityStartDateAndOrTime = message.MktActivityRecord!.ValidityStartDate,
                ChargeType = MapChargeType(message),
            };
        }

        private MktActivityRecordStatus MapActivityRecordStatus(Domain.ChangeOfCharges.MktActivityRecordStatus status)
        {
            return (MktActivityRecordStatus)status;
        }

        private ChargeType MapChargeType(ChangeOfChargesMessage message)
        {
            return new ChargeType
            {
                MRid = message.ChargeTypeMRid,
                ChargeTypeKind = MapChargeTypeKind(message.Type!),
                Name = message.MktActivityRecord!.ChargeType!.Name,
                Description = message.MktActivityRecord!.ChargeType!.Description,
                VatPayer = MapVatPayer(message.MktActivityRecord!.ChargeType!.VATPayer!),
                TaxIndicator = message.MktActivityRecord!.ChargeType!.TaxIndicator,
                TransparentInvoicing = message.MktActivityRecord!.ChargeType!.TransparentInvoicing,
                ChargeTypeOwnerMarketParticipant = MapOwnerMarketParticipant(message),
            };
        }

        private ChargeTypeKind MapChargeTypeKind(string chargeType)
        {
            switch (chargeType)
            {
                case "D01":
                    return ChargeTypeKind.Subscription;
                case "D02":
                    return ChargeTypeKind.Fee;
                case "D03":
                    return ChargeTypeKind.Tariff;
                default:
                    throw new ArgumentException(nameof(ChargeTypeKind));
            }
        }

        private VatPayer MapVatPayer(string payer)
        {
            switch (payer)
            {
                case "D01":
                    return VatPayer.NoVat;
                case "D02":
                    return VatPayer.Vat;
                default:
                    throw new ArgumentException(nameof(VatPayer));
            }
        }

        private TimeSeriesPeriod MapTimeSeriesPeriod(ChargeTypePeriod period)
        {
            return new TimeSeriesPeriod
            {
                Resolution = period.Resolution,
                Points = MapPoints(period.Points!),
            };
        }

        private List<Point> MapPoints(List<Domain.ChangeOfCharges.Point> points)
        {
            return points
                .Select(p => new Point { Position = p.Position, PriceAmount = p.PriceAmount })
                .ToList();
        }
    }
}
