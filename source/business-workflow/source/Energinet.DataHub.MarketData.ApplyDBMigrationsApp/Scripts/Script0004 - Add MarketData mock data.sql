SET IDENTITY_INSERT [dbo].[MarketEvaluationPoints] ON
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (1, N'571234567891234605', 0, 0, 0, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (2, N'571234567891234612', 0, 1, 0, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (3, N'571234567891234629', 0, 2, 0, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (4, N'571234567891234636', 0, 3, 0, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (5, N'571234567891234643', 0, 1, 1, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (6, N'571234567891234650', 1, 1, 1, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (7, N'571234567891234667', 1, 2, 1, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (8, N'571234567891234674', 0, 2, 1, 1)
GO
INSERT [dbo].[MarketEvaluationPoints] ([Id], [GsrnNumber], [ProductionObligated], [PhysicalState], [Type], [RowVersion]) VALUES (9, N'571234567891234681', 0, 1, 0, 1)
GO
SET IDENTITY_INSERT [dbo].[MarketEvaluationPoints] OFF
GO
SET IDENTITY_INSERT [dbo].[MarketParticipants] ON
GO
INSERT [dbo].[MarketParticipants] ([Id], [MrId], [RowVersion]) VALUES (1, N'5790001687137', 1)
GO
INSERT [dbo].[MarketParticipants] ([Id], [MrId], [RowVersion]) VALUES (2, N'5790001687144', 1)
GO
INSERT [dbo].[MarketParticipants] ([Id], [MrId], [RowVersion]) VALUES (3, N'5790001687151', 1)
GO
INSERT [dbo].[MarketParticipants] ([Id], [MrId], [RowVersion]) VALUES (4, N'0101800000', 1)
GO
INSERT [dbo].[MarketParticipants] ([Id], [MrId], [RowVersion]) VALUES (5, N'0102800000', 1)
GO
INSERT [dbo].[MarketParticipants] ([Id], [MrId], [RowVersion]) VALUES (6, N'88888888', 1)
GO
SET IDENTITY_INSERT [dbo].[MarketParticipants] OFF
GO
SET IDENTITY_INSERT [dbo].[Relationships] ON
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (1, 1, 1, 0, CAST(N'2021-02-01T07:30:20.0100000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (2, 2, 2, 0, CAST(N'2021-01-23T07:30:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (3, 3, 3, 0, CAST(N'2020-01-23T09:35:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (4, 1, 4, 0, CAST(N'2020-03-23T07:30:20.0100000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (5, 2, 5, 0, CAST(N'2020-06-23T07:30:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (6, 3, 6, 0, CAST(N'2021-01-23T09:35:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (7, 1, 7, 0, CAST(N'2021-01-21T07:30:20.0100000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (8, 2, 8, 0, CAST(N'2021-01-20T07:30:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (9, 3, 9, 0, CAST(N'2020-01-23T09:35:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (10, 4, 1, 1, CAST(N'2021-02-01T07:30:20.0100000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (11, 5, 2, 1, CAST(N'2021-01-23T07:30:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (12, 6, 3, 1, CAST(N'2020-01-23T09:35:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (13, 4, 4, 1, CAST(N'2020-03-23T07:30:20.0100000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (14, 5, 5, 1, CAST(N'2020-06-23T07:30:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (15, 6, 6, 1, CAST(N'2021-01-23T09:35:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (16, 4, 7, 1, CAST(N'2021-01-21T07:30:20.0100000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (17, 5, 8, 1, CAST(N'2021-01-20T07:30:20.0000000' AS DateTime2), 1)
GO
INSERT [dbo].[Relationships] ([Id], [MarketParticipant_Id], [MarketEvaluationPoint_Id], [Type], [EffectuationDate], [State]) VALUES (18, 6, 9, 1, CAST(N'2020-01-23T09:35:20.0000000' AS DateTime2), 1)
GO
SET IDENTITY_INSERT [dbo].[Relationships] OFF
GO
