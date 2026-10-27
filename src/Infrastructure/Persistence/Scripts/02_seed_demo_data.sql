-- ============================================================================
-- Automotive Unified Service Scheduler: Demo Seed Data Script
-- ============================================================================

INSERT INTO "Dealerships" ("Id", "Name", "Address", "TimeZone")
VALUES ('d1111111-1111-1111-1111-111111111111', 'Downtown Automotive Center', '100 Motorway Ave, Detroit, MI', 'UTC')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Customers" ("Id", "FirstName", "LastName", "Email", "PhoneNumber")
VALUES ('c2222222-2222-2222-2222-222222222222', 'Jane', 'Doe', 'jane.doe@example.com', '+1-555-0199')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Vehicles" ("Id", "OwnerId", "Make", "Model", "Year", "Vin", "LicensePlate")
VALUES ('a3333333-3333-3333-3333-333333333333', 'c2222222-2222-2222-2222-222222222222', 'Tesla', 'Model Y', 2024, '1HGCR2F83HA000123', 'EV-SCHED1')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "ServiceTypes" ("Id", "Name", "ServiceCategory", "BaseDuration", "RequiredSkillLevel", "RequiredBayCapability", "Description")
VALUES 
  ('b4444444-4444-4444-4444-444444444444', 'Standard Brake Service', 'Brakes', '01:00:00', 'Journeyman', 'Lift', 'Brake rotor inspection, pad replacement, and fluid bleed.'),
  ('e5555555-5555-5555-5555-555555555555', 'HV Battery Diagnostics', 'EV Powertrain', '02:00:00', 'Specialist', 'EvCharging', 'High-voltage contactor inspection and battery module diagnostics.')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Technicians" ("Id", "DealershipId", "EmployeeNumber", "IsActive", "FirstName", "LastName", "Email", "PhoneNumber", "Version")
VALUES ('f6666666-6666-6666-6666-666666666666', 'd1111111-1111-1111-1111-111111111111', 'TECH-007', true, 'John', 'Smith', 'john.smith@dealership.com', '+1-555-0200', 0)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "TechnicianCertifications" ("Id", "TechnicianId", "ServiceCategory", "Level", "IssuedOn", "ExpiresOn")
VALUES ('11111111-2222-3333-4444-555555555555', 'f6666666-6666-6666-6666-666666666666', 'Brakes', 'Senior', '2025-01-01', '2028-01-01')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "ServiceBays" ("Id", "DealershipId", "BayCode", "Capability", "IsUnderMaintenance", "Version")
VALUES ('97777777-7777-7777-7777-777777777777', 'd1111111-1111-1111-1111-111111111111', 'BAY-01', 'Lift', false, 0)
ON CONFLICT ("Id") DO NOTHING;
