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
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class TestNotificationHandlerSpy : INotificationHandler<TestNotification>
{
    private bool _shouldThrowException;

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        if (_shouldThrowException)
        {
            _shouldThrowException = false;
            throw new InvalidOperationException("A test exception");
        }

        return Task.CompletedTask;
    }

    public void ShouldThrowException()
    {
        _shouldThrowException = true;
    }

    public void CannotHandleEvent()
    {
        throw new NotImplementedException();
    }
}
