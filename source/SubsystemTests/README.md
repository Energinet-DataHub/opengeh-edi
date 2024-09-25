# Subsystem Tests

## Getting started

In order to run the subsystem tests locally, you need to setup local settings to point at the target environment and an azp token.

* Make a copy of subsystem.local.settings.sample.json called subsystem.local.settings.json
    * Update the settings with the excepted values
        * SHARED_KEYVAULT_NAME: key vault from the target environment
        * AZP_TOKEN: target token
* Log in to the Azure CLI using the az login command.
    * Find Azure CLI installation guidance here: <https://learn.microsoft.com/en-us/cli/azure/install-azure-cli>
* Connect to the VPN
* Run Subsystem tests
