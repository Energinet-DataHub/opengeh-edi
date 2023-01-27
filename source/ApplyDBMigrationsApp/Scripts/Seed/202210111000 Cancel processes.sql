UPDATE p
SET p.Status = 1
    FROM dbo.BusinessProcesses p
  LEFT JOIN SupplierRegistrations s ON p.Id = s.BusinessProcessId
WHERE s.Id IS NULL AND p.Status = 0