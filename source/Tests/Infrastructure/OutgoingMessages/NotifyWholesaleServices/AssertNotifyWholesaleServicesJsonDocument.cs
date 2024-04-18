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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using FluentAssertions;
using Json.Schema;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

public sealed class AssertNotifyWholesaleServicesJsonDocument : IAssertNotifyWholesaleServicesDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertNotifyWholesaleServicesJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("NotifyWholesaleServices_MarketDocument");
    }

    public async Task<IAssertNotifyWholesaleServicesDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas
            .GetSchemaAsync<JsonSchema>("NOTIFYWHOLESALESERVICES", "0", CancellationToken.None)
            .ConfigureAwait(false);

        schema.Should().NotBeNull("Cannot validate document without a schema");

        var validationOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var validationResult = schema!.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors)
            .Select(x => (x.InstanceLocation, x.EvaluationPath, x.Errors))
            .SelectMany(
                p => p.Errors!.Values.Select(
                    e => $"==> '{p.InstanceLocation}' does not adhere to '{p.EvaluationPath}' with error: {e}\n"));

        validationResult.IsValid.Should().BeTrue(string.Join("\n", errors));

        return this;
    }

    #region header validation

    public IAssertNotifyWholesaleServicesDocument HasMessageId(string expectedMessageId)
    {
        _root.GetProperty("mRID").GetString().Should().Be(expectedMessageId);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument MessageIdExists()
    {
        _root.TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasBusinessReason(
        BusinessReason expectedBusinessReason,
        CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedBusinessReason);
        _root.GetProperty("process.processType")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedBusinessReason.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderId(ActorNumber expectedSenderId, string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(codingScheme);

        _root.GetProperty("sender_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSenderId.Value);

        _root.GetProperty("sender_MarketParticipant.mRID")
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderRole);
        _root.GetProperty("sender_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSenderRole.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverId);
        _root.GetProperty("receiver_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedReceiverId.Value);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasReceiverRole(ActorRole expectedReceiverRole, CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverRole);
        _root.GetProperty("receiver_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedReceiverRole.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasTimestamp(string expectedTimestamp)
    {
        _root.GetProperty("createdDateTime").GetString().Should().Be(expectedTimestamp);
        return this;
    }

    #endregion

    public IAssertNotifyWholesaleServicesDocument HasTransactionId(Guid expectedTransactionId)
    {
        FirstWholesaleSeriesElement().GetProperty("mRID").GetString().Should().Be(expectedTransactionId.ToString());
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument TransactionIdExists()
    {
        FirstWholesaleSeriesElement().TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCalculationVersion(int expectedVersion)
    {
        FirstWholesaleSeriesElement()
            .GetProperty("version")
            .GetString()
            .Should()
            .Be(expectedVersion.ToString(CultureInfo.InvariantCulture));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementVersion);
        FirstWholesaleSeriesElement()
            .GetProperty("settlement_Series.version")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSettlementVersion.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasOriginalTransactionIdReference(
        string expectedOriginalTransactionIdReference)
    {
        FirstWholesaleSeriesElement()
            .GetProperty("originalTransactionIDReference_Series.mRID")
            .GetString()
            .Should()
            .Be(expectedOriginalTransactionIdReference);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        FirstWholesaleSeriesElement().TryGetProperty("originalTransactionIDReference_Series.mRID", out _).Should().BeFalse();
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod expectedSettlementMethod)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementMethod);
        FirstWholesaleSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSettlementMethod.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceForPosition(int position, string? expectedPrice)
    {
        FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[position - 1]
            .GetProperty("price.amount")
            .GetProperty("value")
            .GetDecimal()
            .ToString(NumberFormatInfo.InvariantInfo)
            .Should()
            .Be(expectedPrice);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType)
    {
        ArgumentNullException.ThrowIfNull(expectedMeteringPointType);
        FirstWholesaleSeriesElement()
            .GetProperty("marketEvaluationPoint.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedMeteringPointType.Code);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        FirstWholesaleSeriesElement()
            .GetProperty("chargeType.mRID")
            .GetString()
            .Should()
            .Be(expectedChargeTypeNumber);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType expectedChargeType)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeType);
        FirstWholesaleSeriesElement()
            .GetProperty("chargeType.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedChargeType.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(
        ActorNumber expectedChargeTypeOwner,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeTypeOwner);
        FirstWholesaleSeriesElement()
            .GetProperty("chargeType.chargeTypeOwner_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedChargeTypeOwner.Value);

        FirstWholesaleSeriesElement()
            .GetProperty("chargeType.chargeTypeOwner_MarketParticipant.mRID")
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasGridAreaCode(string expectedGridAreaCode, string codingScheme)
    {
        FirstWholesaleSeriesElement()
            .GetProperty("meteringGridArea_Domain.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedGridAreaCode);

        FirstWholesaleSeriesElement()
            .GetProperty("meteringGridArea_Domain.mRID")
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasEnergySupplierNumber(
        ActorNumber expectedEnergySupplierNumber,
        string codingScheme)
    {
        ArgumentNullException.ThrowIfNull(expectedEnergySupplierNumber);

        FirstWholesaleSeriesElement()
            .GetProperty("energySupplier_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedEnergySupplierNumber.Value);

        FirstWholesaleSeriesElement()
            .GetProperty("energySupplier_MarketParticipant.mRID")
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(codingScheme);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasProductCode(string expectedProductCode)
    {
        FirstWholesaleSeriesElement().GetProperty("product").GetString().Should().Be(expectedProductCode);
        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedMeasurementUnit);
        FirstWholesaleSeriesElement()
            .GetProperty("quantity_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedMeasurementUnit.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceMeasurementUnit(
        MeasurementUnit expectedPriceMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceMeasurementUnit);
        FirstWholesaleSeriesElement()
            .GetProperty("price_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPriceMeasurementUnit.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasCurrency(Currency expectedPriceUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceUnit);
        FirstWholesaleSeriesElement()
            .GetProperty("currency_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPriceUnit.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("start")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPeriod.StartToString());

        FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("end")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPeriod.EndToString());

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("resolution")
            .GetString()
            .Should()
            .Be(resolution.Code);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasSumQuantityForPosition(int expectedPosition, int expectedSumQuantity)
    {
        var point = FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[0];

        point
            .GetProperty("position")
            .GetProperty("value")
            .GetInt32()
            .Should()
            .Be(expectedPosition);

        point
            .GetProperty("energySum_Quantity.quantity")
            .GetDecimal()
            .Should()
            .Be(expectedSumQuantity);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQuantityForPosition(int expectedPosition, int expectedQuantity)
    {
        var point = FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[0];

        point
            .GetProperty("position")
            .GetProperty("value")
            .GetInt32()
            .Should()
            .Be(expectedPosition);

        point
            .GetProperty("energy_Quantity.quantity")
            .GetDecimal()
            .Should()
            .Be(expectedQuantity);

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementMethodDoesNotExist()
    {
        var act = () => FirstWholesaleSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod");

        act.Should()
            .ThrowExactly<KeyNotFoundException>()
            .WithMessage("The given key was not present in the dictionary.");

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasQualityForPosition(
        int expectedPosition,
        CalculatedQuantityQuality expectedQuantityQuality)
    {
        FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[expectedPosition - 1]
            .GetProperty("quality")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(CimCode.ForWholesaleServicesOf(expectedQuantityQuality));

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument SettlementVersionDoesNotExist()
    {
        var act = () => FirstWholesaleSeriesElement()
            .GetProperty("settlement_Series.version");

        act.Should()
            .ThrowExactly<KeyNotFoundException>()
            .WithMessage("The given key was not present in the dictionary.");

        return this;
    }

    private JsonElement FirstWholesaleSeriesElement()
    {
        var wholesaleSeries = _root.GetProperty("Series").EnumerateArray().ToList();

        wholesaleSeries.Should().NotBeEmpty();

        return wholesaleSeries[0];
    }
}
