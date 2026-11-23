-- ------------------------------------------------------------
-- Optional one-off fix: shift legacy timestamps that were written
-- in UTC to Belarus local time (UTC+3).
--
-- Run this ONCE, and only BEFORE any new order is placed with the
-- updated code. New rows are already stored in local time, so
-- running this again would shift them twice.
-- ------------------------------------------------------------

UPDATE "OrderInfo"
SET "createdAt" = "createdAt" + interval '3 hours',
    "paidAt"    = "paidAt"    + interval '3 hours';

UPDATE "Review"         SET "createdAt"   = "createdAt"   + interval '3 hours';
UPDATE "Notice"         SET "createdAt"   = "createdAt"   + interval '3 hours';
UPDATE "ContactMessage" SET "createdAt"   = "createdAt"   + interval '3 hours',
                        "repliedAt"   = "repliedAt"   + interval '3 hours';
UPDATE "Customer"       SET "registeredAt" = "registeredAt" + interval '3 hours';
UPDATE "Merchant"       SET "submittedAt"  = "submittedAt"  + interval '3 hours',
                        "approvedAt"   = "approvedAt"   + interval '3 hours';
UPDATE "Product"        SET "createdAt"   = "createdAt"   + interval '3 hours';
UPDATE "ViewLog"        SET "viewedAt"    = "viewedAt"    + interval '3 hours';
-- DailyVisit."statDate" is a calendar bucket, leave it unchanged.
