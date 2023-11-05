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
using System.IO;
using System.Text;

namespace Energinet.DataHub.EDI.Infrastructure.EbixMessageAdapter.Utilities
{
    public class StringWriterWithEncoding : StringWriter
    {
#nullable enable
        private readonly Encoding? _encoding;
#nullable disable

        public StringWriterWithEncoding()
        {
        }

        public StringWriterWithEncoding(IFormatProvider formatProvider)
            : base(formatProvider)
        {
        }

        public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider)
            : base(sb, formatProvider)
        {
        }

        public StringWriterWithEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public StringWriterWithEncoding(IFormatProvider formatProvider, Encoding encoding)
            : base(formatProvider)
        {
            _encoding = encoding;
        }

        public StringWriterWithEncoding(StringBuilder sb, IFormatProvider formatProvider, Encoding encoding)
            : base(sb, formatProvider)
        {
            _encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding ?? base.Encoding; }
        }
    }
}
