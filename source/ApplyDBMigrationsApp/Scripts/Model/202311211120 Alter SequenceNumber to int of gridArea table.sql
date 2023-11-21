DECLARE @ConstraintName nvarchar(200)
SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS
WHERE PARENT_OBJECT_ID = OBJECT_ID('GridAreaOwner')
  AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns
                          WHERE NAME = N'SequenceNumber'
                            AND object_id = OBJECT_ID(N'GridAreaOwner'))
    IF @ConstraintName IS NOT NULL
EXEC('ALTER TABLE GridAreaOwner DROP CONSTRAINT ' + @ConstraintName)

ALTER TABLE [dbo].[GridAreaOwner] ALTER COLUMN [SequenceNumber] INT NOT NULL;