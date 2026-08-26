USE JACO_CR;
GO
SET NOCOUNT ON;

PRINT '=== 005 CR attachments ===';

IF OBJECT_ID(N'dbo.ChangeRequests',N'U') IS NULL
    THROW 51030, 'dbo.ChangeRequests does not exist. Complete scripts 001-004 first.', 1;
GO

IF OBJECT_ID(N'dbo.CRAttachments',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CRAttachments
    (
        Id BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_CRAttachments PRIMARY KEY,
        ChangeRequestId BIGINT NOT NULL,
        OriginalFileName NVARCHAR(260) NOT NULL,
        StoredFileName NVARCHAR(260) NOT NULL,
        ContentType NVARCHAR(150) NOT NULL,
        FileSize BIGINT NOT NULL,
        UploadedByUserName NVARCHAR(100) NOT NULL,
        UploadedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        TransferStatus NVARCHAR(30) NOT NULL DEFAULT(N'Pending'),
        ApprovalAttachmentId BIGINT NULL,
        TransferredAt DATETIME2 NULL,

        CONSTRAINT FK_CRAttachments_ChangeRequest
            FOREIGN KEY(ChangeRequestId)
            REFERENCES dbo.ChangeRequests(Id)
    );

    CREATE INDEX IX_CRAttachments_ChangeRequest
        ON dbo.CRAttachments(ChangeRequestId,UploadedAt);
END;
GO

PRINT '005_CR_Attachments completed successfully.';
GO
