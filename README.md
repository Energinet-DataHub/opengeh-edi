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

![Market role Architecture](https://user-images.githubusercontent.com/72008816/160091353-afb253c0-ba98-424d-9821-4e895da0a1cf.png)

## Context Streams

![Market role domain context](https://user-images.githubusercontent.com/72008816/160091489-023a18f5-9c78-4c10-99b8-7b32383c9858.png)

Events sent from market role to other domains

![image](https://user-images.githubusercontent.com/72008816/182849372-8fae47d9-3561-4e5a-99dc-1a8681874977.png)


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

In this program increment we are working on

- A energy supplier can receive responses in CIM JSON format
- The future energy supplier can receive customer master data

## Getting Started

[Read here how to get started](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/docs/getting-started.md).

## Where can I get more help?

Please see the [community documentation](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/COMMUNITY.md)
