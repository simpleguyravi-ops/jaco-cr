CREATE DATABASE JACO_CR;
GO
USE JACO_CR;
GO

CREATE TABLE dbo.ChangeRequests
(
    Id BIGINT IDENTITY PRIMARY KEY,
    CRNumber NVARCHAR(40) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Department NVARCHAR(50) NOT NULL,
    Priority NVARCHAR(30) NOT NULL,
    Justification NVARCHAR(MAX) NOT NULL,
    RequestedDate DATE NULL,
    Impact NVARCHAR(30) NOT NULL,
    ChangeReason NVARCHAR(80) NULL,
    Notes NVARCHAR(MAX) NULL,

    CreatorUserId INT NOT NULL,
    CreatorUserName NVARCHAR(100) NOT NULL,

    Status NVARCHAR(40) NOT NULL,
    ApprovalWorkflowNo NVARCHAR(50) NULL,
    ApprovalStatus NVARCHAR(40) NULL,
    ApprovalCurrentLevel INT NULL,

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE UNIQUE INDEX UX_ChangeRequests_CRNumber
    ON dbo.ChangeRequests(CRNumber);

CREATE INDEX IX_ChangeRequests_Creator
    ON dbo.ChangeRequests(CreatorUserId,CreatedAt);

CREATE INDEX IX_ChangeRequests_ApprovalWorkflow
    ON dbo.ChangeRequests(ApprovalWorkflowNo);

PRINT 'JACO_CR database created successfully.';
GO
