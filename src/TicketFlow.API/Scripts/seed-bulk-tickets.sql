-- Bulk-seeds N test tickets so you can eyeball the grid/scroll behaviour in the UI.
-- Run with: psql -h localhost -U ticketflow_user -d ticketflow_dev -f seed-bulk-tickets.sql
-- Reuses an existing User/Category so FKs stay valid; safe to re-run (each run adds another batch).

DO $$
DECLARE
    v_count       INT := 300;  -- how many tickets to create
    v_user_id     INT := (SELECT "Id" FROM "Users" ORDER BY "Id" LIMIT 1);
    v_category_id INT := (SELECT "Id" FROM "Categories" ORDER BY "Id" LIMIT 1);
BEGIN
    IF v_user_id IS NULL OR v_category_id IS NULL THEN
        RAISE EXCEPTION 'No Users or Categories found - create at least one of each first.';
    END IF;

    FOR i IN 1..v_count LOOP
        INSERT INTO "Tickets"
            ("Title", "Description", "Status", "Priority", "CreatedAt", "UpdatedAt", "UserId", "AgentId", "CategoryId",
             "ProjectId", "TicketNumber", "IsDeleted", "DeletedAt", "ResolvedAt", "DueDate")
        VALUES
            ('Load test ticket #' || i,
             'Auto-generated ticket for UI scroll/pagination testing.',
             CASE (i % 4)
                 WHEN 0 THEN 'Open'
                 WHEN 1 THEN 'InProgress'
                 WHEN 2 THEN 'Resolved'
                 ELSE 'Closed' END,
             CASE (i % 4)
                 WHEN 0 THEN 'Low'
                 WHEN 1 THEN 'Medium'
                 WHEN 2 THEN 'High'
                 ELSE 'Urgent' END,
             (now() AT TIME ZONE 'utc') - (i * INTERVAL '1 minute'),
             NULL,
             v_user_id,
             NULL,
             v_category_id,
             NULL,
             nextval('"TicketNumberSeq"'),
             false,
             NULL,
             NULL,
             NULL);
    END LOOP;

    RAISE NOTICE '% tickets inserted.', v_count;
END $$;
