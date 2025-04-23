-- Step 1: Add the PaginationCursorValue column
ALTER TABLE [dbo].[MeteringPointArchivedMessages]
    ADD [PaginationCursorValue] BIGINT NOT NULL IDENTITY(1,1);

-- Step 2: Create a non-clustered index on the PaginationCursorValue column
CREATE NONCLUSTERED INDEX IX_MeteringPointArchivedMessages_PaginationCursorValue
ON [dbo].[MeteringPointArchivedMessages]([PaginationCursorValue] ASC);