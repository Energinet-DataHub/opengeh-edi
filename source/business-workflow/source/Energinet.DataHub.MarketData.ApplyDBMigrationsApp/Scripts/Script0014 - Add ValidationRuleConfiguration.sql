CREATE TABLE ValidationRuleConfiguration
(
    [Key] nvarchar(50) NOT NULL PRIMARY KEY,
    [Value] nvarchar(50) NOT NULL
)

INSERT INTO [dbo].[ValidationRuleConfiguration] VALUES ('VR209_Start_Of_Valid_Interval_From_Now_In_Days', '31');
INSERT INTO [dbo].[ValidationRuleConfiguration] VALUES ('VR209_End_Of_Valid_Interval_From_Now_In_Days', '1095');