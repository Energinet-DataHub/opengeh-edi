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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

public class AuthenticatedUserDoesNotMatchSenderId : ValidationError
{
    public AuthenticatedUserDoesNotMatchSenderId()
        : base("Sender id does not match id of current authenticated user", "00002", "Sender Identification in the Business Message is not authorised or user of the SendMessage operation has no relation with the organisation (i.e. Sender Identification)", "B2B-008", "SenderId")
    {
    }
}
