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

namespace Energinet.DataHub.EDI.IntegrationTests.Migration;

public static class JsonPayloadConstants
{
    public const string SingleTimeSeriesWithSingleObservation = """
       {
           "MeteredDataTimeSeriesDH3": {
               "Header": {
                   "MessageId": "13255042",
                   "DocumentType": "E66",
                   "Creation": "2024-01-16T07:55:33Z",
                   "EnergyBusinessProcess": "D42",
                   "EnergyIndustryClassification": "23",
                   "SenderIdentification": "5790001330552",
                   "RecipientIdentification": "5790001330595",
                   "EnergyBusinessProcessRole": "D3M"
               },
               "TimeSeries": [
                   {
                       "TimeSeriesId": "74634301_86192545",
                       "OriginalMessageId": "bc8897bc5d5b4d8a9e7f72efe4b0d4c5",
                       "OriginalTimeSeriesId": "e1f06dee48d842c1a48b187065e710ff",
                       "EnergyTimeSeriesFunction": "9",
                       "EnergyTimeSeriesProduct": "8716867000030",
                       "EnergyTimeSeriesMeasureUnit": "KWH",
                       "TypeOfMP": "E17",
                       "SettlementMethod": "D01",
                       "AggregationCriteria": {
                           "MeteringPointId": "571051839308770693"
                       },
                       "Observation": [
                           {
                               "Position": 1,
                               "QuantityQuality": "E01",
                               "EnergyQuantity": 2.0
                           }
                       ],
                       "TimeSeriesPeriod": {
                           "ResolutionDuration": "PT1H",
                           "Start": "2023-12-25T23:00:00Z",
                           "End": "2023-12-26T23:00:00Z"
                       },
                       "TransactionInsertDate": "2024-01-16T08:55:14Z",
                       "TimeSeriesStatus": "2"
                   }
               ]
           }
       }
       """;

    public const string SingleDeletedTimeSeriesWithSingleObservation = """
       {
           "MeteredDataTimeSeriesDH3": {
               "Header": {
                   "MessageId": "13255042",
                   "DocumentType": "E66",
                   "Creation": "2024-01-16T07:55:33Z",
                   "EnergyBusinessProcess": "D42",
                   "EnergyIndustryClassification": "23",
                   "SenderIdentification": "5790001330552",
                   "RecipientIdentification": "5790001330595",
                   "EnergyBusinessProcessRole": "D3M"
               },
               "TimeSeries": [
                   {
                       "TimeSeriesId": "74634301_86192545",
                       "OriginalMessageId": "bc8897bc5d5b4d8a9e7f72efe4b0d4c5",
                       "OriginalTimeSeriesId": "e1f06dee48d842c1a48b187065e710ff",
                       "EnergyTimeSeriesFunction": "9",
                       "EnergyTimeSeriesProduct": "8716867000030",
                       "EnergyTimeSeriesMeasureUnit": "KWH",
                       "TypeOfMP": "E17",
                       "SettlementMethod": "D01",
                       "AggregationCriteria": {
                           "MeteringPointId": "571051839308770693"
                       },
                       "Observation": [
                           {
                               "Position": 1,
                               "QuantityQuality": "E01",
                               "EnergyQuantity": 2.0
                           }
                       ],
                       "TimeSeriesPeriod": {
                           "ResolutionDuration": "PT1H",
                           "Start": "2023-12-25T23:00:00Z",
                           "End": "2023-12-26T23:00:00Z"
                       },
                       "TransactionInsertDate": "2024-01-16T08:55:14Z",
                       "TimeSeriesStatus": "9"
                   }
               ]
           }
       }
       """;

    public const string TwoTimeSeries = """
        {
            "MeteredDataTimeSeriesDH3": {
                "Header": {
                    "MessageId": "13255042",
                    "DocumentType": "E66",
                    "Creation": "2024-01-16T07:55:33Z",
                    "EnergyBusinessProcess": "D42",
                    "EnergyIndustryClassification": "23",
                    "SenderIdentification": "5790001330552",
                    "RecipientIdentification": "5790001330595",
                    "EnergyBusinessProcessRole": "D3M"
                },
                "TimeSeries": [
                    {
                        "TimeSeriesId": "74634299_86192542",
                        "OriginalMessageId": "d221605be3744015aed832614accb579",
                        "OriginalTimeSeriesId": "83521745ef4f4ada83f2115dda402e30",
                        "EnergyTimeSeriesFunction": "9",
                        "EnergyTimeSeriesProduct": "8716867000030",
                        "EnergyTimeSeriesMeasureUnit": "KWH",
                        "TypeOfMP": "E17",
                        "SettlementMethod": "D01",
                        "AggregationCriteria": {
                            "MeteringPointId": "571051839308770693"
                        },
                        "Observation": [
                            {
                                "Position": 1,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 2,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 3,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 4,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 5,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 6,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 7,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 8,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 9,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 10,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 11,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 12,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 13,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 14,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 15,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 16,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 17,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 18,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 19,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 20,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 21,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 22,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 23,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            },
                            {
                                "Position": 24,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 1.0
                            }
                        ],
                        "TimeSeriesPeriod": {
                            "ResolutionDuration": "PT1H",
                            "Start": "2023-12-31T23:00:00Z",
                            "End": "2024-01-01T23:00:00Z"
                        },
                        "TransactionInsertDate": "2024-01-16T08:54:58Z",
                        "TimeSeriesStatus": "2"
                    },
                    {
                        "TimeSeriesId": "74634301_86192545",
                        "OriginalMessageId": "bc8897bc5d5b4d8a9e7f72efe4b0d4c5",
                        "OriginalTimeSeriesId": "e1f06dee48d842c1a48b187065e710ff",
                        "EnergyTimeSeriesFunction": "9",
                        "EnergyTimeSeriesProduct": "8716867000030",
                        "EnergyTimeSeriesMeasureUnit": "KWH",
                        "TypeOfMP": "E17",
                        "SettlementMethod": "D01",
                        "AggregationCriteria": {
                            "MeteringPointId": "571051839308770693"
                        },
                        "Observation": [
                            {
                                "Position": 1,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 2,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 3,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 4,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 5,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 6,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 7,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 8,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 9,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 10,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 11,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 12,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 13,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 14,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 15,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 16,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 17,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 18,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 19,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 20,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 21,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 22,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 23,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            },
                            {
                                "Position": 24,
                                "QuantityQuality": "E01",
                                "EnergyQuantity": 2.0
                            }
                        ],
                        "TimeSeriesPeriod": {
                            "ResolutionDuration": "PT1H",
                            "Start": "2023-12-25T23:00:00Z",
                            "End": "2023-12-26T23:00:00Z"
                        },
                        "TransactionInsertDate": "2024-01-16T08:55:14Z",
                        "TimeSeriesStatus": "2"
                    }
                ]
            }
        }
        """;

    public const string NoTimeSeries = """
        {
          "MeteredDataTimeSeriesDH3": {
              "Header": {
                  "MessageId": "13255042",
                  "DocumentType": "E66",
                  "Creation": "2024-01-16T07:55:33Z",
                  "EnergyBusinessProcess": "D42",
                  "EnergyIndustryClassification": "23",
                  "SenderIdentification": "5790001330552",
                  "RecipientIdentification": "5790001330595",
                  "EnergyBusinessProcessRole": "D3M"
              }
          }
        }
        """;

    public const string QuantityMissingIndicatorTrueAndQualityNull = """
        {
            "MeteredDataTimeSeriesDH3": {
                "Header": {
                    "MessageId": "13255042",
                    "DocumentType": "E66",
                    "Creation": "2024-01-16T07:55:33Z",
                    "EnergyBusinessProcess": "D42",
                    "EnergyIndustryClassification": "23",
                    "SenderIdentification": "5790001330552",
                    "RecipientIdentification": "5790001330595",
                    "EnergyBusinessProcessRole": "D3M"
                },
                "TimeSeries": [
                    {
                        "TimeSeriesId": "74634299_86192542",
                        "OriginalMessageId": "d221605be3744015aed832614accb579",
                        "OriginalTimeSeriesId": "83521745ef4f4ada83f2115dda402e30",
                        "EnergyTimeSeriesFunction": "9",
                        "EnergyTimeSeriesProduct": "8716867000030",
                        "EnergyTimeSeriesMeasureUnit": "KWH",
                        "TypeOfMP": "E17",
                        "SettlementMethod": "D01",
                        "AggregationCriteria": {
                            "MeteringPointId": "571051839308770693"
                        },
                        "Observation": [
                            {
                                "Position": 1,
                                "QuantityQuality": null,
                                "QuantityMissingIndicator": true
                            },
                            {
                                "Position": 2,
                                "QuantityQuality": "E01",
                                "Quantity": 2.0
                            }
                        ],
                        "TimeSeriesPeriod": {
                            "ResolutionDuration": "PT1H",
                            "Start": "2023-12-25T23:00:00Z",
                            "End": "2023-12-26T23:00:00Z"
                        },
                        "TransactionInsertDate": "2024-01-16T08:55:14Z",
                        "TimeSeriesStatus": "2"
                    }
                ]
            }
        }
        """;

    public const string QuantityMissingIndicatorTrueForAllObservations = """
        {
            "MeteredDataTimeSeriesDH3": {
                "Header": {
                    "MessageId": "13255042",
                    "DocumentType": "E66",
                    "Creation": "2024-01-16T07:55:33Z",
                    "EnergyBusinessProcess": "D42",
                    "EnergyIndustryClassification": "23",
                    "SenderIdentification": "5790001330552",
                    "RecipientIdentification": "5790001330595",
                    "EnergyBusinessProcessRole": "D3M"
                },
                "TimeSeries": [
                    {
                        "TimeSeriesId": "74634299_86192542",
                        "OriginalMessageId": "d221605be3744015aed832614accb579",
                        "OriginalTimeSeriesId": "83521745ef4f4ada83f2115dda402e30",
                        "EnergyTimeSeriesFunction": "9",
                        "EnergyTimeSeriesProduct": "8716867000030",
                        "EnergyTimeSeriesMeasureUnit": "KWH",
                        "TypeOfMP": "E17",
                        "SettlementMethod": "D01",
                        "AggregationCriteria": {
                            "MeteringPointId": "571051839308770693"
                        },
                        "Observation": [
                            {
                                "Position": 1,
                                "QuantityQuality": null,
                                "QuantityMissingIndicator": true
                            },
                            {
                                "Position": 2,
                                "QuantityQuality": null,
                                "QuantityMissingIndicator": true
                            }
                        ],
                        "TimeSeriesPeriod": {
                            "ResolutionDuration": "PT1H",
                            "Start": "2023-12-25T23:00:00Z",
                            "End": "2023-12-26T23:00:00Z"
                        },
                        "TransactionInsertDate": "2024-01-16T08:55:14Z",
                        "TimeSeriesStatus": "2"
                    }
                ]
            }
        }
        """;
}
