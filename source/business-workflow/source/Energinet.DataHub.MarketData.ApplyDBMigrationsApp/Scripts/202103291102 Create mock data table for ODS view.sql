CREATE TABLE [dbo].[MOCK_master_data]
(
    [MarketEvaluationPoint_mRID]                        NVARCHAR(18)    NOT NULL,
    [ValidFrom]                                         DATETIME        NOT NULL,
    [ValidTo]                                           DATETIME        NULL,
    [MeterReadingPeriodicity]                           NVARCHAR(6)     NULL,
    [MeteringMethod]                                    NVARCHAR(3)     NULL,
    [MeteringGridArea_Domain_mRID]                      NVARCHAR(3)     NULL,
    [ConnectionState]                                   NVARCHAR(3)     NULL,
    [EnergySupplier_MarketParticipant_mRID]             NVARCHAR(18)    NULL,
    [BalanceResponsibleParty_MarketParticipant_mRID]    NVARCHAR(18)    NULL,
    [InMeteringGridArea_Domain_mRID]                    NVARCHAR(18)    NULL,
    [OutMeteringGridArea_Domain_mRID]                   NVARCHAR(18)    NULL,
    [Parent_Domain_mRID]                                NVARCHAR(18)    NULL,
    [ServiceCategory_Kind]                              BIT             NULL,
    [MarketEvaluationPointType]                         NVARCHAR(3)     NULL,
    [SettlementMethod]                                  NVARCHAR(3)     NULL,
    [QuantityMeasurementUnit_Name]                      NVARCHAR(3)     NULL,
    [Product]                                           NVARCHAR(18)    NULL,
    [Technology]                                        NVARCHAR(3)     NULL,
    [OutMeteringGridArea_Domain_Owner_mRID]             NVARCHAR(18)    NULL,
    [InMeteringGridArea_Domain_Owner_mRID]              NVARCHAR(18)    NULL,
    [Meta]                                              NVARCHAR(MAX)   NULL
)
GO