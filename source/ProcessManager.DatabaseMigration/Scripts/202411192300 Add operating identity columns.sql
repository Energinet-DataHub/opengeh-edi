ALTER TABLE [pm].[OrchestrationInstance]
    ADD [Lifecycle_CreatedBy_IdentityType]  NVARCHAR(255) NULL,
        [Lifecycle_CreatedBy_ActorId]       UNIQUEIDENTIFIER NULL,
        [Lifecycle_CreatedBy_UserId]        UNIQUEIDENTIFIER NULL,
        [Lifecycle_CanceledBy_IdentityType] NVARCHAR(255) NULL,
        [Lifecycle_CanceledBy_ActorId]      UNIQUEIDENTIFIER NULL,
        [Lifecycle_CanceledBy_UserId]       UNIQUEIDENTIFIER NULL
GO
