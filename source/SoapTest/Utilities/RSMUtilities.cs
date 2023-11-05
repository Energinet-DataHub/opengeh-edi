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
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SoapTest.Exceptions;
using SoapTest.RSM.Extensions;

namespace SoapTest.Utilities
{
    /// <summary>
    /// RSM document utilities
    /// </summary>
    public static class RSMUtilities
    {
#nullable enable
        /// <summary>
        /// Get corresponding class from RSM document
        /// </summary>
        /// <param name="document">RSM document</param>
        /// <returns>RSM class or null if unknown type</returns>
        public static Type? GetRSMClassFromDocument(XmlDocument document)
        {
            var rootTag = GetRootElementName(document);
            var schemeVer = GetSchemeVersion(document);
            return schemeVer switch
            {
                SchemeVersion.V3 => GetRSMClassFromDocumentV3(rootTag),
                _ => GetCustomRsmClassFromDocument(rootTag),
            };
        }
#nullable disable

        /// <summary>
        /// Convert xml document to RSM class
        /// </summary>
        /// <param name="document">Document to convert</param>
        /// <returns>RSM class</returns>
        public static RsmDocument ConvertToRSM(XmlDocument document)
        {
            var rsmType = GetRSMClassFromDocument(document) ?? throw new RsmException("Unknown RSM document: " + document.OuterXml);

            var serialiser = new XmlSerializer(rsmType);
            using var reader = new XmlNodeReader(document);

            var rsmDoc = serialiser.Deserialize(reader);

            return (RsmDocument)rsmDoc;
        }

        public static XmlDocument ConvertToXmlDocument(this RsmDocument document)
        {
            return RSMUtilities.ConvertToXmlDocument((object)document);
        }

        public static XmlDocument ConvertToXmlDocument(object document)
        {
            var serializer = new XmlSerializer(document.GetType());
            var sb = new StringBuilder();

            // using custom stringwriter class to be able to set encoding.
            // MS' StringWriter will always use UTF-16, disregarding the encoding you may set.
            using (var stringWriter = new StringWriterWithEncoding(sb, new CultureInfo("da-DK"), new UTF8Encoding(false)))
            {
                serializer.Serialize(stringWriter, document);
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(sb.ToString());
            return xmlDocument;
        }

#nullable enable
        /// <summary>
        /// Converts a string to a XML document.
        /// </summary>
        /// <param name="rsm">The RSM.</param>
        /// <returns>XmlDocument</returns>
        public static XmlDocument? ConvertToXmlDocument(string rsm)
        {
            if (string.IsNullOrEmpty(rsm))
            { return null; }
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(rsm);
            return xmlDocument;
        }

#nullable disable
        /// <summary>
        /// Convert xml document to string
        /// </summary>
        /// <param name="xmlDocument">Document to convert</param>
        /// <returns>XmlDocument</returns>
        public static string ConvertToString(XmlDocument xmlDocument)
        {
            var serializer = new XmlSerializer(xmlDocument.GetType());
            var sb = new StringBuilder();

            // using custom stringwriter class to be able to set encoding.
            // MS' StringWriter will always use UTF-16, disregarding the encoding you may set.
            using (var stringWriter = new StringWriterWithEncoding(sb, new CultureInfo("da-DK"), new UTF8Encoding(false)))
            {
                serializer.Serialize(stringWriter, xmlDocument);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert id to string which can be used in a RSM document. Strips all '-' to get length below 35
        /// </summary>
        /// <param name="id">Id to convert to string</param>
        /// <returns>Id as string</returns>
        public static string ConvertIdToString(Guid id)
        {
            return id.ToString("N");
        }

        /// <summary>
        /// Gets the version of the Scheme
        /// </summary>
        /// <param name="rsmXmlDoc"></param>
        /// <returns>Returns the version of the schema</returns>
        public static SchemeVersion GetSchemeVersion(XmlDocument rsmXmlDoc)
        {
            var ret = SchemeVersion.Unknown;
            try
            {
                var tag = rsmXmlDoc.DocumentElement.NamespaceURI.Split(':');
                var version = tag[tag.Length - 1];
                ret = SchemeVersionUtilities.ConvertToSchemeVersion(version);
            }
            finally
            { }

            return ret;
        }

#nullable enable
        /// <summary>
        /// Converts the source rsm xml doc to the targetSchemeVersion
        /// </summary>
        /// <param name="sourceRsmXmlDoc"></param>
        /// <param name="targetSchemeVersion"></param>
        /// <returns>A new xmldocument in the right version</returns>
        public static RsmDocument? ConvertToRSM(XmlDocument sourceRsmXmlDoc, SchemeVersion targetSchemeVersion)
        {
            var sourceSchemeVer = GetSchemeVersion(sourceRsmXmlDoc);
            if (sourceSchemeVer == targetSchemeVersion)
                return RSMUtilities.ConvertToRSM(sourceRsmXmlDoc);
            var rsmType = GetRSMClassFromDocument(sourceRsmXmlDoc, targetSchemeVersion);
            if (rsmType == null)
                return null;
            var schemeConvertedXmlDoc = ConvertXmlSchemeVersion(sourceRsmXmlDoc, sourceSchemeVer, targetSchemeVersion);
            var serialiser = new XmlSerializer(rsmType);
            using var reader = new XmlNodeReader(schemeConvertedXmlDoc);
            var rsmDoc = serialiser.Deserialize(reader);
            var res = (RsmDocument?)rsmDoc;
            return res;
        }

        /// <summary>
        /// Get corresponding class from RSM document
        /// </summary>
        /// <param name="document">RSM document</param>
        /// <param name="schemeVer">The shemeversion of the RSM document</param>
        /// <returns>RSM class or null if unknown type</returns>
        public static Type? GetRSMClassFromDocument(XmlDocument document, SchemeVersion schemeVer)
        {
            var rootTag = GetRootElementName(document);
            return schemeVer switch
            {
                SchemeVersion.V3 => GetRSMClassFromDocumentV3(rootTag),
                _ => GetCustomRsmClassFromDocument(rootTag),
            };
        }

        /// <summary>
        /// Get corresponding class from RSM document of version V3
        /// </summary>
        /// <param name="documentTypeName">RSM document</param>
        /// <returns>RSM class or null if unknown type</returns>
        private static Type? GetRSMClassFromDocumentV3(string documentTypeName)
        {
            switch (documentTypeName)
            {
                case "DK_ConfirmChangeOfSupplier":
                    return typeof(RSM.RSM001.V3.Response.DK_ConfirmChangeOfSupplierType);

                case "DK_RejectChangeOfSupplier":
                    return typeof(RSM.RSM001.V3.Reject.DK_RejectChangeOfSupplierType);

                case "DK_RequestChangeOfSupplier":
                    return typeof(RSM.RSM001.V3.Request.DK_RequestChangeOfSupplierType);

                case "DK_ConfirmCancelChangeOfSupplier":
                    return typeof(RSM.RSM002.V3.Confirm.DK_ConfirmCancelChangeOfSupplierType);

                case "DK_RejectCancelChangeOfSupplier":
                    return typeof(RSM.RSM002.V3.Reject.DK_RejectCancelChangeOfSupplierType);

                case "DK_RequestCancelChangeOfSupplier":
                    return typeof(RSM.RSM002.V3.Request.DK_RequestCancelChangeOfSupplierType);

                case "DK_ConfirmReallocateChangeOfSupplier":
                    return typeof(RSM.RSM003.V3.Confirm.DK_ConfirmReallocateChangeOfSupplierType);

                case "DK_RejectReallocateChangeOfSupplier":
                    return typeof(RSM.RSM003.V3.Reject.DK_RejectReallocateChangeOfSupplierType);

                case "DK_RequestReallocateChangeOfSupplier":
                    return typeof(RSM.RSM003.V3.Request.DK_RequestReallocateChangeOfSupplierType);

                case "DK_NotifyChangeOfSupplier":
                    return typeof(RSM.RSM004.V3.Notify.DK_NotifyChangeOfSupplierType);

                case "DK_ConfirmEndOfSupply":
                    return typeof(RSM.RSM005.V3.Confirm.DK_ConfirmEndOfSupplyType);

                case "DK_RejectEndOfSupply":
                    return typeof(RSM.RSM005.V3.Reject.DK_RejectEndOfSupplyType);

                case "DK_RequestEndOfSupply":
                    return typeof(RSM.RSM005.V3.Request.DK_RequestEndOfSupplyType);

                case "DK_RejectQueryMasterData":
                    return typeof(RSM.RSM006.V3.Reject.DK_RejectQueryMasterDataType);

                case "DK_QueryMasterData":
                    return typeof(RSM.RSM006.V3.Request.DK_QueryMasterDataType);

                case "DK_ConfirmCancelEndOfSupply":
                    return typeof(RSM.RSM008.V3.Confirm.DK_ConfirmCancelEndOfSupplyType);

                case "DK_RejectCancelEndOfSupply":
                    return typeof(RSM.RSM008.V3.Reject.DK_RejectCancelEndOfSupplyType);

                case "DK_RequestCancelEndOfSupply":
                    return typeof(RSM.RSM008.V3.Request.DK_RequestCancelEndOfSupplyType);

                case "DK_Acknowledgement":
                    return typeof(RSM.RSM009.V3.Response.DK_AcknowledgementType);

                case "DK_NotifyVolumes":
                    return typeof(RSM.RSM010.V3.Notify.DK_NotifyVolumesType);

                case "DK_NonContinuousMetering":
                    return typeof(RSM.RSM011.V3.Notify.DK_NonContinuousMeteringType);

                case "DK_MeteredDataTimeSeries":
                    return typeof(RSM.RSM012.V3.Notify.DK_MeteredDataTimeSeriesType);

                case "DK_LoadProfile":
                    return typeof(RSM.RSM013.V3.DK_LoadProfileType);

                case "DK_AggregatedMeteredDataTimeSeries":
                    return typeof(RSM.RSM014.V3.Notify.DK_AggregatedMeteredDataTimeSeriesType);

                case "DK_RejectRequestMeteredDataValidated":
                    return typeof(RSM.RSM015.V3.Reject.DK_RejectRequestMeteredDataValidatedType);

                case "DK_RequestMeteredDataValidated":
                    return typeof(RSM.RSM015.V3.Request.DK_RequestMeteredDataValidatedType);

                case "DK_RejectRequestMeteredDataAggregated":
                    return typeof(RSM.RSM016.V3.Reject.DK_RejectRequestMeteredDataAggregatedType);

                case "DK_RequestMeteredDataAggregated":
                    return typeof(RSM.RSM016.V3.Request.DK_RequestMeteredDataAggregatedType);

                case "DK_RejectAggregatedBillingInformation":
                    return typeof(RSM.RSM017.V3.Reject.DK_RejectAggregatedBillingInformationType);

                case "DK_RequestAggregatedBillingInformation":
                    return typeof(RSM.RSM017.V3.Request.DK_RequestAggregatedBillingInformationType);

                case "DK_NotifyMissingData":
                    return typeof(RSM.RSM018.V3.Notify.DK_NotifyMissingDataType);

                case "DK_NotifyAggregatedWholesaleServices":
                    return typeof(RSM.RSM019.V3.Notify.DK_NotifyAggregatedWholesaleServicesType);

                case "DK_ConfirmServices":
                    return typeof(RSM.RSM020.V3.Confirm.DK_ConfirmServicesType);

                case "DK_RejectServices":
                    return typeof(RSM.RSM020.V3.Reject.DK_RejectServicesType);

                case "DK_RequestServices":
                    return typeof(RSM.RSM020.V3.Request.DK_RequestServicesType);

                case "DK_ConfirmUpdateMasterDataMeteringPoint":
                    return typeof(RSM.RSM021.V3.Confirm.DK_ConfirmUpdateMasterDataMeteringPointType);

                case "DK_RejectUpdateMasterDataMeteringPoint":
                    return typeof(RSM.RSM021.V3.Reject.DK_RejectUpdateMasterDataMeteringPointType);

                case "DK_RequestUpdateMasterDataMeteringPoint":
                    return typeof(RSM.RSM021.V3.Request.DK_RequestUpdateMasterDataMeteringPointType);

                case "DK_NotifyMasterDataMeteringPoint":
                    return typeof(RSM.RSM022.V3.Notify.DK_NotifyMasterDataMeteringPointType);

                case "DK_ResponseMasterDataMeteringPoint":
                    return typeof(RSM.RSM023.V3.Response.DK_ResponseMasterDataMeteringPointType);

                case "DK_ConfirmCancellation":
                    return typeof(RSM.RSM024.V3.Confirm.DK_ConfirmCancellationType);

                case "DK_RejectCancellation":
                    return typeof(RSM.RSM024.V3.Reject.DK_RejectCancellationType);

                case "DK_RequestCancellation":
                    return typeof(RSM.RSM024.V3.Request.DK_RequestCancellationType);

                case "DK_NotifyCancellation":
                    return typeof(RSM.RSM025.V3.Notify.DK_NotifyCancellationType);

                case "DK_ConfirmUpdateMasterDataConsumer":
                    return typeof(RSM.RSM027.V3.Response.DK_ConfirmUpdateMasterDataConsumerType);

                case "DK_RejectUpdateMasterDataConsumer":
                    return typeof(RSM.RSM027.V3.Reject.DK_RejectUpdateMasterDataConsumerType);

                case "DK_RequestUpdateMasterDataConsumer":
                    return typeof(RSM.RSM027.V3.Request.DK_RequestUpdateMasterDataConsumerType);

                case "DK_NotifyMasterDataConsumer":
                    return typeof(RSM.RSM028.V3.Notify.DK_NotifyMasterDataConsumerType);

                case "DK_ResponseMasterDataConsumer":
                    return typeof(RSM.RSM029.V3.Response.DK_ResponseMasterDataConsumerType);

                case "DK_ConfirmUpdateMasterDataCharge":
                    return typeof(RSM.RSM030.V3.Confirm.DK_ConfirmUpdateMasterDataChargeType);

                case "DK_RejectUpdateMasterDataCharge":
                    return typeof(RSM.RSM030.V3.Reject.DK_RejectUpdateMasterDataChargeType);

                case "DK_RequestUpdateMasterDataCharge":
                    return typeof(RSM.RSM030.V3.Request.DK_RequestUpdateMasterDataChargeType);

                case "DK_NotifyMasterDataCharge":
                    return typeof(RSM.RSM031.V3.Notify.DK_NotifyMasterDataChargeType);

                case "DK_QueryMasterDataCharge":
                    return typeof(RSM.RSM032.V3.Request.DK_QueryMasterDataChargeType);

                case "DK_RejectMasterDataCharge":
                    return typeof(RSM.RSM032.V3.Reject.DK_RejectMasterDataChargeType);

                case "DK_ResponseMasterDataCharge":
                    return typeof(RSM.RSM032.V3.Response.DK_ResponseMasterDataChargeType);

                case "DK_ConfirmUpdateChargeInformation":
                    return typeof(RSM.RSM033.V3.Confirm.DK_ConfirmUpdateChargeInformationType);

                case "DK_RejectUpdateChargeInformation":
                    return typeof(RSM.RSM033.V3.Reject.DK_RejectUpdateChargeInformationType);

                case "DK_RequestUpdateChargeInformation":
                    return typeof(RSM.RSM033.V3.Request.DK_RequestUpdateChargeInformationType);

                case "DK_NotifyChargeInformation":
                    return typeof(RSM.RSM034.V3.Notify.DK_NotifyChargeInformationType);

                case "DK_QueryChargeInformation":
                    return typeof(RSM.RSM035.V3.Request.DK_QueryChargeInformationType);

                case "DK_RejectQueryChargeInformation":
                    return typeof(RSM.RSM035.V3.Reject.DK_RejectQueryChargeInformationType);

                case "DK_ResponseQueryChargeInformation":
                    return typeof(RSM.RSM035.V3.Response.DK_ResponseQueryChargeInformationType);

                // Our own fault message
                case "RsmFaultMessage":
                    return typeof(RsmFaultMessage);
                // Our own confirm message
                case "RsmConfirmMessage":
                    return typeof(RsmConfirmMessage);

                default:
                    break;
            }

            return null;
        }

        /// <summary>
        /// Get custom class
        /// </summary>
        /// <param name="documentTypeName"></param>
        /// <returns>A custom RSM class</returns>
        private static Type? GetCustomRsmClassFromDocument(string documentTypeName)
        {
            switch (documentTypeName)
            {
                // Our own fault message
                case "RsmFaultMessage":
                    return typeof(RsmFaultMessage);
                // Our own confirm message
                case "RsmConfirmMessage":
                    return typeof(RsmConfirmMessage);

                default:
                    break;
            }

            return null;
        }
#nullable disable

        private static XmlDocument ConvertXmlSchemeVersion(XmlDocument srcRsmDoc, SchemeVersion srcSchVer, SchemeVersion tarSchVer)
        {
            var srcVer = srcSchVer == SchemeVersion.V3 ? ":v3" : ":v2";
            var tarVer = tarSchVer == SchemeVersion.V3 ? ":v3" : ":v2";
            var srcDocXmlNs = srcRsmDoc.DocumentElement.NamespaceURI;
            var tarDocXmlNs = srcDocXmlNs.Replace(srcVer, tarVer);
            var srcDocAsStr = RSMUtilities.ConvertToString(srcRsmDoc);
            var tarDocAsStr = srcDocAsStr.Replace(srcDocXmlNs, tarDocXmlNs);
            var tarXmlDoc = RSMUtilities.ConvertToXmlDocument(tarDocAsStr);
            return tarXmlDoc;
        }

        private static string GetRootElementName(XmlDocument doc)
        {
            var tag = doc.DocumentElement.Name.Split(':');
            var rootElmName = tag[tag.Length - 1];
            return rootElmName;
        }
    }
}
