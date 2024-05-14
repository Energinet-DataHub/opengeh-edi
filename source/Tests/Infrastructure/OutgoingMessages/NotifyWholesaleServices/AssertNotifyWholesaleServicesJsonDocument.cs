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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Json.Schema;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

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
        var validationErrors = validationResult.Details.Where(detail => !detail.IsValid && detail.HasErrors)
            .Select(x => (x.InstanceLocation, x.EvaluationPath, x.Errors))
            .SelectMany(
                p => p.Errors!.Values.Select(
                    e => $"==> '{p.InstanceLocation}' does not adhere to '{p.EvaluationPath}' with error: {e}\n"))
            .ToList();

        validationResult.IsValid.Should().BeTrue($"because document should be valid. Validation errors:{Environment.NewLine}{{0}}", string.Join("\n", validationErrors));

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

    public IAssertNotifyWholesaleServicesDocument HasCalculationVersion(long expectedVersion)
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

    public IAssertNotifyWholesaleServicesDocument HasSettlementMethod(SettlementMethod? expectedSettlementMethod)
    {
        if (expectedSettlementMethod is not null)
        {
            FirstWholesaleSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSettlementMethod.Code);
        }
        else
        {
            SettlementMethodDoesNotExist();
        }

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

    public IAssertNotifyWholesaleServicesDocument HasMeteringPointType(MeteringPointType? expectedMeteringPointType)
    {
        if (expectedMeteringPointType is not null)
        {
            FirstWholesaleSeriesElement()
                .GetProperty("marketEvaluationPoint.type")
                .GetProperty("value")
                .GetString()
                .Should()
                .Be(expectedMeteringPointType.Code);
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("marketEvaluationPoint.type");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeCode(string? expectedChargeTypeNumber)
    {
        if (expectedChargeTypeNumber is not null)
        {
            FirstWholesaleSeriesElement()
                .GetProperty("chargeType.mRID")
                .GetString()
                .Should()
                .Be(expectedChargeTypeNumber);
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("chargeType.mRID");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeType(ChargeType? expectedChargeType)
    {
        if (expectedChargeType is not null)
        {
            FirstWholesaleSeriesElement()
                .GetProperty("chargeType.type")
                .GetProperty("value")
                .GetString()
                .Should()
                .Be(expectedChargeType.Code);
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("chargeType.type");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasChargeTypeOwner(
        ActorNumber? expectedChargeTypeOwner,
        string codingScheme)
    {
        if (expectedChargeTypeOwner is not null)
        {
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
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("chargeType.chargeTypeOwner_MarketParticipant.mRID");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

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
        if (expectedMeasurementUnit is not null)
        {
            FirstWholesaleSeriesElement()
                .GetProperty("quantity_Measure_Unit.name")
                .GetProperty("value")
                .GetString()
                .Should()
                .Be(expectedMeasurementUnit.Code);
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("quantity_Measure_Unit.name");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPriceMeasurementUnit(
        MeasurementUnit? expectedPriceMeasurementUnit)
    {
        if (expectedPriceMeasurementUnit is not null)
        {
            FirstWholesaleSeriesElement()
            .GetProperty("price_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPriceMeasurementUnit.Code);
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("price_Measure_Unit.name");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

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

    public IAssertNotifyWholesaleServicesDocument HasResolution(Resolution? resolution)
    {
        if (resolution is not null)
        {
            FirstWholesaleSeriesElement()
                .GetProperty("Period")
                .GetProperty("resolution")
                .GetString()
                .Should()
                .Be(resolution.Code);
        }
        else
        {
            var act = () => FirstWholesaleSeriesElement()
                .GetProperty("Period")
                .GetProperty("resolution");

            act.Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage("The given key was not present in the dictionary.");
        }

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

    public IAssertNotifyWholesaleServicesDocument HasAnyPoints()
    {
        FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .TryGetProperty("Point", out _)
            .Should().BeTrue();

        return this;
    }

    public IAssertNotifyWholesaleServicesDocument HasPoints(IReadOnlyCollection<WholesaleServicesRequestSeries.Types.Point> points)
    {
        var pointsInDocument = FirstWholesaleSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .OrderBy(p => p.GetProperty("position")
                                        .GetProperty("value")
                                        .GetInt32())
            .ToList();

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Time).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            pointsInDocument[i]
                .GetProperty("energySum_Quantity.quantity")
                .GetDecimal()
                .Should()
                .Be(expectedPoints[i].Amount.ToDecimal());

            if (expectedPoints[i].Quantity is not null)
            {
                pointsInDocument[i]
                    .GetProperty("energy_Quantity.quantity")
                    .GetDecimal()
                    .Should()
                    .Be(expectedPoints[i].Quantity.ToDecimal());
            }
            else
            {
                var act = () => pointsInDocument[i]
                    .GetProperty("energy_Quantity.quantity");

                act.Should()
                    .ThrowExactly<KeyNotFoundException>()
                    .WithMessage("The given key was not present in the dictionary.");
            }

            pointsInDocument[i]
                .GetProperty("position")
                .GetProperty("value")
                .GetInt32()
                .Should()
                .Be(i + 1);

            if (expectedPoints[i].Price is not null)
            {
                pointsInDocument[i]
                    .GetProperty("price.amount")
                    .GetProperty("value")
                    .GetDecimal()
                    .Should()
                    .Be(expectedPoints[i].Price.ToDecimal());
            }
            else
            {
                var act = () => pointsInDocument[i]
                    .GetProperty("price.amount");

                act.Should()
                    .ThrowExactly<KeyNotFoundException>()
                    .WithMessage("The given key was not present in the dictionary.");
            }

            if (expectedPoints[i].QuantityQualities.Count > 0)
            {
                var expectedQuantityQuality = expectedPoints[i].QuantityQualities.Single() switch
                {
                    QuantityQuality.Calculated => "A06",
                    _ => throw new NotImplementedException(
                        $"Quantity quality {expectedPoints[i].QuantityQualities.Single()} not implemented"),
                };

                pointsInDocument[i]
                    .GetProperty("quality")
                    .GetProperty("value")
                    .GetString()
                    .Should()
                    .Be(expectedQuantityQuality);
            }
            else
            {
                var act = () => pointsInDocument[i]
                    .GetProperty("quality");

                act.Should()
                    .ThrowExactly<KeyNotFoundException>()
                    .WithMessage("The given key was not present in the dictionary.");
            }
        }

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
