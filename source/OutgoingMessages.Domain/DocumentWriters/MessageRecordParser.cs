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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;

public class MessageRecordParser : IMessageRecordParser
{
    private readonly ISerializer _serializer;

    public MessageRecordParser(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public string From<TMessageRecord>(TMessageRecord messageRecord)
    {
        return _serializer.Serialize(messageRecord);
    }

    public TMessageRecord From<TMessageRecord>(string payload)
    {
        return _serializer.Deserialize<TMessageRecord>(payload);
    }
}
