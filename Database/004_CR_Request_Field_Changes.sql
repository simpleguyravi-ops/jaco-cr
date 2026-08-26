USE JACO_CR;
GO
SET NOCOUNT ON;

PRINT '=== 004 CR request field updates ===';

IF OBJECT_ID(N'dbo.ChangeRequests',N'U') IS NULL
    THROW 51020, 'dbo.ChangeRequests does not exist. Complete scripts 001-003 first.', 1;
GO

/* Rename RequestedDate -> RequiredBy */
IF COL_LENGTH(N'dbo.ChangeRequests',N'RequestedDate') IS NOT NULL
   AND COL_LENGTH(N'dbo.ChangeRequests',N'RequiredBy') IS NULL
BEGIN
    EXEC sp_rename
        N'dbo.ChangeRequests.RequestedDate',
        N'RequiredBy',
        N'COLUMN';
END;
GO

/* Rename Justification -> BusinessRequirements */
IF COL_LENGTH(N'dbo.ChangeRequests',N'Justification') IS NOT NULL
   AND COL_LENGTH(N'dbo.ChangeRequests',N'BusinessRequirements') IS NULL
BEGIN
    EXEC sp_rename
        N'dbo.ChangeRequests.Justification',
        N'BusinessRequirements',
        N'COLUMN';
END;
GO

/* Rename Notes -> TangibleBenefits */
IF COL_LENGTH(N'dbo.ChangeRequests',N'Notes') IS NOT NULL
   AND COL_LENGTH(N'dbo.ChangeRequests',N'TangibleBenefits') IS NULL
BEGIN
    EXEC sp_rename
        N'dbo.ChangeRequests.Notes',
        N'TangibleBenefits',
        N'COLUMN';
END;
GO

/* Add Intangible Benefits */
IF COL_LENGTH(N'dbo.ChangeRequests',N'IntangibleBenefits') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD IntangibleBenefits NVARCHAR(MAX) NULL;
END;
GO

/* Add explicit background date/time audit fields */
IF COL_LENGTH(N'dbo.ChangeRequests',N'CreatedOnDate') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD CreatedOnDate DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.ChangeRequests',N'CreatedOnTime') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD CreatedOnTime TIME(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.ChangeRequests',N'LastUpdateDate') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD LastUpdateDate DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.ChangeRequests',N'LastUpdateOn') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD LastUpdateOn TIME(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.ChangeRequests',N'UpdatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD UpdatedByUserId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.ChangeRequests',N'UpdatedByUserName') IS NULL
BEGIN
    ALTER TABLE dbo.ChangeRequests
        ADD UpdatedByUserName NVARCHAR(100) NULL;
END;
GO

/* Backfill background metadata for existing rows from CreatedAt/UpdatedAt. */
UPDATE dbo.ChangeRequests
SET
    CreatedOnDate = COALESCE(CreatedOnDate, CONVERT(date, CreatedAt)),
    CreatedOnTime = COALESCE(CreatedOnTime, CONVERT(time(0), CreatedAt)),
    LastUpdateDate = COALESCE(LastUpdateDate, CONVERT(date, UpdatedAt)),
    LastUpdateOn = COALESCE(LastUpdateOn, CONVERT(time(0), UpdatedAt)),
    UpdatedByUserId = COALESCE(UpdatedByUserId, CreatorUserId),
    UpdatedByUserName = COALESCE(UpdatedByUserName, CreatorUserName);
GO

PRINT '004_CR_Request_Field_Changes completed successfully.';
GO

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.ChangeRequests')
  AND c.name IN
  (
      N'BusinessRequirements',
      N'TangibleBenefits',
      N'IntangibleBenefits',
      N'RequiredBy',
      N'CreatorUserId',
      N'CreatorUserName',
      N'CreatedOnDate',
      N'CreatedOnTime',
      N'LastUpdateDate',
      N'LastUpdateOn',
      N'UpdatedByUserId',
      N'UpdatedByUserName'
  )
ORDER BY c.column_id;
GO
