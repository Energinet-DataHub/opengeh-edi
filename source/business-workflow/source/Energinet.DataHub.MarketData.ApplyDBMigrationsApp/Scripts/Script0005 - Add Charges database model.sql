CREATE TABLE ChargeType
(
	ID int NOT NULL PRIMARY KEY,
	name nvarchar(12) NOT NULL CHECK (name IN('Fee', 'Subscription', 'Tariff'))
)

INSERT INTO [dbo].[ChargeType] VALUES (1, 'Fee');
INSERT INTO [dbo].[ChargeType] VALUES (2, 'Subscription');
INSERT INTO [dbo].[ChargeType] VALUES (3, 'Tariff');

CREATE TABLE ResolutionType
(
	ID int NOT NULL PRIMARY KEY,
	name nvarchar(5) NOT NULL CHECK (name IN('PT15M', 'PT1H', 'P1D', 'P1M'))
)

INSERT INTO [dbo].[ResolutionType] VALUES (1, 'PT15M');
INSERT INTO [dbo].[ResolutionType] VALUES (2, 'PT1H');
INSERT INTO [dbo].[ResolutionType] VALUES (3, 'P1D');
INSERT INTO [dbo].[ResolutionType] VALUES (4, 'P1M');

CREATE TABLE VATPayer
(
	ID int NOT NULL PRIMARY KEY,
	name nvarchar(3) NOT NULL CHECK (name IN('D01', 'D02'))
)
INSERT INTO [dbo].[VATPayer] VALUES (1, 'D01');
INSERT INTO [dbo].[VATPayer] VALUES (2, 'D02');

CREATE TABLE MarketParticipant
(
	mRID nvarchar(35) NOT NULL PRIMARY KEY
)

CREATE TABLE Charge
(
	ID int NOT NULL PRIMARY KEY,
	mRID nvarchar(35) NOT NULL,
	ChargeTypeID int NOT NULL FOREIGN KEY REFERENCES ChargeType(ID),
	Name nvarchar(132) NOT NULL,
	Description nvarchar(2048) NOT NULL,
	Status TINYINT CHECK (Status >= 2 AND Status <= 4),
	StartDate bigint NOT NULL,
	EndDate bigint NOT NULL,
	Currency nvarchar(10) NOT NULL,
	ChargeTypeOwnermRID nvarchar(35) NOT NULL FOREIGN KEY REFERENCES MarketParticipant(mRID),
	TransparentInvoicing bit NOT NULL,
	TaxIndicator bit NOT NULL,
	ResolutionTypeId int NOT NULL FOREIGN KEY REFERENCES ResolutionType(ID),
	VATPayer int NOT NULL FOREIGN KEY REFERENCES VATPayer(ID),
	LastUpdatedByCorrelationID nvarchar(36) NOT NULL,
	LastUpdatedByTransactionID nvarchar(100) NOT NULL,
	LastUpdatedBy nvarchar(100) NOT NULL,
	RequestDateTime bigint NOT NULL
)

CREATE TABLE ChargePrice
(
	ID int NOT NULL PRIMARY KEY,
	ChargeId int NOT NULL FOREIGN KEY REFERENCES Charge(ID),
	Time bigint NOT NULL,
	Amount DECIMAL(14,6),
	LastUpdatedByCorrelationID nvarchar(36) NOT NULL,
	LastUpdatedByTransactionID nvarchar(100) NOT NULL,
	LastUpdatedBy nvarchar(100) NOT NULL,
	RequestDateTime bigint NOT NULL
)