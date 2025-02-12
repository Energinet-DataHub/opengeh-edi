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

using Azure.Storage.Blobs;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Activities;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Database;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using HttpClientFactory = Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks.HttpClientFactory;

namespace Energinet.DataHub.EDI.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IDisposable, IAsyncLifetime
{
    private bool _disposed;
    private bool _databricksWholesaleDataInserted;
    private bool _databricksAggregatedMeasureDataInserted;

    public IntegrationTestFixture()
    {
        IntegrationTestConfiguration = new IntegrationTestConfiguration();

        DatabaseManager = new EdiDatabaseManager("IntegrationTests");

        DatabricksSchemaManager = new DatabricksSchemaManager(
            new HttpClientFactory(),
            databricksSettings: IntegrationTestConfiguration.DatabricksSettings,
            schemaPrefix: "edi_integration_tests");
    }

    public EdiDatabaseManager DatabaseManager { get; set; }

    public AzuriteManager AzuriteManager { get; } = new(true);

    public IntegrationTestConfiguration IntegrationTestConfiguration { get; }

    public DatabricksSchemaManager DatabricksSchemaManager { get; }

    public async Task InitializeAsync()
    {
        await DatabricksSchemaManager.CreateSchemaAsync();
        await DatabaseManager.CreateDatabaseAsync();
        AzuriteManager.StartAzurite();
        CleanupFileStorage();
    }

    public async Task DisposeAsync()
    {
        if (_databricksWholesaleDataInserted || _databricksAggregatedMeasureDataInserted)
            await DatabricksSchemaManager.DropSchemaAsync();
        Dispose();
        await Task.CompletedTask;
    }

    public void CleanupFileStorage(bool disposing = false)
    {
        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);

        var containers = blobServiceClient.GetBlobContainers();

        foreach (var containerToDelete in containers)
        {
            blobServiceClient.DeleteBlobContainer(containerToDelete.Name);
        }

        if (disposing)
        {
            // Cleanup actual Azurite "database" files
            if (Directory.Exists("__blobstorage__"))
                Directory.Delete("__blobstorage__", true);

            if (Directory.Exists("__queuestorage__"))
                Directory.Delete("__queuestorage__", true);

            if (Directory.Exists("__tablestorage__"))
                Directory.Delete("__tablestorage__", true);

            if (File.Exists("__azurite_db_blob__.json"))
                File.Delete("__azurite_db_blob__.json");

            if (File.Exists("__azurite_db_blob_extent__.json"))
                File.Delete("__azurite_db_blob_extent__.json");

            if (File.Exists("__azurite_db_queue__.json"))
                File.Delete("__azurite_db_queue__.json");

            if (File.Exists("__azurite_db_queue_extent__.json"))
                File.Delete("__azurite_db_queue_extent__.json");

            if (File.Exists("__azurite_db_table__.json"))
                File.Delete("__azurite_db_table__.json");

            if (File.Exists("__azurite_db_table_extent__.json"))
                File.Delete("__azurite_db_table_extent__.json");
        }
        else
        {
            CreateRequiredContainers();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected internal async Task InsertWholesaleDataDatabricksDataAsync(IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        if (_databricksWholesaleDataInserted)
            return;
        await GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreasAsync(ediDatabricksOptions);
        await GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerChargeAsync(ediDatabricksOptions);
        await GivenDatabricksResultDataForWholesaleResultTotalAmountAsync(ediDatabricksOptions);

        _databricksWholesaleDataInserted = true;
    }

    protected internal async Task InsertAggregatedMeasureDataDatabricksDataAsync(IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        if (_databricksAggregatedMeasureDataInserted)
            return;
        await GivenDatabricksResultDataForEnergyResultPerGridAreaAsync(ediDatabricksOptions);
        await GivenDatabricksResultDataForEnergyResultPerBalanceResponsibleAsync(ediDatabricksOptions);
        await GivenDatabricksResultDataForEnergyResultPerEnergySupplierAsync(ediDatabricksOptions);

        _databricksAggregatedMeasureDataInserted = true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CleanupFileStorage(true);
            DatabaseManager.DeleteDatabase();
            AzuriteManager.Dispose();
        }

        _disposed = true;
    }

    private void CreateRequiredContainers()
    {
        List<FileStorageCategory> containerCategories = [
            FileStorageCategory.ArchivedMessage(),
            FileStorageCategory.OutgoingMessage(),
        ];

        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);
        foreach (var fileStorageCategory in containerCategories)
        {
            var container = blobServiceClient.GetBlobContainerClient(fileStorageCategory.Value);
            var containerExists = container.Exists();

            if (!containerExists)
                container.Create();
        }
    }

    private async Task GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreasAsync(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        var wholesaleResultForAmountPerChargeInTwoGridAreasDescription = new WholesaleResultForAmountPerChargeInTwoGridAreasDescription();
        var wholesaleAmountPerChargeQuery = new WholesaleAmountPerChargeQuery(
            new LoggerSpy(),
            ediDatabricksOptions.Value,
            wholesaleResultForAmountPerChargeInTwoGridAreasDescription.GridAreaOwners,
            EventId.From(Guid.NewGuid()),
            wholesaleResultForAmountPerChargeInTwoGridAreasDescription.CalculationId,
            null);

        await DatabricksSchemaManager.CreateTableAsync(wholesaleAmountPerChargeQuery.DataObjectName, wholesaleAmountPerChargeQuery.SchemaDefinition);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleAmountPerChargeQuery.DataObjectName, wholesaleAmountPerChargeQuery.SchemaDefinition, wholesaleResultForAmountPerChargeInTwoGridAreasDescription.TestFilePath);
    }

    private async Task GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerChargeAsync(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        var wholesaleResultForMonthlyAmountPerChargeDescription = new WholesaleResultForMonthlyAmountPerChargeDescription();
        var wholesaleMonthlyAmountPerChargeQuery = new WholesaleMonthlyAmountPerChargeQuery(
            new LoggerSpy(),
            ediDatabricksOptions.Value,
            wholesaleResultForMonthlyAmountPerChargeDescription.GridAreaOwners,
            EventId.From(Guid.NewGuid()),
            wholesaleResultForMonthlyAmountPerChargeDescription.CalculationId,
            null);

        await DatabricksSchemaManager.CreateTableAsync(wholesaleMonthlyAmountPerChargeQuery.DataObjectName, wholesaleMonthlyAmountPerChargeQuery.SchemaDefinition);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleMonthlyAmountPerChargeQuery.DataObjectName, wholesaleMonthlyAmountPerChargeQuery.SchemaDefinition, wholesaleResultForMonthlyAmountPerChargeDescription.TestFilePath);
    }

    private async Task GivenDatabricksResultDataForWholesaleResultTotalAmountAsync(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        var resultDataForWholesaleResultTotalAmount = new WholesaleResultForTotalAmountDescription();
        var wholesaleTotalAmountQuery = new WholesaleTotalAmountQuery(
            new LoggerSpy(),
            ediDatabricksOptions.Value,
            resultDataForWholesaleResultTotalAmount.GridAreaOwners,
            EventId.From(Guid.NewGuid()),
            resultDataForWholesaleResultTotalAmount.CalculationId,
            null);

        await DatabricksSchemaManager.CreateTableAsync(wholesaleTotalAmountQuery.DataObjectName, wholesaleTotalAmountQuery.SchemaDefinition);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleTotalAmountQuery.DataObjectName, wholesaleTotalAmountQuery.SchemaDefinition, resultDataForWholesaleResultTotalAmount.TestFilePath);
    }

    private async Task GivenDatabricksResultDataForEnergyResultPerGridAreaAsync(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        var energyResultPerGridAreaTestDataDescription = new EnergyResultPerGridAreaDescription();
        var energyResultPerGridAreaQuery = new EnergyResultPerGridAreaQuery(
            new LoggerSpy(),
            ediDatabricksOptions.Value,
            energyResultPerGridAreaTestDataDescription.GridAreaOwners,
            EventId.From(Guid.NewGuid()),
            energyResultPerGridAreaTestDataDescription.CalculationId);

        await DatabricksSchemaManager.CreateTableAsync(energyResultPerGridAreaQuery.DataObjectName, energyResultPerGridAreaQuery.SchemaDefinition);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerGridAreaQuery.DataObjectName, energyResultPerGridAreaQuery.SchemaDefinition, energyResultPerGridAreaTestDataDescription.TestFilePath);
    }

    private async Task GivenDatabricksResultDataForEnergyResultPerBalanceResponsibleAsync(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        var energyResultPerBrpDescription = new EnergyResultPerBrpGridAreaDescription();
        var energyResultPerBrpQuery = new EnergyResultPerBalanceResponsiblePerGridAreaQuery(
            new LoggerSpy(),
            ediDatabricksOptions.Value,
            EventId.From(Guid.NewGuid()),
            energyResultPerBrpDescription.CalculationId);

        await DatabricksSchemaManager.CreateTableAsync(energyResultPerBrpQuery.DataObjectName, energyResultPerBrpQuery.SchemaDefinition);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerBrpQuery.DataObjectName, energyResultPerBrpQuery.SchemaDefinition, energyResultPerBrpDescription.TestFilePath);
    }

    private async Task GivenDatabricksResultDataForEnergyResultPerEnergySupplierAsync(
        IOptions<EdiDatabricksOptions> ediDatabricksOptions)
    {
        var energyResultPerEnergySupplierDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        var energyResultPerEnergySupplierQuery = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(
            new LoggerSpy(),
            ediDatabricksOptions.Value,
            EventId.From(Guid.NewGuid()),
            energyResultPerEnergySupplierDescription.CalculationId);

        await DatabricksSchemaManager.CreateTableAsync(energyResultPerEnergySupplierQuery.DataObjectName, energyResultPerEnergySupplierQuery.SchemaDefinition);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerEnergySupplierQuery.DataObjectName, energyResultPerEnergySupplierQuery.SchemaDefinition, energyResultPerEnergySupplierDescription.TestFilePath);
    }
}
