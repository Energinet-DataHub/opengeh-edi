﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix {


    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="urn:www:datahub:dk:b2b:service:v01", ConfigurationName="DataHubService.marketMessagingB2BServiceV01PortType")]
    public interface marketMessagingB2BServiceV01PortType {

        // CODEGEN: Generating message contract since the wrapper namespace (urn:www:datahub:dk:b2b:v01) of message sendMessageRequest does not match the default value (urn:www:datahub:dk:b2b:service:v01)
        [System.ServiceModel.OperationContractAttribute(Action="sendMessage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageResponse sendMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest request);

        [System.ServiceModel.OperationContractAttribute(Action="sendMessage", ReplyAction="*")]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageResponse> sendMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest request);

        // CODEGEN: Generating message contract since the wrapper namespace (urn:www:datahub:dk:b2b:v01) of message getMessageRequest does not match the default value (urn:www:datahub:dk:b2b:service:v01)
        [System.ServiceModel.OperationContractAttribute(Action="getMessage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageResponse getMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest request);

        [System.ServiceModel.OperationContractAttribute(Action="getMessage", ReplyAction="*")]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageResponse> getMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest request);

        // CODEGEN: Generating message contract since the wrapper namespace (urn:www:datahub:dk:b2b:v01) of message getMessageIdsRequest does not match the default value (urn:www:datahub:dk:b2b:service:v01)
        [System.ServiceModel.OperationContractAttribute(Action="getMessageIds", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsResponse getMessageIds(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest request);

        [System.ServiceModel.OperationContractAttribute(Action="getMessageIds", ReplyAction="*")]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsResponse> getMessageIdsAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest request);

        [System.ServiceModel.OperationContractAttribute(Action="queryData", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataResponse queryData(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataRequest request);

        // CODEGEN: Generating message contract since the operation has multiple return values.
        [System.ServiceModel.OperationContractAttribute(Action="queryData", ReplyAction="*")]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataResponse> queryDataAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataRequest request);

        // CODEGEN: Generating message contract since the wrapper namespace (urn:www:datahub:dk:b2b:v01) of message peekMessageRequest does not match the default value (urn:www:datahub:dk:b2b:service:v01)
        [System.ServiceModel.OperationContractAttribute(Action="peekMessage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageResponse peekMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest request);

        [System.ServiceModel.OperationContractAttribute(Action="peekMessage", ReplyAction="*")]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageResponse> peekMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest request);

        // CODEGEN: Generating message contract since the wrapper namespace (urn:www:datahub:dk:b2b:v01) of message dequeueMessageRequest does not match the default value (urn:www:datahub:dk:b2b:service:v01)
        [System.ServiceModel.OperationContractAttribute(Action="dequeueMessage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageResponse dequeueMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest request);

        [System.ServiceModel.OperationContractAttribute(Action="dequeueMessage", ReplyAction="*")]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageResponse> dequeueMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest request);
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:www:datahub:dk:b2b:v01")]
    public partial class MessageContainer_Type : object, System.ComponentModel.INotifyPropertyChanged {

        private string messageReferenceField;

        private string documentTypeField;

        private MessageType_Type messageTypeField;

        private System.Xml.XmlElement payloadField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string MessageReference {
            get {
                return this.messageReferenceField;
            }
            set {
                this.messageReferenceField = value;
                this.RaisePropertyChanged("MessageReference");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string DocumentType {
            get {
                return this.documentTypeField;
            }
            set {
                this.documentTypeField = value;
                this.RaisePropertyChanged("DocumentType");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public MessageType_Type MessageType {
            get {
                return this.messageTypeField;
            }
            set {
                this.messageTypeField = value;
                this.RaisePropertyChanged("MessageType");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public System.Xml.XmlElement Payload {
            get {
                return this.payloadField;
            }
            set {
                this.payloadField = value;
                this.RaisePropertyChanged("Payload");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:www:datahub:dk:b2b:v01")]
    public enum MessageType_Type {

        /// <remarks/>
        XML,

        /// <remarks/>
        EDIFACT,
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="SendMessageRequest", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class sendMessageRequest {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer;

        public sendMessageRequest() {
        }

        public sendMessageRequest(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer) {
            this.MessageContainer = MessageContainer;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="SendMessageResponse", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class sendMessageResponse {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public string MessageId;

        public sendMessageResponse() {
        }

        public sendMessageResponse(string MessageId) {
            this.MessageId = MessageId;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetMessageRequest", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class getMessageRequest {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public string MessageId;

        public getMessageRequest() {
        }

        public getMessageRequest(string MessageId) {
            this.MessageId = MessageId;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetMessageResponse", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class getMessageResponse {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer;

        public getMessageResponse() {
        }

        public getMessageResponse(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer) {
            this.MessageContainer = MessageContainer;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetMessageIdsRequest", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class getMessageIdsRequest {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public System.DateTime From;

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=1)]
        public System.DateTime To;

        public getMessageIdsRequest() {
        }

        public getMessageIdsRequest(System.DateTime From, System.DateTime To) {
            this.From = From;
            this.To = To;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetMessageIdsResponse", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class getMessageIdsResponse {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        [System.Xml.Serialization.XmlArrayItemAttribute("MessageId", IsNullable=false)]
        public string[] MessageIdList;

        public getMessageIdsResponse() {
        }

        public getMessageIdsResponse(string[] MessageIdList) {
            this.MessageIdList = MessageIdList;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="QueryDataRequest", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class queryDataRequest {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="", Order=0)]
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any;

        public queryDataRequest() {
        }

        public queryDataRequest(System.Xml.XmlElement[] Any) {
            this.Any = Any;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.MessageContractAttribute(WrapperName="QueryDataResponse", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class queryDataResponse {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="", Order=0)]
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any;

        public queryDataResponse() {
        }

        public queryDataResponse(System.Xml.XmlElement[] Any) {
            this.Any = Any;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="PeekMessageRequest", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class peekMessageRequest {

        public peekMessageRequest() {
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="PeekMessageResponse", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class peekMessageResponse {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer;

        public peekMessageResponse() {
        }

        public peekMessageResponse(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer) {
            this.MessageContainer = MessageContainer;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="DequeueMessageRequest", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class dequeueMessageRequest {

        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="urn:www:datahub:dk:b2b:v01", Order=0)]
        public string MessageId;

        public dequeueMessageRequest() {
        }

        public dequeueMessageRequest(string MessageId) {
            this.MessageId = MessageId;
        }
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="DequeueMessageResponse", WrapperNamespace="urn:www:datahub:dk:b2b:v01", IsWrapped=true)]
    public partial class dequeueMessageResponse {

        public dequeueMessageResponse() {
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface marketMessagingB2BServiceV01PortTypeChannel : Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType, System.ServiceModel.IClientChannel {
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class marketMessagingB2BServiceV01PortTypeClient : System.ServiceModel.ClientBase<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType>, Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType {

        public marketMessagingB2BServiceV01PortTypeClient() {
        }

        public marketMessagingB2BServiceV01PortTypeClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
                base(binding, remoteAddress) {
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageResponse Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.sendMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest request) {
            return base.Channel.sendMessage(request);
        }

        public string sendMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest();
            inValue.MessageContainer = MessageContainer;
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageResponse retVal = ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).sendMessage(inValue);
            return retVal.MessageId;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageResponse> Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.sendMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest request) {
            return base.Channel.sendMessageAsync(request);
        }

        public System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageResponse> sendMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type MessageContainer) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.sendMessageRequest();
            inValue.MessageContainer = MessageContainer;
            return ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).sendMessageAsync(inValue);
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageResponse Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.getMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest request) {
            return base.Channel.getMessage(request);
        }

        public Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type getMessage(string MessageId) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest();
            inValue.MessageId = MessageId;
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageResponse retVal = ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).getMessage(inValue);
            return retVal.MessageContainer;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageResponse> Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.getMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest request) {
            return base.Channel.getMessageAsync(request);
        }

        public System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageResponse> getMessageAsync(string MessageId) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageRequest();
            inValue.MessageId = MessageId;
            return ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).getMessageAsync(inValue);
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsResponse Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.getMessageIds(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest request) {
            return base.Channel.getMessageIds(request);
        }

        public string[] getMessageIds(System.DateTime From, System.DateTime To) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest();
            inValue.From = From;
            inValue.To = To;
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsResponse retVal = ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).getMessageIds(inValue);
            return retVal.MessageIdList;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsResponse> Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.getMessageIdsAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest request) {
            return base.Channel.getMessageIdsAsync(request);
        }

        public System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsResponse> getMessageIdsAsync(System.DateTime From, System.DateTime To) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.getMessageIdsRequest();
            inValue.From = From;
            inValue.To = To;
            return ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).getMessageIdsAsync(inValue);
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataResponse Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.queryData(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataRequest request) {
            return base.Channel.queryData(request);
        }

        public void queryData(ref System.Xml.XmlElement[] Any) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataRequest();
            inValue.Any = Any;
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataResponse retVal = ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).queryData(inValue);
            Any = retVal.Any;
        }

        public System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataResponse> queryDataAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.queryDataRequest request) {
            return base.Channel.queryDataAsync(request);
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageResponse Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.peekMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest request) {
            return base.Channel.peekMessage(request);
        }

        public Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.MessageContainer_Type peekMessage() {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest();
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageResponse retVal = ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).peekMessage(inValue);
            return retVal.MessageContainer;
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageResponse> Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.peekMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest request) {
            return base.Channel.peekMessageAsync(request);
        }

        public System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageResponse> peekMessageAsync() {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.peekMessageRequest();
            return ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).peekMessageAsync(inValue);
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageResponse Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.dequeueMessage(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest request) {
            return base.Channel.dequeueMessage(request);
        }

        public void dequeueMessage(string MessageId) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest();
            inValue.MessageId = MessageId;
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageResponse retVal = ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).dequeueMessage(inValue);
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageResponse> Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType.dequeueMessageAsync(Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest request) {
            return base.Channel.dequeueMessageAsync(request);
        }

        public System.Threading.Tasks.Task<Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageResponse> dequeueMessageAsync(string MessageId) {
            Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest inValue = new Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.dequeueMessageRequest();
            inValue.MessageId = MessageId;
            return ((Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix.marketMessagingB2BServiceV01PortType)(this)).dequeueMessageAsync(inValue);
        }
    }
}
