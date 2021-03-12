CREATE VIEW [dbo].[MeteringPointsRegisteredAsGridLossOrSystemCorrections] AS
    SELECT
        md.*,
        CONVERT(BIT, 1) AS IsGridLoss,
        CONVERT(BIT, 0) AS IsSystemCorrection
    FROM
        [dbo].[MOCK_master_data] md
    WHERE
            md.MarketEvaluationPoint_mRID IN (
                                              '578710000000000208',
                                              '578710000000000214',
                                              '578710000000000227',
                                              '578710000000000232',
                                              '578710000000000244',
                                              '578710000000000250',
                                              '578710000000000268',
                                              '578710000000000274'
            )
    UNION
    SELECT
        md.*,
        CONVERT(BIT, 0) AS IsGridLoss,
        CONVERT(BIT, 1) AS IsSystemCorrection
    FROM
        [dbo].[MOCK_master_data] md
    WHERE
            md.MarketEvaluationPoint_mRID IN (
                                              '578710000000000220',
                                              '578710000000000238',
                                              '578710000000000242',
                                              '578710000000000256',
                                              '578710000000000262',
                                              '578710000000000280',
                                              '578710000000000286'
            )
GO