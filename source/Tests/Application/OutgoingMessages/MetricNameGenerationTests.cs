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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Mapping;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.OutgoingMessages;

public class MetricNameGenerationTests
{
    // The following values have to be hardcoded, since they are hardcoded in the terraform defining the dashboard.
    // When a new document type is introduced, the following needs to be inserted into to the terraform script:
    // {
    //     "resourceMetadata": {
    //         "id": "${appi_sharedres_id}"
    //     },
    //     "name": "MeteredDataForMeasurementPoint{format}",
    //     "aggregationType": 7,
    //     "namespace": "azure.applicationinsights",
    //     "metricVisualization": {
    //         "displayName": "MeteredDataForMeasurementPoint{format}"
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
        "RejectRequestAggregatedMeasureData",
        "NotifyWholesaleServices",
        "NotifyWholesaleServicesResponse",
        "RejectRequestWholesaleSettlement",
        "MeteredDataForMeasurementPoint",
        "MeteredDataForMeasurementPointResponse",
    ];

    private static readonly DocumentType[] _doesNotHaveIncomingMessageResponse =
    [
        DocumentType.RejectRequestWholesaleSettlement,
        DocumentType.RejectRequestAggregatedMeasureData
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

        foreach (var documentFormat in documentFormats)
        {
            foreach (var documentType in documentTypes)
            {
                // Most documents are logged as an outgoing message and as a response to a corresponding incoming message
                names.Add(MetricNameMapper.MessageGenerationMetricName(
                    documentType,
                    documentFormat,
                    false));

                // Some documents are only logged as an outgoing message
                if (!_doesNotHaveIncomingMessageResponse.Contains(documentType))
                {
                    names.Add(MetricNameMapper.MessageGenerationMetricName(
                        documentType,
                        documentFormat,
                        true));
                }
            }
        }

        return names;
    }
}
