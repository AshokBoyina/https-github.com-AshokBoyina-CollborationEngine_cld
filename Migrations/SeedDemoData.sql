-- ================================================================================
-- Migration : SeedDemoData
-- Schema    : [Collaboration]
-- Purpose   : Seeds the three reference applications so that the Login screen
--             works immediately.
--
-- WHY ONLY APPLICATIONS?
--   • Users and ApplicationUsers are auto-created by AuthController.UpsertUserAsync
--     on the very first login — no pre-seeding needed.
--   • ApplicationUserTypeConfigs are not yet read from SQL; the app reads feature
--     flags from appsettings.json (JsonApplicationConfigProvider).  These rows
--     will matter in Phase 2 when the SQL provider is wired in.
--   • The one hard requirement for login is that the Application row exists in DB,
--     because AuthController does db.Applications.FirstOrDefaultAsync(a => a.Name == name)
--     and throws if it is missing.
--
-- IMPORTANT: Application GUIDs and API keys here are IDENTICAL to the hard-coded
--   values in DemoController.Seeds dictionary.  Do not change them — the ChatUI
--   Login.razor drop-down and DemoSetup page both depend on these exact values.
--
-- Idempotent: guarded by IF NOT EXISTS — safe to run more than once.
-- Run AFTER : CreateCollaborationSchema.sql
-- ================================================================================

PRINT '=== NICE Platform Collaboration — Demo Seed Script ===';
PRINT 'Started: ' + CONVERT(varchar, GETUTCDATE(), 120);
GO

-- ================================================================================
-- Applications
--
-- Login screen drop-down ↔ API key mapping:
--   "SurveyPortal"    → X-Api-Key: survey-portal-key
--   "CustomerSupport" → X-Api-Key: customer-support-key
--   "NicePortal"      → X-Api-Key: nice-portal-key
--
-- Note: HashedApiKey stores the plain-text key here because the demo pipeline
--   uses it as-is (mock mode).  In production store SHA-256(rawKey) instead.
--
-- LOGIN QUICK REFERENCE (mock mode — type the slug into Auth Token):
-- ┌───────────────────┬──────────────────┬────────────────────┬──────────────────────┐
-- │ Token             │ App              │ UserType           │ API Key              │
-- ├───────────────────┼──────────────────┼────────────────────┼──────────────────────┤
-- │ alice-smith       │ SurveyPortal     │ External           │ survey-portal-key    │
-- │ agent-sarah       │ SurveyPortal     │ Agent              │ survey-portal-key    │
-- │ supervisor-james  │ CustomerSupport  │ Supervisor         │ customer-support-key │
-- │ internal-kate     │ NicePortal       │ Internal           │ nice-portal-key      │
-- │ standalone-tom    │ SurveyPortal     │ Standalone         │ survey-portal-key    │
-- │ monitor-jane      │ SurveyPortal     │ StandaloneMonitor  │ survey-portal-key    │
-- └───────────────────┴──────────────────┴────────────────────┴──────────────────────┘
-- Any slug is valid — AuthController creates the user row on first login automatically.
-- ================================================================================

-- ── SurveyPortal ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM [Collaboration].[Applications]
    WHERE  Id = '00000000-0000-0000-0001-000000000001'
)
BEGIN
    INSERT INTO [Collaboration].[Applications]
        (Id, Name, HashedApiKey, AuthProvider,
         MaxAgentsOnline, MaxUsersOnline, BlobContainerPath,
         WebhookUrl, IsActive, CreatedAt)
    VALUES
        ('00000000-0000-0000-0001-000000000001',
         'SurveyPortal',
         'survey-portal-key',
         'ANON',
         20, 100, 'surveyportal',
         NULL, 1, GETUTCDATE());

    PRINT '  [+] Application: SurveyPortal';
END
ELSE
    PRINT '  [=] Application: SurveyPortal — already exists, skipped.';
GO

-- ── CustomerSupport ───────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM [Collaboration].[Applications]
    WHERE  Id = '00000000-0000-0000-0001-000000000002'
)
BEGIN
    INSERT INTO [Collaboration].[Applications]
        (Id, Name, HashedApiKey, AuthProvider,
         MaxAgentsOnline, MaxUsersOnline, BlobContainerPath,
         WebhookUrl, IsActive, CreatedAt)
    VALUES
        ('00000000-0000-0000-0001-000000000002',
         'CustomerSupport',
         'customer-support-key',
         'READI',
         20, 200, 'customersupport',
         NULL, 1, GETUTCDATE());

    PRINT '  [+] Application: CustomerSupport';
END
ELSE
    PRINT '  [=] Application: CustomerSupport — already exists, skipped.';
GO

-- ── NicePortal ────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM [Collaboration].[Applications]
    WHERE  Id = '00000000-0000-0000-0001-000000000003'
)
BEGIN
    INSERT INTO [Collaboration].[Applications]
        (Id, Name, HashedApiKey, AuthProvider,
         MaxAgentsOnline, MaxUsersOnline, BlobContainerPath,
         WebhookUrl, IsActive, CreatedAt)
    VALUES
        ('00000000-0000-0000-0001-000000000003',
         'NicePortal',
         'nice-portal-key',
         'NICE',
         10, 50, 'niceportal',
         NULL, 1, GETUTCDATE());

    PRINT '  [+] Application: NicePortal';
END
ELSE
    PRINT '  [=] Application: NicePortal — already exists, skipped.';
GO

-- ================================================================================
PRINT '';
PRINT '=== Seed complete ===';
PRINT 'Finished: ' + CONVERT(varchar, GETUTCDATE(), 120);
PRINT '';
PRINT 'Three applications are now registered.';
PRINT 'Users are created automatically by AuthController on first login.';
PRINT 'Type any slug (e.g. agent-sarah) into the Auth Token field to log in.';
GO
