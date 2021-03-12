ALTER VIEW [dbo].[ODSMasterDataEnrichment] AS
select
    MarketEvaluationPoint_mRID,
    _ValidFrom as ValidFrom,
    _ValidTo as ValidTo,
    MeterReadingPeriodicity,
    MeteringMethod,
    MeteringGridArea_Domain_mRID,
    ConnectionState,
    EnergySupplier_MarketParticipant_mRID,
    BalanceResponsibleParty_MarketParticipant_mRID,
    InMeteringGridArea_Domain_mRID,
    OutMeteringGridArea_Domain_mRID,
    Parent_Domain_mRID,
    ServiceCategory_Kind,
    MarketEvaluationPointType,
    SettlementMethod,
    QuantityMeasurementUnit_Name,
    Product,
    Technology,
    OutMeteringGridArea_Domain_Owner_mRID,
    InMeteringGridArea_Domain_Owner_mRID,
    CONCAT(
            '[',
            CONCAT_WS(
                    ',',
                    dl_energysupplier,
                    dl_parent_energysupplier,
                    dl_sts,
                    dl_ez,
                    dl_out_ddm,
                    dl_in_ddm),
            ']') as DistributionList,
    Meta
from (
         SELECT
             e.*,
             case when e.ValidFrom >= coalesce(parent.Validfrom, e.ValidFrom) then e.ValidFrom else parent.ValidFrom end as _ValidFrom,
		case when e.ValidTo > parent.ValidTo then parent.ValidTo else e.ValidTo end as _ValidTo,
		
		case
			when e.EnergySupplier_MarketParticipant_mRID is not null then '{"mRID":"'+e.EnergySupplier_MarketParticipant_mRID+'","role":"DDQ"}'
		end
		as dl_energysupplier,

		case
			when e.Parent_Domain_mRID is not null and e.MarketEvaluationPointType <> 'D20'
			then '{"mRID":"'+parent.EnergySupplier_MarketParticipant_mRID+'","role":"DDQ"}'
		end
		as dl_parent_energysupplier,

		case when (e.MarketEvaluationPointType) in ('E18', 'D01', 'D04') then '{"mRID":"5790001330584","role":"STS"}' end as dl_sts,

		case when e.MarketEvaluationPointType = 'D02' then '{"mRID":"5790000432752","role":"EZ"}' end as dl_ez,

		case when (e.MarketEvaluationPointType) in ('E20', 'D20') then '{"mRID":"'+e.OutMeteringGridArea_Domain_Owner_mRID+'","role":"DDM"}' end as dl_out_ddm,
		case when (e.MarketEvaluationPointType) in ('E20', 'D20') then '{"mRID":"'+e.InMeteringGridArea_Domain_Owner_mRID+'","role":"DDM"}' end as dl_in_ddm

         FROM dbo.MOCK_master_data as e
             -- Self join with parent and create new rows when parent valid period splits child valid period
             left outer join dbo.ODSMasterDataEnrichment_manual as parent
         on e.Parent_Domain_mRID = parent.MarketEvaluationPoint_mRID
             and (e.ValidFrom < parent.ValidTo or parent.ValidTo is null) and (parent.ValidFrom < e.ValidTo or e.ValidTo is null)
     ) foo
GO