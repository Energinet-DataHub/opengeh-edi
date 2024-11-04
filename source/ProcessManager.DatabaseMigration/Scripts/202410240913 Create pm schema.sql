IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pm')
BEGIN
    EXEC('CREATE SCHEMA pm');
END
