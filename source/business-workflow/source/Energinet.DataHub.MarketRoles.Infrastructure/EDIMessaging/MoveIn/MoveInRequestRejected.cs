// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#pragma warning disable // Auto-generated code
namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.MoveIn
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:ebix:org:ChangeOfSupplier:0:1")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn:ebix:org:ChangeOfSupplier:0:1", IsNullable = false)]
    public partial class RejectRequestChangeOfSupplier_MarketDocument
    {
        private string mRIDField;

        private ushort typeField;

        private string processprocessTypeField;

        private string businessSectortypeField;

        private RejectRequestChangeOfSupplier_MarketDocumentSender_MarketParticipantmRID
            sender_MarketParticipantmRIDField;

        private string sender_MarketParticipantmarketRoletypeField;

        private RejectRequestChangeOfSupplier_MarketDocumentReceiver_MarketParticipantmRID
            receiver_MarketParticipantmRIDField;

        private string receiver_MarketParticipantmarketRoletypeField;

        private System.DateTime createdDateTimeField;

        private RejectRequestChangeOfSupplier_MarketDocumentReason reasonField;

        private RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecord mktActivityRecordField;

        /// <remarks/>
        public string mRID
        {
            get
            {
                return mRIDField;
            }
            set
            {
                mRIDField = value;
            }
        }

        /// <remarks/>
        public ushort type
        {
            get
            {
                return typeField;
            }
            set
            {
                typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("process.processType")]
        public string processprocessType
        {
            get
            {
                return processprocessTypeField;
            }
            set
            {
                processprocessTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("businessSector.type")]
        public string businessSectortype
        {
            get
            {
                return businessSectortypeField;
            }
            set
            {
                businessSectortypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("sender_MarketParticipant.mRID")]
        public RejectRequestChangeOfSupplier_MarketDocumentSender_MarketParticipantmRID sender_MarketParticipantmRID
        {
            get
            {
                return sender_MarketParticipantmRIDField;
            }
            set
            {
                sender_MarketParticipantmRIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("sender_MarketParticipant.marketRole.type")]
        public string sender_MarketParticipantmarketRoletype
        {
            get
            {
                return sender_MarketParticipantmarketRoletypeField;
            }
            set
            {
                sender_MarketParticipantmarketRoletypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("receiver_MarketParticipant.mRID")]
        public RejectRequestChangeOfSupplier_MarketDocumentReceiver_MarketParticipantmRID receiver_MarketParticipantmRID
        {
            get
            {
                return receiver_MarketParticipantmRIDField;
            }
            set
            {
                receiver_MarketParticipantmRIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("receiver_MarketParticipant.marketRole.type")]
        public string receiver_MarketParticipantmarketRoletype
        {
            get
            {
                return receiver_MarketParticipantmarketRoletypeField;
            }
            set
            {
                receiver_MarketParticipantmarketRoletypeField = value;
            }
        }

        /// <remarks/>
        public System.DateTime createdDateTime
        {
            get
            {
                return createdDateTimeField;
            }
            set
            {
                createdDateTimeField = value;
            }
        }

        /// <remarks/>
        public RejectRequestChangeOfSupplier_MarketDocumentReason Reason
        {
            get
            {
                return reasonField;
            }
            set
            {
                reasonField = value;
            }
        }

        /// <remarks/>
        public RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecord MktActivityRecord
        {
            get
            {
                return mktActivityRecordField;
            }
            set
            {
                mktActivityRecordField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:ebix:org:ChangeOfSupplier:0:1")]
    public partial class RejectRequestChangeOfSupplier_MarketDocumentSender_MarketParticipantmRID
    {

        private string codingSchemeField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string codingScheme
        {
            get
            {
                return codingSchemeField;
            }
            set
            {
                codingSchemeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return valueField;
            }
            set
            {
                valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:ebix:org:ChangeOfSupplier:0:1")]
    public partial class RejectRequestChangeOfSupplier_MarketDocumentReceiver_MarketParticipantmRID
    {

        private string codingSchemeField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string codingScheme
        {
            get
            {
                return codingSchemeField;
            }
            set
            {
                codingSchemeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return valueField;
            }
            set
            {
                valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:ebix:org:ChangeOfSupplier:0:1")]
    public partial class RejectRequestChangeOfSupplier_MarketDocumentReason
    {

        private string codeField;

        private string textField;

        /// <remarks/>
        public string code
        {
            get
            {
                return codeField;
            }
            set
            {
                codeField = value;
            }
        }

        /// <remarks/>
        public string text
        {
            get
            {
                return textField;
            }
            set
            {
                textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:ebix:org:ChangeOfSupplier:0:1")]
    public partial class RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecord
    {

        private string mRIDField;

        private RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecordReason[] reasonField;

        private string marketEvaluationPointmRIDField;

        private string originalTransactionIDReference_MktActivityRecordmRIDField;

        private string businessProcessReference_MktActivityRecordmRIDField;

        private System.DateTime start_DateAndOrTimedateField;

        /// <remarks/>
        public string mRID
        {
            get
            {
                return mRIDField;
            }
            set
            {
                mRIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Reason")]
        public RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecordReason[] Reason
        {
            get
            {
                return reasonField;
            }
            set
            {
                reasonField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("marketEvaluationPoint.mRID")]
        public string marketEvaluationPointmRID
        {
            get
            {
                return marketEvaluationPointmRIDField;
            }
            set
            {
                marketEvaluationPointmRIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("originalTransactionIDReference_MktActivityRecord.mRID")]
        public string originalTransactionIDReference_MktActivityRecordmRID
        {
            get
            {
                return originalTransactionIDReference_MktActivityRecordmRIDField;
            }
            set
            {
                originalTransactionIDReference_MktActivityRecordmRIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("businessProcessReference_MktActivityRecord.mRID")]
        public string businessProcessReference_MktActivityRecordmRID
        {
            get
            {
                return businessProcessReference_MktActivityRecordmRIDField;
            }
            set
            {
                businessProcessReference_MktActivityRecordmRIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("start_DateAndOrTime.date", DataType = "date")]
        public System.DateTime start_DateAndOrTimedate
        {
            get
            {
                return start_DateAndOrTimedateField;
            }
            set
            {
                start_DateAndOrTimedateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:ebix:org:ChangeOfSupplier:0:1")]
    public partial class RejectRequestChangeOfSupplier_MarketDocumentMktActivityRecordReason
    {

        private string codeField;

        private string textField;

        /// <remarks/>
        public string code
        {
            get
            {
                return codeField;
            }
            set
            {
                codeField = value;
            }
        }

        /// <remarks/>
        public string text
        {
            get
            {
                return textField;
            }
            set
            {
                textField = value;
            }
        }
    }
}
#pragma warning restore
