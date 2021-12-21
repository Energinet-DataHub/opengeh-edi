# MarketRoles

[![codecov](https://codecov.io/gh/Energinet-DataHub/geh-market-roles/branch/main/graph/badge.svg?token=R80X7DC6C0)](https://codecov.io/gh/Energinet-DataHub/geh-market-roles)

## Intro

The market roles domain is in charge of supply and consumer relations.
It handles changes in supply (who supplies/produces electricity),
and changes in consumers/producers (who is a consumer), on metering points.

These are the processes maintained by this domain.

| Process                                                                                                                                                                                       |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Change of Energy Supplier](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/change-of-energy-supplier.md)                                             |
| [End of Supply](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/end-of-supply.md)                                                                     |
| [Forced Energy Supplier change](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/forced-energy-supplier-change.md)                                     |
| [Incorrect Energy Supplier change](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/incorrect-energy-supplier-change.md)                               |
| [Consumer move in](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/consumer-move-in.md)                                                               |
| [Consumer move out](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/consumer-move-out.md)                                                             |
| [Incorrect move](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/incorrect-move.md)                                                                   |
| [Forward consumer master data by Energy Supplier](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/forward-consumer-master-data-by-energy-supplier.md) |
| [Forward contact address by Grid Access Provider](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/forward-contact-address-by-grid-access-provider.md)    |
| [Change of BRP for Energy Supplier](https://github.com/Energinet-DataHub/geh-market-roles/blob/main/docs/business-processes/change-of-energy-supplier.md)                                     |
| ....                                                                                                                                                                                          |

## Architecture

![design](ARCHITECTURE.png)

## Context Streams

<img width="405" alt="Market roles context streams" src="https://user-images.githubusercontent.com/25637982/114846333-e2e5a980-9ddc-11eb-9941-ac03cbcc8336.PNG">

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

## Domain Roadmap

In this program increment we are working on finishing the happy flow MVP for Change of energy supplier. This includes:

- Generation of all remaining messages to be generated in accordance to the sequence diagram in the [Change of energy supplier description](docs/business-processes/change-of-energy-supplier.md).
- Allowing for process to be cancelled before expiration of cancellation period.
- Notifying current energy supplier that his supply period is ending upon expiration of cancellation period.
- Setting end date and on prior supplier once process is completed.

## Getting Started

[Read here how to get started](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/docs/getting-started.md).

## Where can I get more help?

Please see the [community documentation](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/COMMUNITY.md)
