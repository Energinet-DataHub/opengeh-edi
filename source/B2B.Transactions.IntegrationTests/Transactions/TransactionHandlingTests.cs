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
using B2B.Transactions.Configuration;
using B2B.Transactions.DataAccess;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Incoming;
using B2B.Transactions.Xml.Outgoing;
using Xunit;
using Xunit.Categories;

namespace B2B.Transactions.IntegrationTests.Transactions
{
    [IntegrationTest]
    public class TransactionHandlingTests : TestBase
    {
        private static readonly SystemDateTimeProviderStub _dateTimeProvider = new();
        private readonly ICorrelationContext _correlationContext;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly XNamespace _namespace = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private IMessageFactory<IDocument> _messageFactory = new AcceptMessageFactory(_dateTimeProvider, new MessageValidator(new SchemaProvider(new SchemaStore())));

        public TransactionHandlingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _correlationContext = GetService<ICorrelationContext>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _transactionRepository =
                GetService<ITransactionRepository>();
            _unitOfWork = GetService<IUnitOfWork>();
        }

        [Fact]
        public async Task Transaction_is_registered()
        {
            var transaction = TransactionBuilder.CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.GetById(transaction.Message.MessageId);
            Assert.NotNull(savedTransaction);
        }

        [Fact]
        public async Task Message_is_generated_when_transaction_is_accepted()
        {
            var now = _dateTimeProvider.Now();
            _dateTimeProvider.SetNow(now);
            var transaction = TransactionBuilder.CreateTransaction();
            await RegisterTransaction(transaction).ConfigureAwait(false);

            var acceptMessage = _outgoingMessageStore.GetUnpublished().FirstOrDefault();
            Assert.NotNull(acceptMessage);
            var document = CreateDocument(acceptMessage!.MessagePayload ?? string.Empty);

            AssertHeader(document, transaction);
            AssertMarketActivityRecord(document, transaction);
        }

        private static XDocument CreateDocument(string payload)
        {
            return XDocument.Parse(payload);
        }

        private Task RegisterTransaction(B2BTransaction transaction)
        {
            var handler = new RegisterTransaction(_outgoingMessageStore, _transactionRepository, _messageFactory, _unitOfWork, _correlationContext);
            return handler.HandleAsync(transaction);
        }

        private void AssertMarketActivityRecord(XDocument document, B2BTransaction transaction)
        {
            Assert.NotNull(GetMarketActivityRecordValue(document, "mRID"));
            AssertMarketActivityRecordValue(document, "originalTransactionIDReference_MktActivityRecord.mRID", transaction.MarketActivityRecord.Id);
            AssertMarketActivityRecordValue(document, "marketEvaluationPoint.mRID", transaction.MarketActivityRecord.MarketEvaluationPointId);
        }

        private void AssertHeader(XDocument document, B2BTransaction transaction)
        {
            Assert.NotNull(GetMessageHeaderValue(document, "mRID"));
            AssertHasHeaderValue(document, "type", "414");
            AssertHasHeaderValue(document, "process.processType", transaction.Message.ProcessType);
            AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", "5790001330552");
            AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", "DDZ");
            AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", transaction.Message.SenderId);
            AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", transaction.Message.SenderRole);
            AssertHasHeaderValue(document, "createdDateTime", _dateTimeProvider.Now().ToString());
            AssertHasHeaderValue(document, "reason.code", "A01");
        }

        private void AssertHasHeaderValue(XDocument document, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, GetMessageHeaderValue(document, elementName));
        }

        private void AssertMarketActivityRecordValue(XDocument document, string elementName, string? expectedValue)
        {
            Assert.Equal(expectedValue, GetMarketActivityRecordValue(document, elementName));
        }

        private string GetMarketActivityRecordValue(XDocument document, string elementName)
        {
            var element = GetHeaderElement(document)?.Element(_namespace + "MktActivityRecord")?.Element(_namespace + elementName);
            return element?.Value ?? string.Empty;
        }

        private string? GetMessageHeaderValue(XDocument document, string elementName)
        {
            return GetHeaderElement(document)?.Element(_namespace + elementName)?.Value;
        }

        private XElement? GetHeaderElement(XDocument document)
        {
            return document?.Element(_namespace + "ConfirmRequestChangeOfSupplier_MarketDocument");
        }
    }
}
