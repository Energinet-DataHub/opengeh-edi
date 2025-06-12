-- Grant developers access to query execution plans in e.g. SQL Server Management Studio
GRANT CREATE ANY DATABASE EVENT SESSION TO [SEC-G-Datahub-DevelopersAzure];
GRANT SHOWPLAN TO [SEC-G-Datahub-DevelopersAzure];
GRANT VIEW DATABASE STATE TO [SEC-G-Datahub-DevelopersAzure];
GRANT VIEW DATABASE PERFORMANCE STATE TO [SEC-G-Datahub-DevelopersAzure];