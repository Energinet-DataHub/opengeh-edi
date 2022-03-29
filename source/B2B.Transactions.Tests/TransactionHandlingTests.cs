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

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using B2B.Transactions.Messages;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Incoming;
using B2B.Transactions.Xml.Outgoing;
using Xunit;

namespace B2B.Transactions.Tests
{
    public class TransactionHandlingTests
    {
        private static readonly SystemDateTimeProviderStub _dateTimeProvider = new();
        private readonly TransactionRepository _transactionRepository = new();
        private readonly XNamespace _namespace = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
        private OutgoingMessageStoreSpy _outgoingMessageStoreSpy = new();
        private IDocumentProvider<AcceptMessage> _documentProvider = new AcceptDocumentProvider(_dateTimeProvider);

        [Fact]
        public async Task Transaction_is_registered()
        {
            var transaction = CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.GetById(transaction.Message.MessageId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Accept_message_is_sent_to_sender_when_transaction_is_accepted()
        {
            var now = _dateTimeProvider.Now();
            _dateTimeProvider.SetNow(now);
            var transaction = CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var acceptMessage = _outgoingMessageStoreSpy.Messages.FirstOrDefault();
            Assert.NotNull(acceptMessage);
            var document = CreateDocument(acceptMessage!.MessagePayload);
            Assert.NotNull(GetMessageHeaderValue(document, "mRID"));
            AssertHasHeaderValue(document, "type", "414");
            AssertHasHeaderValue(document, "process.processType", transaction.Message.ProcessType);
            AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", "5790001330552");
            AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", "DDZ");
            AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", transaction.Message.SenderId);
            AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", transaction.Message.SenderRole);
            AssertHasHeaderValue(document, "createdDateTime", now.ToString());
            AssertHasHeaderValue(document, "reason.code", "A01");

            Assert.NotNull(GetMarketActivityRecordValue(document, "mRID"));
            AssertMarketActivityRecordValue(document, "originalTransactionIDReference_MktActivityRecord.mRID", transaction.MarketActivityRecord.Id);
            AssertMarketActivityRecordValue(document, "marketEvaluationPoint.mRID", transaction.MarketActivityRecord.MarketEvaluationPointId);
        }

        private static B2BTransaction CreateTransaction()
        {
            return B2BTransaction.Create(
                new MessageHeader("fake", "fake", "fake", "fake", "fake", "somedate", "fake"),
                new MarketActivityRecord()
                {
                    BalanceResponsibleId = "fake",
                    Id = "fake",
                    ConsumerId = "fake",
                    ConsumerName = "fake",
                    EffectiveDate = "fake",
                    EnergySupplierId = "fake",
                    MarketEvaluationPointId = "fake",
                });
        }

        private static XDocument CreateDocument(string payload)
        {
            return XDocument.Parse(payload);
        }

        private Task RegisterTransaction(B2BTransaction transaction)
        {
            var useCase = new RegisterTransaction(_outgoingMessageStoreSpy, _transactionRepository, _documentProvider);
            return useCase.HandleAsync(transaction);
        }

        private void AssertHasHeaderValue(XDocument document, string elementName, string expectedValue)
        {
            Assert.Equal(expectedValue, GetMessageHeaderValue(document, elementName));
        }

        private void AssertMarketActivityRecordValue(XDocument document, string elementName, string expectedValue)
        {
            Assert.Equal(expectedValue, GetMarketActivityRecordValue(document, elementName));
        }

        private string GetMarketActivityRecordValue(XDocument document, string elementName)
        {
            var element = GetHeaderElement(document)?.Element(_namespace + "MktActivityRecord")?.Element(elementName);
            return element?.Value ?? string.Empty;
        }

        private string? GetMessageHeaderValue(XDocument document, string elementName)
        {
            return GetHeaderElement(document)?.Element(elementName)?.Value;
        }

        private XElement? GetHeaderElement(XDocument document)
        {
            return document?.Element(_namespace + "ConfirmRequestChangeOfSupplier_MarketDocument");
        }
    }
}
