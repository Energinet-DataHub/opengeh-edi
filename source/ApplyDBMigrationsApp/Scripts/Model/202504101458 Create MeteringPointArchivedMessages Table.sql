-- Step 1: Create a Partition Function
-- Define monthly ranges for the next 12 years
CREATE PARTITION FUNCTION PF_CreatedAt (DATETIME2)
AS RANGE RIGHT FOR VALUES
(
    '2024-05-01 00:00:00', '2024-06-01 00:00:00', '2024-07-01 00:00:00', '2024-08-01 00:00:00',
    '2024-09-01 00:00:00', '2024-10-01 00:00:00', '2024-11-01 00:00:00', '2024-12-01 00:00:00',
    '2025-01-01 00:00:00', '2025-02-01 00:00:00', '2025-03-01 00:00:00', '2025-04-01 00:00:00',
    '2025-05-01 00:00:00', '2025-06-01 00:00:00', '2025-07-01 00:00:00', '2025-08-01 00:00:00',
    '2025-09-01 00:00:00', '2025-10-01 00:00:00', '2025-11-01 00:00:00', '2025-12-01 00:00:00',
    '2026-01-01 00:00:00', '2026-02-01 00:00:00', '2026-03-01 00:00:00', '2026-04-01 00:00:00',
    '2026-05-01 00:00:00', '2026-06-01 00:00:00', '2026-07-01 00:00:00', '2026-08-01 00:00:00',
    '2026-09-01 00:00:00', '2026-10-01 00:00:00', '2026-11-01 00:00:00', '2026-12-01 00:00:00',
    '2027-01-01 00:00:00', '2027-02-01 00:00:00', '2027-03-01 00:00:00', '2027-04-01 00:00:00',
    '2027-05-01 00:00:00', '2027-06-01 00:00:00', '2027-07-01 00:00:00', '2027-08-01 00:00:00',
    '2027-09-01 00:00:00', '2027-10-01 00:00:00', '2027-11-01 00:00:00', '2027-12-01 00:00:00',
    '2028-01-01 00:00:00', '2028-02-01 00:00:00', '2028-03-01 00:00:00', '2028-04-01 00:00:00',
    '2028-05-01 00:00:00', '2028-06-01 00:00:00', '2028-07-01 00:00:00', '2028-08-01 00:00:00',
    '2028-09-01 00:00:00', '2028-10-01 00:00:00', '2028-11-01 00:00:00', '2028-12-01 00:00:00',
    '2029-01-01 00:00:00', '2029-02-01 00:00:00', '2029-03-01 00:00:00', '2029-04-01 00:00:00',
    '2029-05-01 00:00:00', '2029-06-01 00:00:00', '2029-07-01 00:00:00', '2029-08-01 00:00:00',
    '2029-09-01 00:00:00', '2029-10-01 00:00:00', '2029-11-01 00:00:00', '2029-12-01 00:00:00',
    '2030-01-01 00:00:00', '2030-02-01 00:00:00', '2030-03-01 00:00:00', '2030-04-01 00:00:00',
    '2030-05-01 00:00:00', '2030-06-01 00:00:00', '2030-07-01 00:00:00', '2030-08-01 00:00:00',
    '2030-09-01 00:00:00', '2030-10-01 00:00:00', '2030-11-01 00:00:00', '2030-12-01 00:00:00',
    '2031-01-01 00:00:00', '2031-02-01 00:00:00', '2031-03-01 00:00:00', '2031-04-01 00:00:00',
    '2031-05-01 00:00:00', '2031-06-01 00:00:00', '2031-07-01 00:00:00', '2031-08-01 00:00:00',
    '2031-09-01 00:00:00', '2031-10-01 00:00:00', '2031-11-01 00:00:00', '2031-12-01 00:00:00',
    '2032-01-01 00:00:00', '2032-02-01 00:00:00', '2032-03-01 00:00:00', '2032-04-01 00:00:00',
    '2032-05-01 00:00:00', '2032-06-01 00:00:00', '2032-07-01 00:00:00', '2032-08-01 00:00:00',
    '2032-09-01 00:00:00', '2032-10-01 00:00:00', '2032-11-01 00:00:00', '2032-12-01 00:00:00',
    '2033-01-01 00:00:00', '2033-02-01 00:00:00', '2033-03-01 00:00:00', '2033-04-01 00:00:00',
    '2033-05-01 00:00:00', '2033-06-01 00:00:00', '2033-07-01 00:00:00', '2033-08-01 00:00:00',
    '2033-09-01 00:00:00', '2033-10-01 00:00:00', '2033-11-01 00:00:00', '2033-12-01 00:00:00',
    '2034-01-01 00:00:00', '2034-02-01 00:00:00', '2034-03-01 00:00:00', '2034-04-01 00:00:00',
    '2034-05-01 00:00:00', '2034-06-01 00:00:00', '2034-07-01 00:00:00', '2034-08-01 00:00:00',
    '2034-09-01 00:00:00', '2034-10-01 00:00:00', '2034-11-01 00:00:00', '2034-12-01 00:00:00',
    '2035-01-01 00:00:00', '2035-02-01 00:00:00', '2035-03-01 00:00:00', '2035-04-01 00:00:00',
    '2035-05-01 00:00:00', '2035-06-01 00:00:00', '2035-07-01 00:00:00', '2035-08-01 00:00:00',
    '2035-09-01 00:00:00', '2035-10-01 00:00:00', '2035-11-01 00:00:00', '2035-12-01 00:00:00'
);

-- Step 2: Create a Partition Scheme
-- Map all partitions to the primary filegroup
CREATE PARTITION SCHEME PS_CreatedAt
AS PARTITION PF_CreatedAt
ALL TO ([PRIMARY]);

-- Step 4: Create the Partitioned Table
-- Use the partition scheme for the table
CREATE TABLE [dbo].[MeteringPointArchivedMessages](
    [RecordId] [bigint] IDENTITY(1,1) NOT NULL,
    [Id] [uniqueidentifier] NOT NULL,
    [DocumentType] TINYINT NOT NULL,
    -- CIM has a maxLength of 16 for this field, but Ebix does not have a size limit
    [ReceiverNumber] [varchar](255) NULL,
    [ReceiverRoleCode] TINYINT NULL,
    -- CIM has a maxLength of 16 for this field, but Ebix does not have a size limit
    [SenderNumber] [varchar](255) NULL,
    [SenderRoleCode] TINYINT NULL,
    [CreatedAt] [datetime2](7) NOT NULL,
    [BusinessReason] TINYINT NOT NULL,
    -- Sync validation rule prevents the use of a messageId that is longer than 36 characters
    [MessageId] [varchar](36) NULL, 
    -- {actorNumber}/{year:0000}/{month:00}/{day:00}/{id.ToString("N")} => 16 + 1 + 4 + 1 + 2 + 1 + 2 + 1 + 32 = 60
    [FileStorageReference] [varchar](60) NOT NULL, 
    -- Sync validation rule prevents the use of a messageId that is longer than 36 characters
    [RelatedToMessageId] [varchar](36) NULL, 
    -- Size is limited to 4000 characters (4,000 ÷ 36 ≈ 111 GUIDs).
    [EventIds] varchar(4000) NULL, 
    -- Size: MaxBundleDataCount is 150000, the minimum amount of dataCount for a MeteringPointId is 4(quarterly) * 4(hours) = 16 
    -- 150.000 / 16 = 9375 (max metering point ids per bundle)
    -- In Json Format: (amount of guids * (MeteringPointId_Length + double quotes + comma separator)) + array brackets
    -- (9375 * (36 + 2 + 1)) + 2 = 337,500 --> 337500 exceeds the 8,060-byte in-row limit, so the data will be stored off-row = max
    [MeteringPointIds] [varchar](max) NOT NULL,
    CONSTRAINT [PK_MeteringPointArchivedMessages_Id] PRIMARY KEY NONCLUSTERED ([Id] ASC, [CreatedAt] ASC)
    ) ON PS_CreatedAt(CreatedAt);

-- Create a composite index for combined queries
CREATE NONCLUSTERED INDEX IX_MeteringPointArchivedMessages_Optimized
ON [dbo].[MeteringPointArchivedMessages](
    CreatedAt DESC,
    DocumentType,
    ReceiverNumber,
    ReceiverRoleCode,
    SenderNumber,
    SenderRoleCode
)
INCLUDE (Id, MessageId, BusinessReason, FileStorageReference);