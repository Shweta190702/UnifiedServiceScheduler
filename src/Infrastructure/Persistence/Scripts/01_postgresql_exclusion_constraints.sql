-- ============================================================================
-- Automotive Unified Service Scheduler: Physical Exclusion Constraints
-- ============================================================================
-- Enforces half-open interval [StartTime, EndTime) non-overlapping scheduling
-- at the PostgreSQL storage engine level using the btree_gist extension.
--
-- Even if an application bug bypassed row-locks and application checks,
-- this constraint physically rejects any overlapping booking at the engine level.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS btree_gist;

-- 1. Strict conflict-free schedule for Technicians:
-- A technician cannot have two bookings where the time ranges overlap:
-- [StartTime, EndTime) && [OtherStartTime, OtherEndTime)
ALTER TABLE "TechnicianBookings"
ADD CONSTRAINT "no_overlapping_technician_bookings"
EXCLUDE USING gist (
    "TechnicianId" WITH =,
    tstzrange("StartTime", "EndTime", '[)') WITH &&
);

-- 2. Strict conflict-free schedule for ServiceBays:
-- A bay cannot have two bookings where the time ranges overlap:
ALTER TABLE "BayBookings"
ADD CONSTRAINT "no_overlapping_bay_bookings"
EXCLUDE USING gist (
    "BayId" WITH =,
    tstzrange("StartTime", "EndTime", '[)') WITH &&
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS "ix_technician_bookings_slot"
ON "TechnicianBookings" USING gist (
    "TechnicianId",
    tstzrange("StartTime", "EndTime", '[)')
);

CREATE INDEX IF NOT EXISTS "ix_bay_bookings_slot"
ON "BayBookings" USING gist (
    "BayId",
    tstzrange("StartTime", "EndTime", '[)')
);
