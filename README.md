# EDI domain

[![codecov](https://codecov.io/gh/Energinet-DataHub/geh-market-roles/branch/main/graph/badge.svg?token=R80X7DC6C0)](https://codecov.io/gh/Energinet-DataHub/geh-market-roles)

## Intro

The EDI domain is responsible for handling incoming and outgoing message to and from DataHub.
EDI domain receives incoming requests from actor and performs B2B validations on the request.
The request is then forwarded to relevant domain.
When a actor wished to peek a message from DataHub. EDI is responsible for generating the message, and ensuring that the correct actor receives the message

## Architecture

![image](https://github.com/Energinet-DataHub/opengeh-edi/blob/main/docs/diagrams/edi/Container-001.png?raw=true)

## Business diagram of EDI

![image](https://user-images.githubusercontent.com/72008816/215047284-652c90d7-7e50-408f-b3ce-93f58ea62929.png)

## Supported formats

Currently we support the CIM XML format. The EDI domain will also support the following incoming and outgoing formats: CIM JSON and Ebix.

## Domain Roadmap

In this sprint we are working on

- Be able to search for outgoing messages in frontend

In the next sprint we are working on

- Support new proces type
- New outgoing message. Net exchange pr. grid

## Getting Started

[Read here how to get started](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/docs/getting-started.md).

## Where can I get more help?

Please see the [community documentation](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/COMMUNITY.md)

## Domain C4 model

In the DataHub 3 project we use the [C4 model](https://c4model.com/) to document the high-level software design.

The [DataHub 3 base model](https://github.com/Energinet-DataHub/opengeh-arch-diagrams#datahub-3-base-model) describes elements like organizations, software systems and actors. In domain repositories we should `extend` on this model and add additional elements within the DataHub 3.0 Software System (`dh3`).

The domain C4 model and rendered diagrams are located in the folder hierarchy [docs/diagrams/c4-model](./docs/diagrams/c4-model/) and consists of:

- `model.dsl`: Structurizr DSL describing the domain C4 model.
- `views.dsl`: Structurizr DSL extending the `dh3` software system by referencing domain C4 models using `!include`, and describing the views.
- `views.json`: Structurizr layout information for views.
- `/views/*.png`: A PNG file per view described in the Structurizr DSL.

Maintenance of the C4 model should be performed using VS Code and a local version of Structurizr Lite running in Docker. See [DataHub 3 base model](https://github.com/Energinet-DataHub/opengeh-arch-diagrams#datahub-3-base-model) for a description of how to do this.
