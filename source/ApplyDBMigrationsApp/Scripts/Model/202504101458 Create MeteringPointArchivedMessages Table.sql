-- Step 1: Create a Partition Function
-- Define monthly ranges for the next 4 years
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
    '2027-09-01 00:00:00', '2027-10-01 00:00:00', '2027-11-01 00:00:00', '2027-12-01 00:00:00'
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
    -- Sync validation rule that prevents the use of a messageId that is longer than 36 characters
    [MessageId] [varchar](36) NULL, 
    -- {actorNumber}/{year:0000}/{month:00}/{day:00}/{id.ToString("N")} => 16 + 1 + 4 + 1 + 2 + 1 + 2 + 1 + 32 = 60
    [FileStorageReference] [varchar](60) NOT NULL, 
    -- Sync validation rule that prevents the use of a messageId that is longer than 36 characters
    [RelatedToMessageId] [varchar](36) NULL, 
    -- Size is limited to 4000 characters (4,000 ÷ 36 ≈ 111 GUIDs).
    [EventIds] varchar(4000) NULL, 
    -- Size: MaxBundleDataCount is 150000, the minimum amount of dataCount for a MeteringPointId is 4(quarterly) * 4(hours) = 16 
    -- 150.000 / 16 = 9375 (max metering point ids per bundle)
    -- In Json Format: (amount of guids * (GUID_Length + double quotes + comma separator)) + array brackets
    -- (9375 * (36 + 2 + 1)) + 2 = 337,500 --> 337500 exceeds the 8,060-byte in-row limit, so the data will be stored off-row = max
    [MeteringPointIds] [varchar](max) NULL,
    CONSTRAINT [PK_MeteringPointArchivedMessages_Id] PRIMARY KEY NONCLUSTERED ([Id] ASC)
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