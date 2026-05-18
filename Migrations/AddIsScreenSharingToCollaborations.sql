-- ============================================================
-- Migration: AddIsScreenSharingToCollaborations
-- Schema: [Collaboration]
-- Safe to run on any existing database — checks before adding.
-- Run this once against your target database.
-- ============================================================

-- 1. Add IsScreenSharing column if it doesn't already exist
IF NOT EXISTS (
    SELECT 1
    FROM   sys.columns c
    JOIN   sys.tables  t ON c.object_id = t.object_id
    JOIN   sys.schemas s ON t.schema_id = s.schema_id
    WHERE  s.name = 'Collaboration'
    AND    t.name = 'Collaborations'
    AND    c.name = 'IsScreenSharing'
)
BEGIN
    ALTER TABLE [Collaboration].[Collaborations]
        ADD IsScreenSharing BIT NOT NULL DEFAULT 0;

    PRINT 'Column IsScreenSharing added to [Collaboration].[Collaborations].';
END
ELSE
BEGIN
    PRINT 'Column IsScreenSharing already exists — skipped.';
END
GO

-- 2. Add IsRecorded column if it doesn't already exist
IF NOT EXISTS (
    SELECT 1
    FROM   sys.columns c
    JOIN   sys.tables  t ON c.object_id = t.object_id
    JOIN   sys.schemas s ON t.schema_id = s.schema_id
    WHERE  s.name = 'Collaboration'
    AND    t.name = 'Collaborations'
    AND    c.name = 'IsRecorded'
)
BEGIN
    ALTER TABLE [Collaboration].[Collaborations]
        ADD IsRecorded BIT NOT NULL DEFAULT 0;

    PRINT 'Column IsRecorded added to [Collaboration].[Collaborations].';
END
ELSE
BEGIN
    PRINT 'Column IsRecorded already exists — skipped.';
END
GO

PRINT 'Migration complete.';
GO
