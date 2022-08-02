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

namespace Messaging.Domain.OutgoingMessages;

public class NoMessagesInBundleException : Exception
{
    public NoMessagesInBundleException()
        : base("Cannot not create message from a bundle that does not contain any messages")
    {
    }

    private NoMessagesInBundleException(string message)
        : base(message)
    {
    }

    private NoMessagesInBundleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
