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

using System.Xml;
using Energinet.DataHub.EDI.SubsystemTests.TestData;

namespace Energinet.DataHub.EDI.SubsystemTests.Factories;

internal sealed class RequestAggregatedMeasureXmlBuilder
{
    public static XmlDocument BuildEnergySupplierXmlPayload()
    {
        return BuildEnergySupplierXmlPayload(new Dictionary<string, string>(), string.Empty);
    }

    public static XmlDocument BuildEnergySupplierXmlPayload(Dictionary<string, string> testData)
    {
        return BuildEnergySupplierXmlPayload(testData, string.Empty);
    }

    public static XmlDocument BuildEnergySupplierXmlPayload(string cimXmlNamespaceUri)
    {
        return BuildEnergySupplierXmlPayload(new Dictionary<string, string>(), cimXmlNamespaceUri);
    }

    public static XmlDocument BuildEnergySupplierXmlPayload(Dictionary<string, string> testData, string cimXmlNamespaceUri)
    {
        var defaultData = SynchronousErrorTestData.DefaultEnergySupplierTestData();
        var defaultSeriesData = SynchronousErrorTestData.DefaultEnergySupplierSeriesTestData();
        var elementList = new List<XmlElement>();
        var seriesElementList = new List<XmlElement>();
        var xmlPayload = new XmlDocument();
        xmlPayload.AppendChild(xmlPayload.CreateXmlDeclaration("1.0", "UTF-8", null));
        string xmlNamespace = "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1";

        // BUILD XML HEADER
        var cimNamespaceUri = string.IsNullOrEmpty(cimXmlNamespaceUri)
            ? "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1"
            : cimXmlNamespaceUri;
        XmlElement meteredDataRequest = xmlPayload.CreateElement("cim", "RequestAggregatedMeasureData_MarketDocument", cimNamespaceUri);
        var secondAttribute = xmlPayload.CreateAttribute("xsi", "schemaLocation", "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1 urn-ediel-org-measure-requestaggregatedmeasuredata-0-1.xsd");
        secondAttribute.Value = "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1 urn-ediel-org-measure-requestaggregatedmeasuredata-0-1.xsd";
        meteredDataRequest.Attributes.Append(secondAttribute);
        meteredDataRequest.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");

        // INIT SERIES BLOCK
        XmlElement seriesData = xmlPayload.CreateElement("cim", "Series", "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1");

        // BUILD XML BODY
        if (defaultData != null)
        {
            foreach (var elementName in defaultData.Keys)
            {
                string elementInnerText = testData != null && testData.TryGetValue(elementName, out var value) ? value : defaultData[elementName];
                XmlElement element =
                    CreateAppendableXmlElement(xmlPayload, elementName, xmlNamespace, elementInnerText);
                if (elementName.Equals("cim:sender_MarketParticipant.mRID", StringComparison.Ordinal) || elementName.Equals("cim:receiver_MarketParticipant.mRID", StringComparison.Ordinal)) element.SetAttribute("codingScheme", "A10");

                elementList.Add(element);
            }
        }

        // BUILD XML SERIES BODY
        if (defaultSeriesData != null)
        {
            foreach (var elementName in defaultSeriesData.Keys)
            {
                string elementInnerText = testData != null && testData.TryGetValue(elementName, out var value)
                    ? value
                    : defaultSeriesData[elementName];
                XmlElement element =
                    CreateAppendableXmlElement(xmlPayload, elementName, xmlNamespace, elementInnerText);
                if (elementName.Equals("cim:meteringGridArea_Domain.mRID", StringComparison.Ordinal))
                    element.SetAttribute("codingScheme", "NDK");
                if (elementName.Equals("cim:energySupplier_MarketParticipant.mRID", StringComparison.Ordinal))
                    element.SetAttribute("codingScheme", "A10");

                seriesElementList.Add(element);
            }
        }

        // APPEND ELEMENTS
        foreach (var element in elementList)
        {
            meteredDataRequest.AppendChild(element);
        }

        // APPEND SERIES ELEMENTS
        foreach (var element in seriesElementList)
        {
            seriesData.AppendChild(element);
        }

        meteredDataRequest.AppendChild(seriesData);
        xmlPayload.AppendChild(meteredDataRequest);
        return xmlPayload;
    }

    private static XmlElement CreateAppendableXmlElement(XmlDocument rootElement, string elementName, string xmlNamespace, string innerText)
    {
        XmlElement element = rootElement.CreateElement(elementName, xmlNamespace);
        element.InnerText = innerText;
        return element;
    }
}
