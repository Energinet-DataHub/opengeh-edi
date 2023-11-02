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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kamstrup.DataHub.Integration.DataHub;
using Microsoft.Extensions.Configuration;

namespace SoapTest
{
    public partial class Form1 : Form
    {
        private readonly IConfigurationRoot _configurationRoot;

        public Form1()
        {
            _configurationRoot = new ConfigurationBuilder().AddJsonFile("local.settings.json").Build();
            InitializeComponent();
        }

        private void BtnPeek_Click(object sender, EventArgs e)
        {
            var broker = new DataHubBroker();
            var res = broker.PeekMessage(_configurationRoot["Values:TestUrl"] ?? string.Empty, _configurationRoot["Values:Bearer"] ?? string.Empty);
            txtResult.Text = res is not null ? res.ToString() : "Found nothing";
        }

        private void BtnGetMessage_Click(object sender, EventArgs e)
        {
            var broker = new DataHubBroker();
            var res = broker.GetMessage(_configurationRoot["Values:TestUrl"] ?? string.Empty, _configurationRoot["Values:Bearer"] ?? string.Empty, txtMessageId.Text);
            txtResult.Text = res is not null ? res.ToString() : "Found nothing";
        }
    }
}
