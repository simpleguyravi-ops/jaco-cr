USE JACO_CR;
GO
SET NOCOUNT ON;

PRINT '=== 002 CR ID sequence + SAP reference ===';

IF OBJECT_ID(N'dbo.CRIdSequence',N'SO') IS NULL
BEGIN
    CREATE SEQUENCE dbo.CRIdSequence
        AS BIGINT
        START WITH 1000000000
        INCREMENT BY 1
        MINVALUE 1000000000
        NO MAXVALUE
        CACHE 50;
END;
GO

IF COL_LENGTH('dbo.ChangeRequests','CRId') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD CRId BIGINT NULL;
END;
GO

-- Assign sequence numbers to existing rows.
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'
UPDATE dbo.ChangeRequests
SET CRId = NEXT VALUE FOR dbo.CRIdSequence
WHERE Id = ' + CONVERT(varchar(20),Id) + N' AND CRId IS NULL;'
FROM dbo.ChangeRequests
WHERE CRId IS NULL;
IF @sql <> N'' EXEC sys.sp_executesql @sql;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name=N'UX_ChangeRequests_CRId'
      AND object_id=OBJECT_ID(N'dbo.ChangeRequests')
)
BEGIN
    CREATE UNIQUE INDEX UX_ChangeRequests_CRId
        ON dbo.ChangeRequests(CRId);
END;
GO

IF COL_LENGTH('dbo.ChangeRequests','SAPReferenceId') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD SAPReferenceId NVARCHAR(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name=N'IX_ChangeRequests_SAPReferenceId'
      AND object_id=OBJECT_ID(N'dbo.ChangeRequests')
)
BEGIN
    CREATE INDEX IX_ChangeRequests_SAPReferenceId
        ON dbo.ChangeRequests(SAPReferenceId);
END;
GO

-- Computed CRNumber becomes the formatted 10-digit business identifier.
IF COL_LENGTH('dbo.ChangeRequests','CRNumber') IS NOT NULL
BEGIN
    DECLARE @constraint sysname;

    SELECT @constraint = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c
      ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ChangeRequests')
      AND c.name = N'CRNumber';

    IF @constraint IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.ChangeRequests DROP CONSTRAINT ' + QUOTENAME(@constraint));
    END;

    -- Rebuild CRNumber as a persisted computed column if it is not already computed.
    IF NOT EXISTS (
        SELECT 1
        FROM sys.computed_columns
        WHERE object_id=OBJECT_ID(N'dbo.ChangeRequests')
          AND name=N'CRNumber'
    )
    BEGIN
        ALTER TABLE dbo.ChangeRequests DROP COLUMN CRNumber;
        ALTER TABLE dbo.ChangeRequests
        ADD CRNumber AS
            RIGHT(REPLICATE('0',10) + CONVERT(varchar(10), CRId), 10) PERSISTED;
    END;
END;
GO

PRINT '002_Add_CRId_Sequence_And_SAPReference completed successfully.';
GO
