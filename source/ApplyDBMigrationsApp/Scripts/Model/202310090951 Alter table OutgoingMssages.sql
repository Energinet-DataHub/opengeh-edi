BEGIN
ALTER table outgoingmessages add OriginalData nvarchar(max) null;
GO;
update OutgoingMessages set originaldata = '{}';
GO;
alter table outgoingmessages alter column OriginalData nvarchar(max) not null;
GO;
END	