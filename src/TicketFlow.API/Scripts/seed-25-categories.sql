-- Bulk-seeds 25 test categories.
-- Run with: psql -h localhost -U ticketflow_user -d ticketflow_dev -f seed-25-categories.sql
-- Skips names that already exist so it's safe to re-run.

INSERT INTO "Categories" ("Name")
SELECT v.name
FROM (VALUES
    ('Billing'), ('Technical Support'), ('Account Access'), ('Bug Report'),
    ('Feature Request'), ('General Inquiry'), ('Hardware Issue'), ('Software Issue'),
    ('Network Issue'), ('Security Concern'), ('Data Loss'), ('Performance Issue'),
    ('Installation Help'), ('License Question'), ('Refund Request'), ('Upgrade Request'),
    ('Integration Support'), ('API Issue'), ('Mobile App Issue'), ('Email Issue'),
    ('Password Reset'), ('Onboarding'), ('Training Request'), ('Compliance'),
    ('Other')
) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM "Categories" c WHERE c."Name" = v.name);
