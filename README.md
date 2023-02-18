# EDI domain

[![codecov](https://codecov.io/gh/Energinet-DataHub/geh-market-roles/branch/main/graph/badge.svg?token=R80X7DC6C0)](https://codecov.io/gh/Energinet-DataHub/geh-market-roles)

## Intro

The EDI domain is responsible for handling incoming and outgoing message to and from DataHub.
EDI domain receives incoming requests from actor and performs B2B validations on the request.
The request is then forwarded to relevant domain.
When a actor wished to peek a message from DataHub. EDI is responsible for generating the message, and ensuring that the correct actor receives the message

## Architecture

![structurizr-EDI-Container](https://user-images.githubusercontent.com/72008816/215046778-938cf41d-b6ba-4086-807a-7a5dc9460d9c.png)

## Business diagram of EDI

![image](https://user-images.githubusercontent.com/72008816/215047284-652c90d7-7e50-408f-b3ce-93f58ea62929.png)

## Supported formats

Currently we support the CIM XML format. The EDI domain will also support the following incoming and outgoing formats: CIM JSON and Ebix.

## Domain Roadmap

In this program increment we are working on

- Sending out calculation results for calculated flex consumption for the balance responsible party
- Creating acceptance test for testing message flow

In the next program increment we are working on

- Sending out calculation results for calculated flex consumption for the Balance supplier

## Getting Started

[Read here how to get started](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/docs/getting-started.md).

## Where can I get more help?

Please see the [community documentation](https://github.com/Energinet-DataHub/green-energy-hub/blob/main/COMMUNITY.md)
