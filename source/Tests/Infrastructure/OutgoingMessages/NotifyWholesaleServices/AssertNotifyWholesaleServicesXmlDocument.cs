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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.XPath;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

public class AssertNotifyWholesaleServicesXmlDocument : IAssertNotifyWholesaleServicesDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertNotifyWholesaleServicesXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E31");
    }

    public async Task<IAssertNotifyWholesaleServicesDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyWholesaleServices).ConfigureAwait(false);

        return this;
    }

    #region header validation

    public IAssertNotifyWholesaleServicesDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasBusinessReason(
        BusinessReason expectedBusinessReason,
        CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedBusinessReason);
        _documentAsserter.HasValue("process.processType", expectedBusinessReason.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderId(ActorNumber expectedSenderId, string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(codingScheme);

        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId.Value);
        _documentAsserter.HasAttribute("sender_MarketParticipant.mRID", "codingScheme", codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderRole);
        _documentAsserter.HasValue("sender_MarketParticipant.marketRole.type", expectedSenderRole.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverId);
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId.Value);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverRole(
        ActorRole expectedReceiverRole,
        CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverRole);
        _documentAsserter.HasValue("receiver_MarketParticipant.marketRole.type", expectedReceiverRole.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    #endregion

    public IAssertNotifyWholesaleServicesDocument HasTransactionId(Guid expectedTransactionId)
    {
        _documentAsserter.HasValue($"Series[1]/mRID", expectedTransactionId.ToString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists($"Series[1]/mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCalculationVersion(long expectedVersion)
    {
        _documentAsserter.HasValue($"Series[1]/version", expectedVersion.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementVersion);
        _documentAsserter.HasValue("Series[1]/settlement_Series.version", expectedSettlementVersion.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasOriginalTransactionIdReference(
        string expectedOriginalTransactionIdReference)
    {
        _documentAsserter.HasValue(
            "Series[1]/originalTransactionIDReference_Series.mRID",
            expectedOriginalTransactionIdReference);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/originalTransactionIDReference_Series.mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod expectedSettlementMethod)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementMethod);
        _documentAsserter.HasValue(
                "Series[1]/marketEvaluationPoint.settlementMethod",
                expectedSettlementMethod.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceForPosition(int position, string? expectedPrice)
    {
        _documentAsserter.HasValue($"Series[1]/Period/Point[{position}]/price.amount", expectedPrice ?? "0");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType)
    {
        ArgumentNullException.ThrowIfNull(expectedMeteringPointType);
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.type", expectedMeteringPointType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument MeteringPointTypeDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.type");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        _documentAsserter.HasValue("Series[1]/chargeType.mRID", expectedChargeTypeNumber);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ChargeCodeDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/chargeType.mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType expectedChargeType)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeType);
        _documentAsserter.HasValue("Series[1]/chargeType.type", expectedChargeType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ChargeTypeDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/chargeType.type");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(
        ActorNumber expectedChargeTypeOwner,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeTypeOwner);
        _documentAsserter.HasValue(
            "Series[1]/chargeType.chargeTypeOwner_MarketParticipant.mRID",
            expectedChargeTypeOwner.Value);
        _documentAsserter.HasAttribute(
            "Series[1]/chargeType.chargeTypeOwner_MarketParticipant.mRID",
            "codingScheme",
            codingScheme);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ChargeTypeOwnerDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/chargeType.chargeTypeOwner_MarketParticipant.mRID");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasGridAreaCode(string expectedGridAreaCode, string codingScheme)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        _documentAsserter.HasAttribute("Series[1]/meteringGridArea_Domain.mRID", "codingScheme", codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasEnergySupplierNumber(
        ActorNumber expectedEnergySupplierNumber,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedEnergySupplierNumber);
        _documentAsserter.HasValue(
            "Series[1]/energySupplier_MarketParticipant.mRID",
            expectedEnergySupplierNumber.Value);
        _documentAsserter.HasAttribute("Series[1]/energySupplier_MarketParticipant.mRID", "codingScheme", codingScheme);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/quantity_Measure_Unit.name", expectedMeasurementUnit.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceMeasurementUnit(MeasurementUnit expectedPriceMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/price_Measure_Unit.name", expectedPriceMeasurementUnit.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument PriceMeasurementUnitDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/price_Measure_Unit.name");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCurrency(Currency expectedPriceUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceUnit);
        _documentAsserter.HasValue("Series[1]/currency_Unit.name", expectedPriceUnit.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedPeriod.StartToString())
            .HasValue("Series[1]/Period/timeInterval/end", expectedPeriod.EndToString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _documentAsserter.HasValue("Series[1]/Period/resolution", resolution.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument ResolutionDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/Period/resolution");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSumQuantityForPosition(
        int expectedPosition,
        int expectedSumQuantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue(
                "Series[1]/Period/Point[1]/energySum_Quantity.quantity",
                expectedSumQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityForPosition(int expectedPosition, int expectedQuantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", expectedPosition.ToString(CultureInfo.InvariantCulture))
            .HasValue(
                "Series[1]/Period/Point[1]/energy_Quantity.quantity",
                expectedQuantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementMethodDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQualityForPosition(
        int expectedPosition,
        CalculatedQuantityQuality expectedQuantityQuality)
    {
        _documentAsserter.HasValue(
            $"Series[1]/Period/Point[{expectedPosition}]/quality",
            CimCode.ForWholesaleServicesOf(expectedQuantityQuality));
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasAnyPoints()
    {
        _documentAsserter.ElementExists($"Series[1]/Period/Point[0]");
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPoints(
        IReadOnlyCollection<WholesaleServicesRequestSeries.Types.Point> points)
    {
        var pointsInDocument = _documentAsserter
            .GetElements("Series[1]/Period/Point")!;

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Time).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("energySum_Quantity.quantity"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToDecimal()
                .Should()
                .Be(expectedPoints[i].Amount.ToDecimal());

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("energy_Quantity.quantity"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToDecimal()
                .Should()
                .Be(expectedPoints[i].Quantity.ToDecimal());

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("position"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToInt()
                .Should()
                .Be(i + 1);

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("price.amount"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToDecimal()
                .Should()
                .Be(expectedPoints[i].Price.ToDecimal());

            var expectedQuantityQuality = expectedPoints[i].QuantityQualities.Single() switch
            {
                // For WholesaleServices then calculated, estimated and measured is written as calculated
                QuantityQuality.Calculated => CimCode.QuantityQualityCodeCalculated,
                QuantityQuality.Estimated => CimCode.QuantityQualityCodeCalculated,
                QuantityQuality.Measured => CimCode.QuantityQualityCodeCalculated,
                _ => throw new NotImplementedException(
                    $"Quantity quality {expectedPoints[i].QuantityQualities.Single()} not implemented"),
            };

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("quality"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .Should()
                .Be(expectedQuantityQuality);
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSinglePointWithAmount(DecimalValue expectedAmount)
    {
        ArgumentNullException.ThrowIfNull(expectedAmount);
        var pointsInDocument = _documentAsserter
            .GetElements("Series[1]/Period/Point")!;

        var pointInDocument = pointsInDocument.Single();

        pointInDocument
            .XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("energySum_Quantity.quantity"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToDecimal()
            .Should()
            .Be(expectedAmount.ToDecimal());

        pointInDocument.XPathSelectElement(
                _documentAsserter.EnsureXPathHasPrefix("position"),
                _documentAsserter.XmlNamespaceManager)!
            .Value
            .ToInt()
            .Should()
            .Be(1);

        _documentAsserter.IsNotPresent("Series[1]/Period/Point[1]/energy_Quantity.quantity");

        _documentAsserter.IsNotPresent("Series[1]/Period/Point[1]/price.amount");

        _documentAsserter.IsNotPresent("Series[1]/Period/Point[1]/quality");

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementVersionDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/settlement_Series.version");
        return this;
    }
}
