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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public sealed class BusinessReason : EnumerationTypeWithCode<BusinessReason>
{
    // ReSharper disable InconsistentNaming
    #pragma warning disable IDE1006
    // Must match the BusinessReason names in Energinet.DataHub.Wholesale.Edi.Models.BusinessReason
    public static readonly BusinessReason MoveIn = new(nameof(MoveIn), "E65");
    public static readonly BusinessReason BalanceFixing = new("BalanceFixing", "D04");
    public static readonly BusinessReason PreliminaryAggregation = new("PreliminaryAggregation", "D03");
    public static readonly BusinessReason WholesaleFixing = new("WholesaleFixing", "D05");    //Engrosafiksering
    public static readonly BusinessReason Correction = new("Correction", "D32");

    #region Unused business reasons
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A01 = new(nameof(A01), "A01");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A02 = new(nameof(A02), "A02");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A03 = new(nameof(A03), "A03");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A04 = new(nameof(A04), "A04");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A05 = new(nameof(A05), "A05");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A06 = new(nameof(A06), "A06");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A07 = new(nameof(A07), "A07");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A08 = new(nameof(A08), "A08");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A09 = new(nameof(A09), "A09");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A10 = new(nameof(A10), "A10");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A11 = new(nameof(A11), "A11");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A12 = new(nameof(A12), "A12");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A13 = new(nameof(A13), "A13");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A14 = new(nameof(A14), "A14");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A15 = new(nameof(A15), "A15");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A16 = new(nameof(A16), "A16");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A17 = new(nameof(A17), "A17");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A18 = new(nameof(A18), "A18");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A19 = new(nameof(A19), "A19");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A20 = new(nameof(A20), "A20");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A21 = new(nameof(A21), "A21");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A22 = new(nameof(A22), "A22");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A23 = new(nameof(A23), "A23");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A24 = new(nameof(A24), "A24");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A25 = new(nameof(A25), "A25");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A26 = new(nameof(A26), "A26");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A27 = new(nameof(A27), "A27");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A28 = new(nameof(A28), "A28");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A29 = new(nameof(A29), "A29");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A30 = new(nameof(A30), "A30");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A31 = new(nameof(A31), "A31");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A32 = new(nameof(A32), "A32");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A33 = new(nameof(A33), "A33");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A34 = new(nameof(A34), "A34");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A35 = new(nameof(A35), "A35");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A36 = new(nameof(A36), "A36");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A37 = new(nameof(A37), "A37");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A38 = new(nameof(A38), "A38");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A39 = new(nameof(A39), "A39");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A40 = new(nameof(A40), "A40");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A41 = new(nameof(A41), "A41");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A42 = new(nameof(A42), "A42");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A43 = new(nameof(A43), "A43");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A44 = new(nameof(A44), "A44");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A45 = new(nameof(A45), "A45");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A46 = new(nameof(A46), "A46");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A47 = new(nameof(A47), "A47");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A48 = new(nameof(A48), "A48");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A49 = new(nameof(A49), "A49");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A50 = new(nameof(A50), "A50");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A51 = new(nameof(A51), "A51");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A52 = new(nameof(A52), "A52");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A53 = new(nameof(A53), "A53");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A54 = new(nameof(A54), "A54");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A55 = new(nameof(A55), "A55");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A56 = new(nameof(A56), "A56");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A57 = new(nameof(A57), "A57");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A58 = new(nameof(A58), "A58");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A59 = new(nameof(A59), "A59");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A60 = new(nameof(A60), "A60");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A61 = new(nameof(A61), "A61");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A62 = new(nameof(A62), "A62");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A63 = new(nameof(A63), "A63");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A64 = new(nameof(A64), "A64");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A65 = new(nameof(A65), "A65");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D02 = new(nameof(D02), "D02");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D06 = new(nameof(D06), "D06");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D07 = new(nameof(D07), "D07");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D08 = new(nameof(D08), "D08");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D09 = new(nameof(D09), "D09");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D10 = new(nameof(D10), "D10");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D11 = new(nameof(D11), "D11");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D12 = new(nameof(D12), "D12");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D13 = new(nameof(D13), "D13");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D14 = new(nameof(D14), "D14");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D15 = new(nameof(D15), "D15");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D16 = new(nameof(D16), "D16");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D17 = new(nameof(D17), "D17");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D18 = new(nameof(D18), "D18");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D19 = new(nameof(D19), "D19");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D20 = new(nameof(D20), "D20");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D21 = new(nameof(D21), "D21");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D22 = new(nameof(D22), "D22");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D23 = new(nameof(D23), "D23");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D24 = new(nameof(D24), "D24");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D25 = new(nameof(D25), "D25");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D26 = new(nameof(D26), "D26");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D27 = new(nameof(D27), "D27");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D28 = new(nameof(D28), "D28");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D29 = new(nameof(D29), "D29");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D30 = new(nameof(D30), "D30");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D31 = new(nameof(D31), "D31");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D33 = new(nameof(D33), "D33");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D34 = new(nameof(D34), "D34");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D35 = new(nameof(D35), "D35");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D36 = new(nameof(D36), "D36");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D37 = new(nameof(D37), "D37");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D38 = new(nameof(D38), "D38");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D39 = new(nameof(D39), "D39");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D40 = new(nameof(D40), "D40");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D41 = new(nameof(D41), "D41");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D42 = new(nameof(D42), "D42");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D43 = new(nameof(D43), "D43");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D44 = new(nameof(D44), "D44");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D45 = new(nameof(D45), "D45");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D46 = new(nameof(D46), "D46");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D48 = new(nameof(D48), "D48");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E01 = new(nameof(E01), "E01");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E02 = new(nameof(E02), "E02");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E03 = new(nameof(E03), "E03");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E05 = new(nameof(E05), "E05");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E06 = new(nameof(E06), "E06");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E0G = new(nameof(E0G), "E0G");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E20 = new(nameof(E20), "E20");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E23 = new(nameof(E23), "E23");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E30 = new(nameof(E30), "E30");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E32 = new(nameof(E32), "E32");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E34 = new(nameof(E34), "E34");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E53 = new(nameof(E53), "E53");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E56 = new(nameof(E56), "E56");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E66 = new(nameof(E66), "E66");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E67 = new(nameof(E67), "E67");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E75 = new(nameof(E75), "E75");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E79 = new(nameof(E79), "E79");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E80 = new(nameof(E80), "E80");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E84 = new(nameof(E84), "E84");

    /*
     Represents the following to business reasons from our json schema:
     * "A01",
                "A02",
                "A03",
                "A04",
                "A05",
                "A06",
                "A07",
                "A08",
                "A09",
                "A10",
                "A11",
                "A12",
                "A13",
                "A14",
                "A15",
                "A16",
                "A17",
                "A18",
                "A19",
                "A20",
                "A21",
                "A22",
                "A23",
                "A24",
                "A25",
                "A26",
                "A27",
                "A28",
                "A29",
                "A30",
                "A31",
                "A32",
                "A33",
                "A34",
                "A35",
                "A36",
                "A37",
                "A38",
                "A39",
                "A40",
                "A41",
                "A42",
                "A43",
                "A44",
                "A45",
                "A46",
                "A47",
                "A48",
                "A49",
                "A50",
                "A51",
                "A52",
                "A53",
                "A54",
                "A55",
                "A56",
                "A57",
                "A58",
                "A59",
                "A60",
                "A61",
                "A62",
                "A63",
                "A64",
                "A65",
                "D02",
                "D03",
                "D04",
                "D05",
                "D06",
                "D07",
                "D08",
                "D09",
                "D10",
                "D11",
                "D12",
                "D13",
                "D14",
                "D15",
                "D16",
                "D17",
                "D18",
                "D19",
                "D20",
                "D21",
                "D22",
                "D23",
                "D24",
                "D25",
                "D26",
                "D27",
                "D28",
                "D29",
                "D30",
                "D31",
                "D32",
                "D33",
                "D34",
                "D35",
                "D36",
                "D37",
                "D38",
                "D39",
                "D40",
                "D41",
                "D42",
                "D43",
                "D44",
                "D45",
                "D46",
                "D48",
                "E01",
                "E02",
                "E03",
                "E05",
                "E06",
                "E0G",
                "E20",
                "E23",
                "E30",
                "E32",
                "E34",
                "E53",
                "E56",
                "E65",
                "E66",
                "E67",
                "E75",
                "E79",
                "E80",
                "E84"
     */
    #endregion
    // ReSharper restore InconsistentNaming
    #pragma warning restore IDE1006

    private BusinessReason(string name, string code)
     : base(name, code)
    {
    }
}
