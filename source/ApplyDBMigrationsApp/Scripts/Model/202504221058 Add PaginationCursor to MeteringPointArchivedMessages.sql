-- Step 1: Add the PaginationCursor column
ALTER TABLE [dbo].[MeteringPointArchivedMessages]
    ADD [PaginationCursor] BIGINT NOT NULL IDENTITY(1,1);

-- Step 2: Create a non-clustered index on the PaginationCursor column
CREATE NONCLUSTERED INDEX IX_MeteringPointArchivedMessages_PaginationCursor
ON [dbo].[MeteringPointArchivedMessages]([PaginationCursor] ASC);