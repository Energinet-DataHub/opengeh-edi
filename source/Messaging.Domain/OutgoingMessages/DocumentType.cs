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

using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class DocumentType : EnumerationType
{
    public static readonly DocumentType GenericNotification = new(0, "GenericNotification");
    public static readonly DocumentType ConfirmRequestChangeOfSupplier = new(1, nameof(ConfirmRequestChangeOfSupplier));
    public static readonly DocumentType RejectRequestChangeOfSupplier = new(2, nameof(RejectRequestChangeOfSupplier));
    public static readonly DocumentType AccountingPointCharacteristics = new(3, nameof(AccountingPointCharacteristics));
    public static readonly DocumentType CharacteristicsOfACustomerAtAnAP = new(4, nameof(CharacteristicsOfACustomerAtAnAP));
    public static readonly DocumentType ConfirmRequestChangeAccountingPointCharacteristics = new(5, nameof(ConfirmRequestChangeAccountingPointCharacteristics));

    private DocumentType(int id, string name)
        : base(id, name)
    {
    }

    public override string ToString()
    {
        return Name;
    }
}
