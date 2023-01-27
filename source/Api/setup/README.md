# Setup Guide

This guide will describe how to setup a local development environment for the Messaging.Api

## Prerequisites

* An Azure account
* Locally installed Azure Functions Core Tools
* Locally installed Azurite service (Microsoft Azure Storage Emulator)
* Locally installed SQL database
* Powershell (on mac, it can be installed using: "brew install --cask powershell")

## How to create personal Azure resources

In "source/Messaging.Api/setup"...

Run the Powershell script "setupazure.ps1" with the following parameters:

"./setupazure.ps1 -username yourUserName -subscription sub-xxx-yyy-zzz"

Request the "sub-xxx-yyy-zzz" from your team.

If necessary run: "PowerShell.exe -ExecutionPolicy Bypass -File ./setupazure.ps1"

## Configuring a personal: local.settings.json

* Copy the Messaging.Api/local.settings.sample.json to Messaging.Api/local.settings.json
* Setup the variables in local.settings.json to match the resources created in Azure and the connection string of your locally running SQL database.

Tip: Request an the "local.settings.json" from your team.

## Starting the service

* Make sure the Azurite service is running in the background
* Start the Messaging.Api Function App in your IDE
