DECLARE @DefaultConstraintName nvarchar(50)
select @DefaultConstraintName = Name from sys.default_constraints
where parent_column_id = (SELECT column_id FROM sys.columns
                          WHERE NAME = 'CorrelationId'
                            AND object_id = OBJECT_ID('b2b.OutgoingMessages')) and type = 'D'

    EXEC('ALTER TABLE [b2b].[OutgoingMessages] DROP CONSTRAINT ' + @DefaultConstraintName)
GO
ALTER TABLE [b2b].[OutgoingMessages]
DROP COLUMN CorrelationId
GO