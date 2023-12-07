using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Xml;

namespace EndToEndTesting.Requests;

public class PayloadBuilder
{

    public PayloadBuilder()
    { 
        
    }
    public XmlDocument BuildXmlPayload(Dictionary<string,string> testData, Dictionary<string, string> defaultData, Dictionary<string, string> defaultSeriesData)
    {
        List<XmlElement> elementList = new List<XmlElement>();
        List<XmlElement> seriesElementList = new List<XmlElement>();
        XmlDocument xmlPayload = new XmlDocument();
        xmlPayload.AppendChild(xmlPayload.CreateXmlDeclaration("1.0", "UTF-8", null));
        string xmlNamespace = "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1";
        
        // BUILD XML HEADER
        
        XmlElement meteredDataRequest = xmlPayload.CreateElement("cim","RequestAggregatedMeasureData_MarketDocument", "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1");
        var secondAttribute = xmlPayload.CreateAttribute("xsi", "schemaLocation", "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1 urn-ediel-org-measure-requestaggregatedmeasuredata-0-1.xsd");
        secondAttribute.Value = "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1 urn-ediel-org-measure-requestaggregatedmeasuredata-0-1.xsd";
       
        meteredDataRequest.Attributes.Append(secondAttribute);
        meteredDataRequest.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
        
        // INIT SERIES BLOCK
        XmlElement seriesData = xmlPayload.CreateElement("cim", "Series", "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1");

        // BUILD XML BODY
        foreach (var elementName in defaultData.Keys)
        {
            string elementInnerText = testData.ContainsKey(elementName) ? testData[elementName] : defaultData[elementName];
            XmlElement element = CreateAppendableXmlElement(xmlPayload, elementName, xmlNamespace, elementInnerText);
            if (elementName.Equals("cim:sender_MarketParticipant.mRID") || elementName.Equals("cim:receiver_MarketParticipant.mRID")) element.SetAttribute("codingScheme", "A10");
                
            elementList.Add(element);
        }
        
        // BUILD XML SERIES BODY
        foreach (var elementName in defaultSeriesData.Keys)
        {
            string elementInnerText = testData.ContainsKey(elementName) ? testData[elementName] : defaultSeriesData[elementName];
            XmlElement element = CreateAppendableXmlElement(xmlPayload, elementName, xmlNamespace, elementInnerText);
            if (elementName.Equals("cim:meteringGridArea_Domain.mRID")) element.SetAttribute("codingScheme", "NDK"); 
            if (elementName.Equals("cim:energySupplier_MarketParticipant.mRID")) element.SetAttribute("codingScheme", "A10");
                
            seriesElementList.Add(element);
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

    private XmlElement CreateAppendableXmlElement(XmlDocument rootElement ,string elementName, string xmlNamespace,string innerText)
    {
        XmlElement element = rootElement.CreateElement(elementName, xmlNamespace);
        element.InnerText = innerText;
        return element;
    }
    
    
}