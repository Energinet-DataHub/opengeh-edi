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
using System.Text.Json.Serialization;
using Energinet.DataHub.MessageHub.Model.Model;
using Messaging.Application.Common.Commands;

namespace Messaging.Infrastructure.OutgoingMessages
{
    public class SendMessageRequestNotification : InternalCommand
    {
        [JsonConstructor]
        public SendMessageRequestNotification(Guid id, DataBundleRequestDto dataBundleRequestDto, Uri? uri, DataBundleResponseErrorDto? dataBundleResponseErrorDto)
        : base(id)
        {
            DataBundleRequestDto = dataBundleRequestDto;
            Uri = uri;
            DataBundleResponseErrorDto = dataBundleResponseErrorDto;
        }

        public SendMessageRequestNotification(DataBundleRequestDto dataBundleRequestDto, Uri uri)
        {
            DataBundleRequestDto = dataBundleRequestDto;
            Uri = uri;
        }

        public SendMessageRequestNotification(DataBundleRequestDto dataBundleRequestDto, DataBundleResponseErrorDto dataBundleResponseErrorDto)
        {
            DataBundleRequestDto = dataBundleRequestDto;
            DataBundleResponseErrorDto = dataBundleResponseErrorDto;
        }

        public DataBundleRequestDto DataBundleRequestDto { get; }

        public Uri? Uri { get; }

        public DataBundleResponseErrorDto? DataBundleResponseErrorDto { get; }
    }
}
