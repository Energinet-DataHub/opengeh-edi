# MarketRoles

## Intro

The market roles domain is in charge of supply and consumer relations.
It handles changes in supply (who supplies/produces electricity),
and changes in consumers/producers (who is a consumer), on metering points.

These are the processes maintained by this domain.

| Process                                                                             |
| ----------------------------------------------------------------------------------- |
| [Change of Energy Supplier](.\docs\business-processes\change-of-energy-supplier.md) |
| [End of Supply](.\docs\business-processes\end-of-supply.md)                         |
| Forced Energy Supplier change                                                       |
| Incorrect Energy Supplier change                                                    |
| Consumer move in                                                                    |
| Consumer move out                                                                   |
| Incorrect move                                                                      |
| Forward consumer master data by Energy Supplier                                     |
| Forward contact address by grid operator                                            |
| Change of BRP by Energy Supplier                                                    |
| ....                                                                                |

## Architecture

![design](ARCHITECTURE.png)

## Context Streams

TBD

## Market Participants

The market roles domain introduces the following roles into the Green Energy Hub ecosystem:

- Balance Responsible Party
- Consumer
- Energy supplier
- Grid Access Provider
- Imbalance settlement Responsible
- Metered data aggregator
- Metered data responsible
- System Operator

In the long term all these [roles](https://github.com/Energinet-DataHub/green-energy-hub/docs/dictionary-and-concepts/dictionary-market-participants.md) will be part of the Green Energy Hub

## Domain Roadmap

TBD

## Getting Started

TBD

## Where can I get more help?

Code owners? Gitter versus Slack? Referral to main repository?
