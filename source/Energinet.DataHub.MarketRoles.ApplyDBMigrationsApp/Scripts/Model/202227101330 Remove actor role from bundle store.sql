ALTER TABLE [b2b].[BundleStore]
    DROP CONSTRAINT PK_Id
ALTER TABLE [b2b].[BundleStore]
    DROP COLUMN [ActorRole]
ALTER TABLE [b2b].[BundleStore]
    ADD CONSTRAINT [PK_Id] PRIMARY KEY NONCLUSTERED
        (
        [ActorNumber],
        [MessageCategory] 
        ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY];