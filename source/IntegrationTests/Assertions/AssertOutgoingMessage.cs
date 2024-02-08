﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Assertions;

public class AssertOutgoingMessage
{
    private readonly Serializer _serializer = new();
    private readonly dynamic _message;
    private readonly string _messageRecord;

    private AssertOutgoingMessage(dynamic message, string messageRecord)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        Assert.NotNull(message);
        _message = message;
        _messageRecord = messageRecord;
    }

    public static async Task<AssertOutgoingMessage> OutgoingMessageAsync(string messageType, string businessReason, ActorRole receiverRole, IDatabaseConnectionFactory connectionFactoryFactory, IFileStorageClient fileStorageClient)
    {
        ArgumentNullException.ThrowIfNull(receiverRole);
        ArgumentNullException.ThrowIfNull(connectionFactoryFactory);
        ArgumentNullException.ThrowIfNull(fileStorageClient);

        using var connection = await connectionFactoryFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var message = await connection.QuerySingleAsync(
            $"SELECT m.Id, m.RecordId, m.DocumentType, m.ReceiverId, m.ProcessId, m.BusinessReason," +
            $"m.ReceiverRole, m.SenderId, m.SenderRole, m.FileStorageReference " +
            $" FROM [dbo].[OutgoingMessages] m" +
            $" WHERE m.DocumentType = '{messageType}' AND m.BusinessReason = '{businessReason}' AND m.ReceiverRole = '{receiverRole.Code}'");

        Assert.NotNull(message);
        Assert.NotNull(message.FileStorageReference);

        var fileStorageFile = await fileStorageClient.DownloadAsync(new FileStorageReference(FileStorageCategory.OutgoingMessage(), message.FileStorageReference));

        var messageRecord = await fileStorageFile.ReadAsStringAsync();

        return new AssertOutgoingMessage(message, messageRecord);
    }

    public static async Task OutgoingMessageIsNullAsync(string messageType, string businessReason, ActorRole receiverRole, IDatabaseConnectionFactory connectionFactoryFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactoryFactory);
        ArgumentNullException.ThrowIfNull(receiverRole);
        using var connection = await connectionFactoryFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var message = await connection.QuerySingleOrDefaultAsync(
            $"SELECT m.Id, m.RecordId, m.DocumentType, m.ReceiverId, m.ProcessId, m.BusinessReason," +
            $"m.ReceiverRole, m.SenderId, m.SenderRole, m.FileStorageReference " +
            $" FROM [dbo].[OutgoingMessages] m" +
            $" WHERE m.DocumentType = '{messageType}' AND m.BusinessReason = '{businessReason}' AND m.ReceiverRole = '{receiverRole.Code}'");

        Assert.Null(message);
    }

    public AssertOutgoingMessage HasReceiverId(string receiverId)
    {
        Assert.Equal(receiverId, _message.ReceiverId);
        return this;
    }

    public AssertOutgoingMessage HasReceiverRole(string receiverRole)
    {
        Assert.Equal(receiverRole, _message.ReceiverRole);
        return this;
    }

    public AssertOutgoingMessage HasSenderId(string senderId)
    {
        Assert.Equal(senderId, _message.SenderId);
        return this;
    }

    public AssertOutgoingMessage HasSenderRole(string senderRole)
    {
        Assert.Equal(senderRole, _message.SenderRole);
        return this;
    }

    public AssertOutgoingMessage HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);

        Assert.Equal(businessReason.Name, _message.BusinessReason);
        return this;
    }

    public AssertOutgoingMessage HasMessageRecordValue<TMessageRecord>(
        Func<TMessageRecord, object?> propertySelector,
        object? expectedValue)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var sut = _serializer.Deserialize<TMessageRecord>(_messageRecord);
        propertySelector(sut).Should().Be(expectedValue);
        return this;
    }

    public AssertOutgoingMessage HasMessageRecordValue<TMessageRecord, TProperty>(
        Func<TMessageRecord, TProperty?> propertySelector,
        Action<TProperty?> assertion)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        ArgumentNullException.ThrowIfNull(propertySelector);

        var sut = _serializer.Deserialize<TMessageRecord>(_messageRecord);
        assertion(propertySelector(sut));
        return this;
    }

    public AssertOutgoingMessage HasPointsInCorrectOrder<TMessageRecord, TType>(
        Func<TMessageRecord, List<TType>> propertySelector,
        IList<TimeSeriesPoint> expectedPointsInRightOrder)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(expectedPointsInRightOrder);

        var sut = _serializer.Deserialize<TMessageRecord>(_messageRecord);
        for (var i = 0; i < expectedPointsInRightOrder.Count; i++)
        {
            propertySelector(sut)[i].Should()
                .Be(decimal.Parse($"{expectedPointsInRightOrder[i].Quantity.Units}.{expectedPointsInRightOrder[i].Quantity.Nanos}", CultureInfo.InvariantCulture));
        }

        return this;
    }
}
