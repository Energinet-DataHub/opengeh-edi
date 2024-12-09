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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using FluentAssertions;
using Json.Schema;
using Namotion.Reflection;
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
        Assert.True(_root.TryGetProperty("mRID", out _));
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

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        Assert.Equal(expectedTransactionId.Value, FirstTimeSeriesElement().GetProperty("mRID").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointNumber(
        string expectedMeteringPointNumber,
        string expectedSchemeCode)
    {
        Assert.Equal(expectedMeteringPointNumber, FirstTimeSeriesElement().GetProperty("marketEvaluationPoint.mRID").GetProperty("value").GetString());
        Assert.Equal(expectedSchemeCode, FirstTimeSeriesElement().GetProperty("marketEvaluationPoint.mRID").GetProperty("codingScheme").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasMeteringPointType(string expectedMeteringPointType)
    {
        Assert.Equal(expectedMeteringPointType, FirstTimeSeriesElement().GetProperty("marketEvaluationPoint.type").GetProperty("value").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasOriginalTransactionIdReferenceId(
        string? expectedOriginalTransactionIdReferenceId)
    {
        Assert.Equal(expectedOriginalTransactionIdReferenceId, FirstTimeSeriesElement().GetProperty("originalTransactionIDReference_Series.mRID").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasProduct(string expectedProduct)
    {
        Assert.Equal(expectedProduct, FirstTimeSeriesElement().GetProperty("product").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasQuantityMeasureUnit(string expectedQuantityMeasureUnit)
    {
        Assert.Equal(expectedQuantityMeasureUnit, FirstTimeSeriesElement().GetProperty("quantity_Measure_Unit.name").GetProperty("value").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasRegistrationDateTime(string expectedRegistrationDateTime)
    {
        Assert.Equal(expectedRegistrationDateTime, FirstTimeSeriesElement().GetProperty("registration_DateAndOrTime.dateTime").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasResolution(string expectedResolution)
    {
        Assert.Equal(expectedResolution, FirstTimeSeriesPeriodElement().GetProperty("resolution").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasStartedDateTime(string expectedStartedDateTime)
    {
        Assert.Equal(expectedStartedDateTime, FirstTimeSeriesPeriodElement().GetProperty("timeInterval").GetProperty("start").GetProperty("value").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasEndedDateTime(string expectedEndedDateTime)
    {
        Assert.Equal(expectedEndedDateTime, FirstTimeSeriesPeriodElement().GetProperty("timeInterval").GetProperty("end").GetProperty("value").GetString());
        return this;
    }

    public IAssertMeteredDateForMeasurementPointDocumentDocument HasPoints(
        IReadOnlyCollection<(RequiredPointDocumentFields Rpdf, OptionalPointDocumentFields? Opdf)> expectedPoints)
    {
        var points = FirstTimeSeriesPeriodElement().GetProperty("Point").EnumerateArray().ToList();
        var expectedPointsList = expectedPoints.ToList();

        points.Should().HaveCount(expectedPoints.Count);

        for (var i = 0; i < expectedPointsList.Count; i++)
        {
            var (requiredPointDocumentFields, optionalPointDocumentFields) = expectedPointsList[i];
            var actualPoint = points[i];

            actualPoint.GetProperty("position")
                .GetProperty("value")
                .GetInt32()
                .Should()
                .Be(requiredPointDocumentFields.Position);

            if (optionalPointDocumentFields == null)
            {
                continue;
            }

            if (optionalPointDocumentFields.Quality != null)
            {
                actualPoint.GetProperty("quality")
                    .GetProperty("value")
                    .GetString()
                    .Should()
                    .Be(optionalPointDocumentFields.Quality);
            }

            if (optionalPointDocumentFields.Quantity != null)
            {
                actualPoint.GetProperty("quantity").GetDecimal().Should().Be(optionalPointDocumentFields.Quantity);
            }
        }

        return this;
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

    private JsonElement FirstTimeSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }

    private JsonElement FirstTimeSeriesPeriodElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0].GetProperty("Period");
    }
}
