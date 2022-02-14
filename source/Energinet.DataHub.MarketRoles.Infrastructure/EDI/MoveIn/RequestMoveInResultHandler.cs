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
using System.Linq;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.EDI;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using ErrorMessageFactory = Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.ErrorMessageFactory;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.MoveIn
{
    public sealed class RequestMoveInResultHandler : BusinessProcessResultHandler<RequestMoveIn>
    {
        private readonly IActorMessageService _actorMessageService;
        private readonly ErrorMessageFactory _errorMessageFactory;
        private readonly ICorrelationContext _correlationContext;
        private readonly ISystemDateTimeProvider _dateTimeProvider;

        public RequestMoveInResultHandler(
            IActorMessageService actorMessageService,
            ErrorMessageFactory errorMessageFactory,
            ICorrelationContext correlationContext,
            ISystemDateTimeProvider dateTimeProvider,
            IOutbox outbox,
            IOutboxMessageFactory outboxMessageFactory)
            : base(outbox, outboxMessageFactory)
        {
            _actorMessageService = actorMessageService;
            _errorMessageFactory = errorMessageFactory;
            _correlationContext = correlationContext;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override object CreateRejectMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var errors = result.ValidationErrors
                .Select(error => _errorMessageFactory.GetErrorMessage(error));

            // TODO: Send reject message
            throw new NotImplementedException("Send reject message");
        }

        protected override object CreateAcceptMessage(RequestMoveIn request, BusinessProcessResult result)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (result == null) throw new ArgumentNullException(nameof(result));

            // TODO: Send confirm message
            throw new NotImplementedException("Send confirm message");
        }
    }
}
