ALTER TABLE [b2b].[Transactions]
    ADD [State] [nvarchar](50)
    CONSTRAINT DF_State DEFAULT 'Started' NOT NULL
declare @name nvarchar(100)
select @name = [name] from sys.objects where type = 'D' and name like 'DF__Transacti__Start__%' and parent_object_id = object_id('b2b.transactions')

    if (@name is not null)
begin
exec ('alter table b2b.transactions drop constraint [' + @name +']')
end
go
alter table b2b.transactions drop column if exists started
go
EXEC sp_rename 'b2b.Transactions', 'MoveInTransactions';