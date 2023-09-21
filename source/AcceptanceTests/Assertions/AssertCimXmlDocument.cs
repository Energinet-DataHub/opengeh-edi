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

using System.Xml.Linq;

namespace Energinet.DataHub.EDI.AcceptanceTests.Assertions;

public class AssertCimXmlDocument
{
    private readonly XDocument _document;

    public AssertCimXmlDocument(Stream body)
    {
        _document = XDocument.Load(body, LoadOptions.None);
    }

    public void IsProductionResultFor(string expectedGridArea)
    {
        var series = _document.Root?.Elements().Where(e => e.Name.LocalName.Equals("Series", StringComparison.Ordinal)).ToList();
        var marketEvaluationPointType = series!.Elements()
            .Single(e => e.Name.LocalName.Equals("marketEvaluationPoint.type", StringComparison.OrdinalIgnoreCase))
            .Value;
        var gridArea = series!.Elements()
            .Single(e => e.Name.LocalName.Equals("meteringGridArea_Domain.mRID", StringComparison.OrdinalIgnoreCase))
            .Value;
        Assert.Equal("E18", marketEvaluationPointType);
        var documentType = _document.Root?.Name.LocalName;
        Assert.Equal(expectedGridArea, gridArea);
        Assert.Equal("NotifyAggregatedMeasureData_MarketDocument", documentType);
    }
}
