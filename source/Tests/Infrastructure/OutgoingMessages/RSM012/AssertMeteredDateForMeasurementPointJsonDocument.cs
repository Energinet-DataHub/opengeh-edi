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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using FluentAssertions;
using Json.Schema;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

public class AssertMeteredDateForMeasurementPointJsonDocument : IAssertMeteredDateForMeasurementPointDocumentDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertMeteredDateForMeasurementPointJsonDocument(Stream documentStream)
    {
        _document = JsonDocument.Parse(documentStream);
        _root = _document.RootElement.GetProperty("NotifyValidatedMeasureData_MarketDocument");

        Assert.Equal("E66", _root.GetProperty("type").GetProperty("value").ToString());
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument MessageIdExists()
    {
        _root.TryGetProperty("mRID", out _).Should().BeTrue("property 'mRID' should be present");
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasBusinessReason(string expectedBusinessReasonCode)
    {
        Assert.Equal(expectedBusinessReasonCode, _root.GetProperty("process.processType").GetProperty("value").ToString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderId(string expectedSenderId, string expectedSchemeCode)
    {
        Assert.Equal(expectedSenderId, _root.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString());
        Assert.Equal(expectedSchemeCode, _root.GetProperty("sender_MarketParticipant.mRID").GetProperty("codingScheme").ToString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasSenderRole(string expectedSenderRole)
    {
        Assert.Equal(expectedSenderRole, _root.GetProperty("sender_MarketParticipant.marketRole.type").GetProperty("value").ToString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverId(string expectedReceiverId, string expectedSchemeCode)
    {
        Assert.Equal(expectedReceiverId, _root.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString());
        Assert.Equal(expectedSchemeCode, _root.GetProperty("receiver_MarketParticipant.mRID").GetProperty("codingScheme").ToString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasReceiverRole(string expectedReceiverRole)
    {
        Assert.Equal(expectedReceiverRole, _root.GetProperty("receiver_MarketParticipant.marketRole.type").GetProperty("value").ToString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasTimestamp(string expectedTimestamp)
    {
        Assert.Equal(expectedTimestamp, _root.GetProperty("createdDateTime").ToString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasTransactionId(
        int seriesIndex,
        TransactionId expectedTransactionId)
    {
        Assert.Equal(expectedTransactionId.Value, GetTimeSeriesElement(seriesIndex).GetProperty("mRID").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointNumber(
        int seriesIndex,
        string expectedMeteringPointNumber,
        string expectedSchemeCode)
    {
        GetTimeSeriesElement(seriesIndex)
            .GetProperty("marketEvaluationPoint.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedMeteringPointNumber);

        GetTimeSeriesElement(seriesIndex)
            .GetProperty("marketEvaluationPoint.mRID")
            .GetProperty("codingScheme")
            .GetString()
            .Should()
            .Be(expectedSchemeCode);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointType(
        int seriesIndex,
        string expectedMeteringPointType)
    {
        GetTimeSeriesElement(seriesIndex)
            .GetProperty("marketEvaluationPoint.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedMeteringPointType);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasOriginalTransactionIdReferenceId(
        int seriesIndex,
        string? expectedOriginalTransactionIdReferenceId)
    {
        if (expectedOriginalTransactionIdReferenceId is null)
        {
            GetTimeSeriesElement(seriesIndex)
                .TryGetProperty("originalTransactionIDReference_Series.mRID", out _)
                .Should()
                .BeFalse("property 'originalTransactionIDReference_Series.mRID' should not be present");

            return this;
        }

        GetTimeSeriesElement(seriesIndex)
            .GetProperty("originalTransactionIDReference_Series.mRID")
            .GetString()
            .Should()
            .Be(expectedOriginalTransactionIdReferenceId);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasProduct(int seriesIndex, string? expectedProduct)
    {
        if (expectedProduct is null)
        {
            GetTimeSeriesElement(seriesIndex)
                .TryGetProperty("product", out _)
                .Should()
                .BeFalse("property 'product' should not be present");

            return this;
        }

        GetTimeSeriesElement(seriesIndex).GetProperty("product").GetString().Should().Be(expectedProduct);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasQuantityMeasureUnit(
        int seriesIndex,
        string expectedQuantityMeasureUnit)
    {
        GetTimeSeriesElement(seriesIndex)
            .GetProperty("quantity_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedQuantityMeasureUnit);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasRegistrationDateTime(
        int seriesIndex,
        string? expectedRegistrationDateTime)
    {
        if (expectedRegistrationDateTime is null)
        {
            GetTimeSeriesElement(seriesIndex)
                .TryGetProperty("registration_DateAndOrTime.dateTime", out _)
                .Should()
                .BeFalse("property 'registration_DateAndOrTime.dateTime' should not be present");

            return this;
        }

        GetTimeSeriesElement(seriesIndex)
            .GetProperty("registration_DateAndOrTime.dateTime")
            .GetString()
            .Should()
            .Be(expectedRegistrationDateTime);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasResolution(
        int seriesIndex,
        string expectedResolution)
    {
        GetTimeSeriesPeriodElement(seriesIndex).GetProperty("resolution").GetString().Should().Be(expectedResolution);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasStartedDateTime(
        int seriesIndex,
        string expectedStartedDateTime)
    {
        GetTimeSeriesPeriodElement(seriesIndex)
            .GetProperty("timeInterval")
            .GetProperty("start")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedStartedDateTime);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasEndedDateTime(
        int seriesIndex,
        string expectedEndedDateTime)
    {
        GetTimeSeriesPeriodElement(seriesIndex)
            .GetProperty("timeInterval")
            .GetProperty("end")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedEndedDateTime);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasInDomain(int seriesIndex, string? expectedInDomain)
    {
        if (expectedInDomain is null)
        {
            GetTimeSeriesElement(seriesIndex)
                .TryGetProperty("in_Domain.mRID", out _)
                .Should()
                .BeFalse("property 'in_Domain.mRID' should not be present");

            return this;
        }

        GetTimeSeriesElement(seriesIndex)
            .GetProperty("in_Domain.mRID")
            .GetString()
            .Should()
            .Be(expectedInDomain);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasOutDomain(int seriesIndex, string? expectedOutDomain)
    {
        if (expectedOutDomain is null)
        {
            GetTimeSeriesElement(seriesIndex)
                .TryGetProperty("out_Domain.mRID", out _)
                .Should()
                .BeFalse("property 'out_Domain.mRID' should not be present");

            return this;
        }

        GetTimeSeriesElement(seriesIndex)
            .GetProperty("out_Domain.mRID")
            .GetString()
            .Should()
            .Be(expectedOutDomain);

        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasPoints(
        int seriesIndex,
        IReadOnlyList<AssertPointDocumentFieldsInput> expectedPoints)
    {
        var points = GetTimeSeriesPeriodElement(seriesIndex).GetProperty("Point").EnumerateArray().ToList();

        points.Should().HaveCount(expectedPoints.Count);

        for (var i = 0; i < expectedPoints.Count; i++)
        {
            var (requiredPointDocumentFields, optionalPointDocumentFields) = expectedPoints[i];
            var actualPoint = points[i];

            actualPoint.GetProperty("position")
                .GetProperty("value")
                .GetInt32()
                .Should()
                .Be(requiredPointDocumentFields.Position);

            if (optionalPointDocumentFields.Quality != null)
            {
                actualPoint.GetProperty("quality")
                    .GetProperty("value")
                    .GetString()
                    .Should()
                    .Be(optionalPointDocumentFields.Quality);
            }
            else
            {
                AssertPropertyNotPresent(actualPoint, "quality");
            }

            if (optionalPointDocumentFields.Quantity != null)
            {
                actualPoint.GetProperty("quantity").GetDecimal().Should().Be(optionalPointDocumentFields.Quantity);
            }
            else
            {
                AssertPropertyNotPresent(actualPoint, "quantity");
            }
        }

        return this;

        void AssertPropertyNotPresent(JsonElement actualPoint, string propertyName)
        {
            actualPoint.TryGetProperty(propertyName, out _)
                .Should()
                .BeFalse($"property '{propertyName}' should not be present");
        }
    }

    public async Task<IAssertMeteredDateForMeasurementPointDocumentDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas.GetSchemaAsync<JsonSchema>("NOTIFYVALIDATEDMEASUREDATA", "0", CancellationToken.None).ConfigureAwait(false);
        var validationResult = IsValid(_document, schema!);
        validationResult.IsValid.Should().BeTrue(string.Join("\n", validationResult.Errors));
        return this;
    }

    private (bool IsValid, IList<string> Errors) IsValid(JsonDocument jsonDocument, JsonSchema schema)
    {
        var errors = new List<string>();
        var result = schema.Evaluate(jsonDocument, new EvaluationOptions() { OutputFormat = OutputFormat.Hierarchical, });
        if (result.IsValid == false)
        {
            errors.Add(FindErrorsForInvalidEvaluation(result));
        }

        return (result.IsValid, errors);
    }

    private string FindErrorsForInvalidEvaluation(EvaluationResults result)
    {
        if (!result.IsValid)
        {
            foreach (var detail in result.Details)
            {
                return FindErrorsForInvalidEvaluation(detail);
            }
        }

        if (!result.HasErrors || result.Errors == null) return string.Empty;

        var propertyName = result.InstanceLocation.ToString();
        foreach (var error in result.Errors)
        {
            return $"{propertyName}: {error}";
        }

        return string.Empty;
    }

    private JsonElement GetTimeSeriesElement(int seriesIndex) =>
        _root.GetProperty("Series").EnumerateArray().ToList()[seriesIndex - 1];

    private JsonElement GetTimeSeriesPeriodElement(int seriesIndex) =>
        _root.GetProperty("Series").EnumerateArray().ToList()[seriesIndex - 1].GetProperty("Period");
}
