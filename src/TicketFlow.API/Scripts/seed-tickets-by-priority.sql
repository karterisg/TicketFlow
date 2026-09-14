-- Bulk-seeds 100 test tickets evenly spread across all 4 priorities (Low/Medium/High/Urgent)
-- and across Open/Resolved/Closed statuses.
-- Run with: psql -h localhost -U ticketflow_user -d ticketflow_dev -f seed-tickets-by-priority.sql
-- Reuses an existing User/Category so FKs stay valid; safe to re-run (each run adds another batch).

DO $$
DECLARE
    v_count       INT := 100;
    v_user_id     INT := (SELECT "Id" FROM "Users" ORDER BY "Id" LIMIT 1);
    v_category_id INT := (SELECT "Id" FROM "Categories" ORDER BY "Id" LIMIT 1);
    v_status      TEXT;
    v_priority    TEXT;
    v_resolved    TIMESTAMP;
BEGIN
    IF v_user_id IS NULL OR v_category_id IS NULL THEN
        RAISE EXCEPTION 'No Users or Categories found - create at least one of each first.';
    END IF;

    FOR i IN 1..v_count LOOP
        v_status := CASE (i % 3)
            WHEN 0 THEN 'Open'
            WHEN 1 THEN 'Resolved'
            ELSE 'Closed'
        END;

        v_priority := CASE (i % 4)
            WHEN 0 THEN 'Low'
            WHEN 1 THEN 'Medium'
            WHEN 2 THEN 'High'
            ELSE 'Urgent'
        END;

        v_resolved := CASE WHEN v_status = 'Open' THEN NULL
            ELSE (now() AT TIME ZONE 'utc') + ((-i + 5) * INTERVAL '1 minute') END;

        INSERT INTO "Tickets"
            ("Title", "Description", "Status", "Priority", "CreatedAt", "UpdatedAt", "UserId", "AgentId", "CategoryId",
             "ProjectId", "TicketNumber", "IsDeleted", "DeletedAt", "ResolvedAt", "DueDate")
        VALUES
            ('Priority test ticket #' || i,
             'Auto-generated ticket for priority reporting/testing.',
             v_status,
             v_priority,
             (now() AT TIME ZONE 'utc') - (i * INTERVAL '1 minute'),
             NULL,
             v_user_id,
             NULL,
             v_category_id,
             NULL,
             nextval('"TicketNumberSeq"'),
             false,
             NULL,
             v_resolved,
             NULL);
    END LOOP;

    RAISE NOTICE '% tickets inserted (25 Low / 25 Medium / 25 High / 25 Urgent, split across Open/Resolved/Closed).', v_count;
END $$;
