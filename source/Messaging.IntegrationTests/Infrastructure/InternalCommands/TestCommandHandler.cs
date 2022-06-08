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

namespace Messaging.IntegrationTests.Infrastructure.InternalCommands
{
    public class TestCommandHandler : IRequestHandler<TestCommand, Unit>
    {
        public Task<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.ThrowException)
            {
                throw new InvalidOperationException("This is a test exception");
            }

            return Unit.Task;
        }
    }
}
