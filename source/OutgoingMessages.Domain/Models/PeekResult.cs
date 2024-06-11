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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;

/// <summary>
/// The result of peeking an actor's queue.
/// The result contains the bundle id and the message id of the next message in the queue.
/// The bundle id is used to identify the bundle that the message belongs to, and is used internally in EDI.
/// The message id is the mRID in the message header, and is used by the actor to identify and dequeue the message.
/// </summary>
public sealed record PeekResult(BundleId BundleId, MessageId MessageId);
