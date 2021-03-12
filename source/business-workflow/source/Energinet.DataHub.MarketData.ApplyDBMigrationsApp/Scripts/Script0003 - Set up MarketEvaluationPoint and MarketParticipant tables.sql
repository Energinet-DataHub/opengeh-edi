SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS(SELECT *
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_NAME = N'MarketEvaluationPoints')
    BEGIN
        CREATE TABLE [dbo].[MarketEvaluationPoints]
        (
            [Id]                  [int] IDENTITY (1,1) NOT NULL,
            [GsrnNumber]          [nvarchar](36)       NOT NULL,
            [ProductionObligated] [bit]                NOT NULL,
            [PhysicalState]       [int]                NOT NULL,
            [Type]                [int]                NOT NULL,
            [RowVersion]          [int]                NOT NULL,
            CONSTRAINT [Pk_MarketEvaluationPoint_Id] PRIMARY KEY CLUSTERED
                (
                 [Id] ASC
                    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [CK_MarketEvaluationPoint_Gsrn] UNIQUE NONCLUSTERED
                (
                 [GsrnNumber] ASC
                    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
GO
/****** Object:  Table [dbo].[MarketParticipants]    Script Date: 2/8/2021 4:18:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS(SELECT *
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_NAME = N'MarketParticipants')
    BEGIN
        CREATE TABLE [dbo].[MarketParticipants]
        (
            [Id]         [int] IDENTITY (1,1) NOT NULL,
            [MrId]       [nvarchar](50)       NOT NULL,
            [RowVersion] [int]                NOT NULL,
            CONSTRAINT [Pk_MarketParticipant_Id] PRIMARY KEY CLUSTERED
                (
                 [Id] ASC
                    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [CK_MarketParticipant_Mrid] UNIQUE NONCLUSTERED
                (
                 [MrId] ASC
                    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
GO
/****** Object:  Table [dbo].[Relationships]    Script Date: 2/8/2021 4:18:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS(SELECT *
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_NAME = N'Relationships')
    BEGIN
        CREATE TABLE [dbo].[Relationships]
        (
            [Id]                       [int] IDENTITY (1,1) NOT NULL,
            [MarketParticipant_Id]     [int]                NOT NULL,
            [MarketEvaluationPoint_Id] [int]                NOT NULL,
            [Type]                     [int]                NOT NULL,
            [EffectuationDate]         [datetime2](2)       NOT NULL,
            [State]                    [int]                NOT NULL
        ) ON [PRIMARY]
        ALTER TABLE [dbo].[Relationships]
            WITH CHECK ADD CONSTRAINT [Fk_Relationship_MarketEvaluationPoint] FOREIGN KEY ([MarketEvaluationPoint_Id])
                REFERENCES [dbo].[MarketEvaluationPoints] ([Id])
        ALTER TABLE [dbo].[Relationships]
            CHECK CONSTRAINT [Fk_Relationship_MarketEvaluationPoint]
        ALTER TABLE [dbo].[Relationships]
            WITH CHECK ADD CONSTRAINT [Fk_Relationship_MarketParticipant] FOREIGN KEY ([MarketParticipant_Id])
                REFERENCES [dbo].[MarketParticipants] ([Id])
        ALTER TABLE [dbo].[Relationships]
            CHECK CONSTRAINT [Fk_Relationship_MarketParticipant]
    END
GO
