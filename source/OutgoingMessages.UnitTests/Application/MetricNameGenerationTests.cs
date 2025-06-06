﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Mapping;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Application;

public class MetricNameGenerationTests
{
    // The following values have to be hardcoded, since they are hardcoded in the terraform defining the dashboard.
    // When a new document type is introduced, the following needs to be inserted into to the terraform script:
    // {
    //     "resourceMetadata": {
    //         "id": "${appi_sharedres_id}"
    //     },
    //     "name": "NotifyValidatedMeasureData{format}",
    //     "aggregationType": 7,
    //     "namespace": "azure.applicationinsights",
    //     "metricVisualization": {
    //         "displayName": "NotifyValidatedMeasureData{format}"
    //     }
    // }
    // replace {format} with the supported formats for the new document type.
    // and include this link to the file: https://github.com/Energinet-DataHub/dh3-infrastructure/blob/main/edi/terraform/main/dashboard-templates/edi_monitor.tpl
    // Look for RejectRequestWholesaleSettlement in the script for an example.
    private static readonly string[] _formats = ["Json", "Xml", "Ebix" ];
    private static readonly string[] _documentMetrics =
    [
        "NotifyAggregatedMeasureData",
        "NotifyAggregatedMeasureDataResponse",
        "RejectRequestAggregatedMeasureDataResponse",
        "NotifyWholesaleServices",
        "NotifyWholesaleServicesResponse",
        "RejectRequestWholesaleSettlementResponse",
        "NotifyValidatedMeasureData",
        "NotifyValidatedMeasureDataResponse",
        "AcknowledgementResponse",
        "ReminderOfMissingMeasureData",
        "RejectRequestMeasurementsResponse",
    ];

    private static readonly DocumentType[] _IsAlwaysAResponseOfARequest =
    [
        DocumentType.RejectRequestWholesaleSettlement,
        DocumentType.RejectRequestAggregatedMeasureData,
        DocumentType.RejectRequestMeasurements,
        DocumentType.Acknowledgement,
    ];

    private static readonly DocumentType[] _IsNeverAResponseOfARequest =
    [
        DocumentType.ReminderOfMissingMeasureData,
    ];

    private static readonly DocumentType[] _isDocumentTypeAnIncomingMessage =
    [
        DocumentType.RequestAggregatedMeasureData,
        DocumentType.RequestWholesaleSettlement,
        DocumentType.RequestMeasurements,
    ];

    private readonly string[] _loggedMessageGenerationMetric = _formats.Select(
        format => _documentMetrics.Select(
                measurement => $"{measurement}{format}"))
        .SelectMany(x => x)
        .ToArray();

    [Fact]
    public void Given_AllMessageGenerationMetricsNames_When_ComparingToCurrentMetricNames_Then_AllMessageGenerationMetricHasACorrespondingMetricAtTheDashboard()
    {
        // Arrange
        var allMetricNamesUsedWhenGeneratingMessages = AllMessageGenerationsMetricsNames();
        var currentlyAddedMetric = _loggedMessageGenerationMetric;

        allMetricNamesUsedWhenGeneratingMessages.Should()
            .BeEquivalentTo(
                currentlyAddedMetric,
                "All documentTypes + documentFormats should have a corresponding visualization in the dashboard, remember to add or remove them to/from the dashboard");
    }

    private static List<string> AllMessageGenerationsMetricsNames()
    {
        var documentTypes = EnumerationType.GetAll<DocumentType>().ToList();
        var documentFormats = EnumerationType.GetAll<DocumentFormat>().ToList();
        var names = new List<string>();

        foreach (var documentType in documentTypes)
        {
            if (_isDocumentTypeAnIncomingMessage.Contains(documentType))
            {
                // Incoming messages are not logged
                continue;
            }

            if (_IsAlwaysAResponseOfARequest.Contains(documentType))
            {
                // {documentType}Response{documentFormat}
                documentFormats.ForEach(documentFormat =>
                    names.Add(MetricNameMapper.MessageGenerationMetricName(
                        documentType,
                        documentFormat,
                        true)));
            }
            else if (_IsNeverAResponseOfARequest.Contains(documentType))
            {
                // {documentType}{documentFormat}
                documentFormats.ForEach(documentFormat =>
                    names.Add(MetricNameMapper.MessageGenerationMetricName(
                        documentType,
                        documentFormat,
                        false)));
            }
            else
            {
                // {documentType}{documentFormat}
                // {documentType}Response{documentFormat}
                names.AddRange(DocumentTypeIsAResponseAndAStandAloneMessage(documentType, documentFormats));
            }
        }

        return names;
    }

    private static IEnumerable<string> DocumentTypeIsAResponseAndAStandAloneMessage(DocumentType documentType, List<DocumentFormat> documentFormats)
    {
        var metric = documentFormats.Select(documentFormat =>
            MetricNameMapper.MessageGenerationMetricName(
                documentType,
                documentFormat,
                true));

        return metric.Concat(
            documentFormats.Select(
                documentFormat =>
                    MetricNameMapper.MessageGenerationMetricName(
                        documentType,
                        documentFormat,
                        false)));
    }
}
