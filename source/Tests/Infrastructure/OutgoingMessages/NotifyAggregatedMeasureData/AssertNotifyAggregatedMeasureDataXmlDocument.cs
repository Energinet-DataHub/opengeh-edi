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

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

public class AssertNotifyAggregatedMeasureDataXmlDocument : IAssertNotifyAggregatedMeasureDataDocument
{
    private readonly AssertXmlDocument _documentAsserter;

    public AssertNotifyAggregatedMeasureDataXmlDocument(AssertXmlDocument documentAsserter)
    {
        _documentAsserter = documentAsserter;
        _documentAsserter.HasValue("type", "E31");
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasMessageId(string expectedMessageId)
    {
        _documentAsserter.HasValue("mRID", expectedMessageId);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument MessageIdExists()
    {
        _documentAsserter.ElementExists("mRID");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSenderId(string expectedSenderId)
    {
        _documentAsserter.HasValue("sender_MarketParticipant.mRID", expectedSenderId);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId)
    {
        _documentAsserter.HasValue("receiver_MarketParticipant.mRID", expectedReceiverId);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasTimestamp(string expectedTimestamp)
    {
        _documentAsserter.HasValue("createdDateTime", expectedTimestamp);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedTransactionId);
        _documentAsserter.HasValue("Series[1]/mRID", expectedTransactionId.Value);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument TransactionIdExists()
    {
        _documentAsserter.ElementExists("Series[1]/mRID");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        _documentAsserter.HasValue("Series[1]/meteringGridArea_Domain.mRID", expectedGridAreaCode);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        _documentAsserter.HasValue("Series[1]/balanceResponsibleParty_MarketParticipant.mRID", expectedBalanceResponsibleNumber);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        _documentAsserter.HasValue("Series[1]/energySupplier_MarketParticipant.mRID", expectedEnergySupplierNumber);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasProductCode(string expectedProductCode)
    {
        _documentAsserter.HasValue("Series[1]/product", expectedProductCode);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        _documentAsserter
            .HasValue("Series[1]/Period/timeInterval/start", expectedPeriod.StartToString())
            .HasValue("Series[1]/Period/timeInterval/end", expectedPeriod.EndToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPoint(int position, int quantity)
    {
        _documentAsserter
            .HasValue("Series[1]/Period/Point[1]/position", position.ToString(CultureInfo.InvariantCulture))
            .HasValue("Series[1]/Period/Point[1]/quantity", quantity.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public async Task<IAssertNotifyAggregatedMeasureDataDocument> DocumentIsValidAsync()
    {
        await _documentAsserter.HasValidStructureAsync(DocumentType.NotifyAggregatedMeasureData).ConfigureAwait(false);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument SettlementMethodIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/marketEvaluationPoint.settlementMethod");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument EnergySupplierNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/energySupplier_MarketParticipant.mRID");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument BalanceResponsibleNumberIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/balanceResponsibleParty_MarketParticipant.mRID");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QuantityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quantity");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QualityIsNotPresentForPosition(int position)
    {
        _documentAsserter.IsNotPresent($"Series[1]/Period/Point[{position}]/quality");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QualityIsPresentForPosition(int position, string quantityQualityCode)
    {
        _documentAsserter.HasValue($"Series[1]/Period/Point[{position}]/quality", quantityQualityCode);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasCalculationResultVersion(long version)
    {
        _documentAsserter.HasValue($"Series[1]/version", version.ToString(NumberFormatInfo.InvariantInfo));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasMeteringPointType(MeteringPointType meteringPointType)
    {
        ArgumentNullException.ThrowIfNull(meteringPointType);
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.type", meteringPointType.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasQuantityMeasurementUnit(MeasurementUnit quantityMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(quantityMeasurementUnit);
        _documentAsserter.HasValue("Series[1]/quantity_Measure_Unit.name", quantityMeasurementUnit.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _documentAsserter.HasValue("Series[1]/Period/resolution", resolution.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPoints(IReadOnlyCollection<TimeSeriesPointAssertionInput> points)
    {
        var pointsInDocument = _documentAsserter
            .GetElements("Series[1]/Period/Point")!;

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Time).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("position"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToInt()
                .Should()
                .Be(i + 1);

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("quantity"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .ToDecimal()
                .Should()
                .Be(expectedPoints[i].Quantity);

            var expectedQuantityQuality = CimCode.ForEnergyResultOf(expectedPoints[i].Quality);

            pointsInDocument[i]
                .XPathSelectElement(_documentAsserter.EnsureXPathHasPrefix("quality"), _documentAsserter.XmlNamespaceManager)!
                .Value
                .Should()
                .Be(expectedQuantityQuality);
        }

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        _documentAsserter.HasValue("process.processType", businessReason.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(settlementVersion);
        _documentAsserter.HasValue("Series[1]/settlement_Series.version", settlementVersion.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument SettlementVersionIsNotPresent()
    {
        _documentAsserter.IsNotPresent("Series[1]/settlement_Series.version");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasOriginalTransactionIdReference(
        TransactionId originalTransactionIdReference)
    {
        ArgumentNullException.ThrowIfNull(originalTransactionIdReference);
        _documentAsserter.HasValue(
            "Series[1]/originalTransactionIDReference_Series.mRID",
            originalTransactionIdReference.Value);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        _documentAsserter.IsNotPresent("Series[1]/originalTransactionIDReference_Series.mRID");
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSettlementMethod(SettlementMethod settlementMethod)
    {
        ArgumentNullException.ThrowIfNull(settlementMethod);
        _documentAsserter.HasValue("Series[1]/marketEvaluationPoint.settlementMethod", settlementMethod.Code);
        return this;
    }
}
