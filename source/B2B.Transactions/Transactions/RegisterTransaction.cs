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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using B2B.Transactions.OutgoingMessages;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace B2B.Transactions.Transactions
{
    public class RegisterTransaction
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly ITransactionRepository _transactionRepository;

        public RegisterTransaction(IOutgoingMessageStore outgoingMessageStore, ISystemDateTimeProvider systemDateTimeProvider, ITransactionRepository transactionRepository)
        {
            _outgoingMessageStore = outgoingMessageStore ?? throw new ArgumentNullException(nameof(outgoingMessageStore));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        }

        public Task HandleAsync(B2BTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);

            _outgoingMessageStore.Add(CreateAcceptMessage(transaction));
            return Task.CompletedTask;
        }

        private static string GenerateTransactionId()
        {
            return TransactionIdGenerator.Generate();
        }

        private static string GenerateMessageId()
        {
            return MessageIdGenerator.Generate();
        }

        private AcceptMessage CreateAcceptMessage(B2BTransaction transaction)
        {
            var settings = new XmlWriterSettings() { OmitXmlDeclaration = false, Encoding = Encoding.UTF8 };
            using var output = new Utf8StringWriter();
            using var writer = XmlWriter.Create(output, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("cim", "ConfirmRequestChangeOfSupplier_MarketDocument", "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd");
            writer.WriteElementString("mRID", null, GenerateMessageId());
            writer.WriteElementString("type", null, "414");
            writer.WriteElementString("process.processType", null, transaction.Message.ProcessType);
            writer.WriteElementString("businessSector.type", null, "23");

            writer.WriteStartElement("sender_MarketParticipant.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue("5790001330552");
            writer.WriteEndElement();

            writer.WriteElementString("sender_MarketParticipant.marketRole.type", null, "DDZ");

            writer.WriteStartElement("receiver_MarketParticipant.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.Message.SenderId);
            writer.WriteEndElement();
            writer.WriteElementString("receiver_MarketParticipant.marketRole.type", null, transaction.Message.SenderRole);
            writer.WriteElementString("createdDateTime", null, GetCurrentDateTime());
            writer.WriteElementString("reason.code", null, "A01");

            writer.WriteStartElement("cim", "MktActivityRecord", null);
            writer.WriteElementString("mRID", null, GenerateTransactionId());
            writer.WriteElementString("originalTransactionIDReference_MktActivityRecord.mRID", null, transaction.MarketActivityRecord.Id);

            writer.WriteStartElement("marketEvaluationPoint.mRID");
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction.MarketActivityRecord.EnergySupplierId);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Close();
            output.Flush();

            return new AcceptMessage(output.ToString());
        }

        private string GetCurrentDateTime()
        {
            return _systemDateTimeProvider.Now().ToString();
        }
    }
}
