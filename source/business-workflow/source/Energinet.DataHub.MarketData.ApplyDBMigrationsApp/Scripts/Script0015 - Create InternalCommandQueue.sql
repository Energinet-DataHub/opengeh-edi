create table InternalCommandQueue
(
    Id int identity
        constraint InternalCommandQueue_pk
            primary key nonclustered,
    Data text not null,
    Type text not null,
    ScheduledDate DATETIME2(1),
    ProcessedDate DATETIME2(1),
)
go