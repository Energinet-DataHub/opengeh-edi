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

using System.ComponentModel.DataAnnotations.Schema;

namespace Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;

public record OperatingIdentityComplexType
{
    internal OperatingIdentityComplexType(OperatingIdentity value)
    {
        Value = value;
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private OperatingIdentityComplexType()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public OperatingIdentity Value
    {
        get
        {
            switch (IdentityType)
            {
                case nameof(ActorIdentity):
                    return new ActorIdentity(new ActorId(ActorId!.Value));

                case nameof(UserIdentity):
                    return new UserIdentity(new UserId(UserId!.Value), new ActorId(ActorId!.Value));

                default:
                    throw new InvalidOperationException($"Unknown operating identity type '{IdentityType}'.");
            }
        }

        private set
        {
            switch (value)
            {
                case ActorIdentity actor:
                    IdentityType = nameof(ActorIdentity);
                    ActorId = actor.ActorId.Value;
                    break;

                case UserIdentity user:
                    IdentityType = nameof(UserIdentity);
                    ActorId = user.ActorId.Value;
                    UserId = user.UserId.Value;
                    break;

                default:
                    throw new InvalidOperationException($"Invalid type '{value.GetType()}'.");
            }
        }
    }

    internal string? IdentityType { get; private set; }

    internal Guid? ActorId { get; private set; }

    internal Guid? UserId { get; private set; }
}
