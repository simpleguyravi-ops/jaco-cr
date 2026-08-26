USE JACO_CR;
GO
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CRLookupValues',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CRLookupValues
    (
        Id INT IDENTITY PRIMARY KEY,
        LookupType NVARCHAR(40) NOT NULL,
        Value NVARCHAR(80) NOT NULL,
        DisplayText NVARCHAR(150) NOT NULL,
        SortOrder INT NOT NULL,
        Active BIT NOT NULL DEFAULT 1
    );

    CREATE UNIQUE INDEX UX_CRLookupValues_Type_Value
        ON dbo.CRLookupValues(LookupType,Value);
END;
GO

-- Fixed/controlled initial values. The table keeps the values out of the view code
-- so an admin-managed lookup screen can be added later without changing CR logic.
DECLARE @values TABLE
(
    LookupType NVARCHAR(40),
    Value NVARCHAR(80),
    DisplayText NVARCHAR(150),
    SortOrder INT
);

INSERT @values VALUES
(N'Department',N'IT',N'IT',1),
(N'Department',N'Finance',N'Finance',2),
(N'Department',N'Sales',N'Sales',3),
(N'Department',N'HR',N'HR',4),
(N'Department',N'Operations',N'Operations',5),
(N'Priority',N'Low',N'Low',1),
(N'Priority',N'Medium',N'Medium',2),
(N'Priority',N'High',N'High',3),
(N'Priority',N'Critical',N'Critical',4),
(N'Impact',N'Low',N'Low',1),
(N'Impact',N'Medium',N'Medium',2),
(N'Impact',N'High',N'High',3),
(N'ChangeReason',N'ProcessImprovement',N'Process Improvement',1),
(N'ChangeReason',N'Compliance',N'Compliance',2),
(N'ChangeReason',N'Regulatory',N'Regulatory Requirement',3),
(N'ChangeReason',N'Security',N'Security',4),
(N'ChangeReason',N'SystemEnhancement',N'System Enhancement',5),
(N'ChangeReason',N'Infrastructure',N'Infrastructure',6),
(N'ChangeReason',N'Other',N'Other',99);

INSERT dbo.CRLookupValues(LookupType,Value,DisplayText,SortOrder)
SELECT v.LookupType,v.Value,v.DisplayText,v.SortOrder
FROM @values v
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.CRLookupValues x
    WHERE x.LookupType=v.LookupType AND x.Value=v.Value
);

PRINT '003_CR_LookupValues completed successfully.';
GO
