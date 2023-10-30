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

using Energinet.DataHub.EDI.Domain.Common;

namespace Energinet.DataHub.EDI.Domain.OutgoingMessages;

public sealed class BusinessReason : EnumerationCodeType
{
    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
    public static readonly BusinessReason MoveIn = new(0, nameof(MoveIn), "E65");
    public static readonly BusinessReason BalanceFixing = new(1, nameof(BalanceFixing), "D04");
    public static readonly BusinessReason PreliminaryAggregation = new(2, nameof(PreliminaryAggregation), "D03");
    public static readonly BusinessReason WholesaleFixing = new(3, nameof(WholesaleFixing), "D05");    //Engrosafiksering
    public static readonly BusinessReason Correction = new(5, nameof(Correction), "D32");

    #region Unused business reasons
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A01 = new(100, nameof(A01), "A01");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A02 = new(101, nameof(A02), "A02");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A03 = new(102, nameof(A03), "A03");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A04 = new(103, nameof(A04), "A04");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A05 = new(104, nameof(A05), "A05");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A06 = new(105, nameof(A06), "A06");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A07 = new(106, nameof(A07), "A07");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A08 = new(107, nameof(A08), "A08");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A09 = new(108, nameof(A09), "A09");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A10 = new(109, nameof(A10), "A10");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A11 = new(110, nameof(A11), "A11");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A12 = new(111, nameof(A12), "A12");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A13 = new(112, nameof(A13), "A13");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A14 = new(113, nameof(A14), "A14");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A15 = new(114, nameof(A15), "A15");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A16 = new(115, nameof(A16), "A16");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A17 = new(116, nameof(A17), "A17");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A18 = new(117, nameof(A18), "A18");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A19 = new(118, nameof(A19), "A19");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A20 = new(119, nameof(A20), "A20");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A21 = new(120, nameof(A21), "A21");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A22 = new(121, nameof(A22), "A22");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A23 = new(122, nameof(A23), "A23");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A24 = new(123, nameof(A24), "A24");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A25 = new(124, nameof(A25), "A25");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A26 = new(125, nameof(A26), "A26");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A27 = new(126, nameof(A27), "A27");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A28 = new(127, nameof(A28), "A28");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A29 = new(128, nameof(A29), "A29");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A30 = new(129, nameof(A30), "A30");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A31 = new(130, nameof(A31), "A31");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A32 = new(131, nameof(A32), "A32");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A33 = new(132, nameof(A33), "A33");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A34 = new(133, nameof(A34), "A34");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A35 = new(134, nameof(A35), "A35");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A36 = new(135, nameof(A36), "A36");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A37 = new(136, nameof(A37), "A37");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A38 = new(137, nameof(A38), "A38");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A39 = new(138, nameof(A39), "A39");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A40 = new(139, nameof(A40), "A40");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A41 = new(140, nameof(A41), "A41");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A42 = new(141, nameof(A42), "A42");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A43 = new(142, nameof(A43), "A43");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A44 = new(143, nameof(A44), "A44");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A45 = new(144, nameof(A45), "A45");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A46 = new(145, nameof(A46), "A46");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A47 = new(146, nameof(A47), "A47");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A48 = new(147, nameof(A48), "A48");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A49 = new(148, nameof(A49), "A49");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A50 = new(149, nameof(A50), "A50");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A51 = new(150, nameof(A51), "A51");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A52 = new(151, nameof(A52), "A52");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A53 = new(152, nameof(A53), "A53");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A54 = new(153, nameof(A54), "A54");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A55 = new(154, nameof(A55), "A55");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A56 = new(155, nameof(A56), "A56");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A57 = new(156, nameof(A57), "A57");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A58 = new(157, nameof(A58), "A58");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A59 = new(158, nameof(A59), "A59");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A60 = new(159, nameof(A60), "A60");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A61 = new(160, nameof(A61), "A61");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A62 = new(161, nameof(A62), "A62");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A63 = new(162, nameof(A63), "A63");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A64 = new(163, nameof(A64), "A64");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason A65 = new(164, nameof(A65), "A65");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D02 = new(165, nameof(D02), "D02");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D06 = new(169, nameof(D06), "D06");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D07 = new(170, nameof(D07), "D07");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D08 = new(171, nameof(D08), "D08");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D09 = new(172, nameof(D09), "D09");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D10 = new(173, nameof(D10), "D10");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D11 = new(174, nameof(D11), "D11");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D12 = new(175, nameof(D12), "D12");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D13 = new(176, nameof(D13), "D13");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D14 = new(177, nameof(D14), "D14");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D15 = new(178, nameof(D15), "D15");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D16 = new(179, nameof(D16), "D16");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D17 = new(180, nameof(D17), "D17");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D18 = new(181, nameof(D18), "D18");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D19 = new(182, nameof(D19), "D19");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D20 = new(183, nameof(D20), "D20");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D21 = new(184, nameof(D21), "D21");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D22 = new(185, nameof(D22), "D22");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D23 = new(186, nameof(D23), "D23");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D24 = new(187, nameof(D24), "D24");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D25 = new(188, nameof(D25), "D25");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D26 = new(189, nameof(D26), "D26");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D27 = new(190, nameof(D27), "D27");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D28 = new(191, nameof(D28), "D28");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D29 = new(192, nameof(D29), "D29");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D30 = new(193, nameof(D30), "D30");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D31 = new(194, nameof(D31), "D31");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D33 = new(196, nameof(D33), "D33");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D34 = new(197, nameof(D34), "D34");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D35 = new(198, nameof(D35), "D35");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D36 = new(199, nameof(D36), "D36");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D37 = new(200, nameof(D37), "D37");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D38 = new(201, nameof(D38), "D38");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D39 = new(202, nameof(D39), "D39");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D40 = new(203, nameof(D40), "D40");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D41 = new(204, nameof(D41), "D41");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D42 = new(205, nameof(D42), "D42");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D43 = new(206, nameof(D43), "D43");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D44 = new(207, nameof(D44), "D44");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D45 = new(208, nameof(D45), "D45");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D46 = new(209, nameof(D46), "D46");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason D48 = new(210, nameof(D48), "D48");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E01 = new(211, nameof(E01), "E01");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E02 = new(212, nameof(E02), "E02");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E03 = new(213, nameof(E03), "E03");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E05 = new(214, nameof(E05), "E05");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E06 = new(215, nameof(E06), "E06");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E0G = new(216, nameof(E0G), "E0G");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E20 = new(217, nameof(E20), "E20");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E23 = new(218, nameof(E23), "E23");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E30 = new(219, nameof(E30), "E30");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E32 = new(220, nameof(E32), "E32");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E34 = new(221, nameof(E34), "E34");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E53 = new(222, nameof(E53), "E53");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E56 = new(223, nameof(E56), "E56");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E66 = new(225, nameof(E66), "E66");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E67 = new(226, nameof(E67), "E67");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E75 = new(227, nameof(E75), "E75");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E79 = new(228, nameof(E79), "E79");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E80 = new(229, nameof(E80), "E80");
    [Obsolete("Unused, but required for schema compliance")]
    public static readonly BusinessReason E84 = new(230, nameof(E84), "E84");

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

    private BusinessReason(int id, string name, string code)
     : base(id, name, code)
    {
    }

    public static BusinessReason From(string valueToParse)
    {
        var businessReason = GetAll<BusinessReason>().FirstOrDefault(processType =>
            processType.Name.Equals(valueToParse, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"{valueToParse} is not a valid process type");
        return businessReason;
    }
}
