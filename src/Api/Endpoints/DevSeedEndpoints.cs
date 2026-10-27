using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Api.Endpoints;

public static class DevSeedEndpoints
{
    public static readonly Guid DealershipId  = Guid.Parse("d1111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerId    = Guid.Parse("c2222222-2222-2222-2222-222222222222");
    public static readonly Guid VehicleId     = Guid.Parse("a3333333-3333-3333-3333-333333333333");
    public static readonly Guid BrakeServiceId = Guid.Parse("b4444444-4444-4444-4444-444444444444");
    public static readonly Guid EvServiceId    = Guid.Parse("e5555555-5555-5555-5555-555555555555");
    public static readonly Guid TransmissionServiceId = Guid.Parse("b5555555-5555-5555-5555-555555555555");
    public static readonly Guid QuickInspectionServiceId = Guid.Parse("b6666666-6666-6666-6666-666666666666");
    public static readonly Guid TechnicianId  = Guid.Parse("f6666666-6666-6666-6666-666666666666");
    public static readonly Guid ServiceBayId  = Guid.Parse("97777777-7777-7777-7777-777777777777");
    public static readonly Guid ServiceBay2Id = Guid.Parse("98888888-8888-8888-8888-888888888888");
    public static readonly Guid ServiceBay3Id = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid Technician2Id = Guid.Parse("f7777777-7777-7777-7777-777777777777");
    public static readonly Guid Technician3Id = Guid.Parse("f8888888-8888-8888-8888-888888888888");

    public static IEndpointRouteBuilder MapDevSeedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/dev/reset-and-seed", async (UnifiedServiceSchedulerDbContext db) =>
        {
            // Truncate tables cleanly in PostgreSQL
            await db.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"Appointments\", \"BayBookings\", \"TechnicianBookings\", \"TechnicianCertifications\", \"ServiceBays\", \"Technicians\", \"Vehicles\", \"Customers\", \"ServiceTypes\", \"Dealerships\" CASCADE;");

            // 1. Dealership
            var dealership = Dealership.Register(new DealershipId(DealershipId), "Downtown Automotive Center", "100 Motorway Ave, Detroit, MI", TimeZoneInfo.Utc);
            await db.Dealerships.AddAsync(dealership);

            // 2. Customer
            var customer = Customer.Create(new CustomerId(CustomerId), ContactInfo.Create("Jane", "Doe", "jane.doe@example.com", "+1-555-0199"));
            await db.Customers.AddAsync(customer);

            // 3. Vehicle
            var vehicle = Vehicle.Register(new VehicleId(VehicleId), customer.Id, "Tesla", "Model Y", 2024, "1HGCR2F83HA000123", "EV-SCHED1");
            await db.Vehicles.AddAsync(vehicle);

            // 4. Service Types
            var brakeService = ServiceType.Define(new ServiceTypeId(BrakeServiceId), "Standard Brake Service", "Brakes", TimeSpan.FromHours(1), SkillLevel.Journeyman, BayCapability.Lift, "Brake rotor inspection and pad replacement.");
            var evService = ServiceType.Define(new ServiceTypeId(EvServiceId), "HV Battery Diagnostics", "EV Powertrain", TimeSpan.FromHours(2), SkillLevel.Specialist, BayCapability.EvCharging, "High-voltage contactor inspection.");
            var transmissionService = ServiceType.Define(new ServiceTypeId(TransmissionServiceId), "Advanced Transmission Overhaul", "Transmission", TimeSpan.FromHours(2), SkillLevel.Master, BayCapability.Lift, "Complete transmission rebuild.");
            var inspectionService = ServiceType.Define(new ServiceTypeId(QuickInspectionServiceId), "Quick Multi-Point Inspection", "Brakes", TimeSpan.FromMinutes(30), SkillLevel.Journeyman, BayCapability.Lift, "30-min inspection.");
            await db.ServiceTypes.AddRangeAsync(brakeService, evService, transmissionService, inspectionService);

            // 5. Technicians (John has Brakes; Sarah has EV; Mike has Inspection)
            var tech1 = Technician.Hire(new TechnicianId(TechnicianId), dealership.Id, ContactInfo.Create("John", "Smith", "john.smith@dealership.com", "+1-555-0200"), "TECH-001");
            tech1.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));

            var tech2 = Technician.Hire(new TechnicianId(Technician2Id), dealership.Id, ContactInfo.Create("Sarah", "Connor", "sarah.connor@dealership.com", "+1-555-0202"), "TECH-002");
            tech2.AddCertification(Certification.Create("EV Powertrain", SkillLevel.Specialist, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));

            var tech3 = Technician.Hire(new TechnicianId(Technician3Id), dealership.Id, ContactInfo.Create("Mike", "FreeTech", "mike.free@dealership.com", "+1-555-0203"), "TECH-003");
            tech3.AddCertification(Certification.Create("Inspection", SkillLevel.Journeyman, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
            await db.Technicians.AddRangeAsync(tech1, tech2, tech3);

            // 6. Service Bays
            var bay1 = ServiceBay.Commission(new ServiceBayId(ServiceBayId), dealership.Id, "BAY-01", BayCapability.Lift);
            var bay2 = ServiceBay.Commission(new ServiceBayId(ServiceBay2Id), dealership.Id, "BAY-02", BayCapability.Lift);
            var bay3 = ServiceBay.Commission(new ServiceBayId(ServiceBay3Id), dealership.Id, "BAY-03", BayCapability.Standard);
            await db.ServiceBays.AddRangeAsync(bay1, bay2, bay3);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Database reset and seeded with fresh baseline data successfully!",
                dealershipId = DealershipId,
                customerId = CustomerId,
                vehicleId = VehicleId,
                brakeServiceId = BrakeServiceId,
                evServiceId = EvServiceId,
                transmissionServiceId = TransmissionServiceId,
                inspectionServiceId = QuickInspectionServiceId,
                technicians = new[] { "John Smith (Brakes)", "Sarah Connor (Brakes)", "Mike FreeTech (Brakes)" },
                serviceBays = new[] { "BAY-01 (Lift)", "BAY-02 (Lift)", "BAY-03 (Standard)" }
            });
        })
        .WithTags("Development")
        .WithSummary("Wipes all tables and resets database to pristine baseline seed data.");

        app.MapPost("/api/v1/dev/seed", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var alreadySeeded = await db.Dealerships.AnyAsync(d => d.Id == new DealershipId(DealershipId));
            if (alreadySeeded)
            {
                var bay2Missing = !await db.ServiceBays.AnyAsync(b => b.Id == new ServiceBayId(ServiceBay2Id));
                if (bay2Missing)
                {
                    var secondBay = ServiceBay.Commission(
                        new ServiceBayId(ServiceBay2Id),
                        new DealershipId(DealershipId),
                        "BAY-02",
                        BayCapability.Lift);
                    await db.ServiceBays.AddAsync(secondBay);
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new
                {
                    message = "Database already contains seed data (BAY-02 verified).",
                    dealershipId = DealershipId,
                    customerId = CustomerId,
                    vehicleId = VehicleId,
                    serviceTypeStandardBrakes = BrakeServiceId,
                    serviceTypeEvSpecialist = EvServiceId,
                    technicianId = TechnicianId,
                    serviceBay1Id = ServiceBayId,
                    serviceBay2Id = ServiceBay2Id
                });
            }

            // 1. Dealership
            var dealership = Dealership.Register(
                new DealershipId(DealershipId),
                "Downtown Automotive Center",
                "100 Motorway Ave, Detroit, MI",
                TimeZoneInfo.Utc);
            await db.Dealerships.AddAsync(dealership);

            // 2. Customer
            var customer = Customer.Create(
                new CustomerId(CustomerId),
                ContactInfo.Create("Jane", "Doe", "jane.doe@example.com", "+1-555-0199"));
            await db.Customers.AddAsync(customer);

            // 3. Vehicle
            var vehicle = Vehicle.Register(
                new VehicleId(VehicleId),
                customer.Id,
                "Tesla",
                "Model Y",
                2024,
                "1HGCR2F83HA000123",
                "EV-SCHED1");
            await db.Vehicles.AddAsync(vehicle);

            // 4. Service Types
            // Standard Brake Service (1 hr, Journeyman level, Lift bay)
            var brakeService = ServiceType.Define(
                new ServiceTypeId(BrakeServiceId),
                "Standard Brake Service",
                "Brakes",
                TimeSpan.FromHours(1),
                SkillLevel.Journeyman,
                BayCapability.Lift,
                "Brake rotor inspection, pad replacement, and fluid bleed.");
            await db.ServiceTypes.AddAsync(brakeService);

            // EV Specialist Service (2 hrs, Specialist level, EV Charging bay)
            var evService = ServiceType.Define(
                new ServiceTypeId(EvServiceId),
                "HV Battery Diagnostics",
                "EV Powertrain",
                TimeSpan.FromHours(2),
                SkillLevel.Specialist,
                BayCapability.EvCharging,
                "High-voltage contactor inspection and battery module diagnostics.");
            await db.ServiceTypes.AddAsync(evService);

            // 5. Technician (Qualified in Brakes up to Senior level; NOT certified for EV Powertrain Specialist)
            var technician = Technician.Hire(
                new TechnicianId(TechnicianId),
                dealership.Id,
                ContactInfo.Create("John", "Smith", "john.smith@dealership.com", "+1-555-0200"),
                "TECH-007");

            technician.AddCertification(Certification.Create(
                "Brakes",
                SkillLevel.Senior,
                new DateOnly(2025, 1, 1),
                new DateOnly(2028, 1, 1)));

            await db.Technicians.AddAsync(technician);

            // 6. Service Bays (Two Lift bays so we can test "Bay Available, Tech Busy")
            var bay1 = ServiceBay.Commission(
                new ServiceBayId(ServiceBayId),
                dealership.Id,
                "BAY-01",
                BayCapability.Lift);
            await db.ServiceBays.AddAsync(bay1);

            var bay2 = ServiceBay.Commission(
                new ServiceBayId(ServiceBay2Id),
                dealership.Id,
                "BAY-02",
                BayCapability.Lift);
            await db.ServiceBays.AddAsync(bay2);

            await db.SaveChangesAsync();

            return Results.Created("/api/v1/dev/seed", new
            {
                message = "Seed data created successfully!",
                dealershipId = DealershipId,
                customerId = CustomerId,
                vehicleId = VehicleId,
                serviceTypeStandardBrakes = BrakeServiceId,
                serviceTypeEvSpecialist = EvServiceId,
                technicianId = TechnicianId,
                serviceBay1Id = ServiceBayId,
                serviceBay2Id = ServiceBay2Id,
                note = "Technician 'John Smith' is certified in Brakes (Senior). Two Lift bays (BAY-01, BAY-02) exist. Dealership operates 08:00–18:00 UTC."
            });
        })
        .WithTags("Development")
        .WithSummary("Seeds sample dealership, customer, vehicle, technician, and bays for testing.");

        app.MapPost("/api/v1/dev/scenario/setup-bay-busy", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var dealership = await db.Dealerships.FirstOrDefaultAsync(d => d.Id == new DealershipId(DealershipId));
            if (dealership is null)
                return Results.BadRequest(new { message = "Run /api/v1/dev/seed first." });

            // 1. Ensure BAY-01 and BAY-02 exist
            var bay1 = await db.ServiceBays.Include(b => b.Bookings).FirstOrDefaultAsync(b => b.Id == new ServiceBayId(ServiceBayId));
            var bay2 = await db.ServiceBays.Include(b => b.Bookings).FirstOrDefaultAsync(b => b.Id == new ServiceBayId(ServiceBay2Id));
            if (bay2 is null)
            {
                bay2 = ServiceBay.Commission(new ServiceBayId(ServiceBay2Id), dealership.Id, "BAY-02", BayCapability.Lift);
                await db.ServiceBays.AddAsync(bay2);
            }

            // 2. Ensure Tech 1 (John Smith), Tech 2 (Sarah Connor), and Tech 3 (Mike FreeTech) exist
            var tech1 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(TechnicianId));
            var tech2 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(Technician2Id));
            if (tech2 is null)
            {
                tech2 = Technician.Hire(new TechnicianId(Technician2Id), dealership.Id, ContactInfo.Create("Sarah", "Connor", "sarah.connor@dealership.com", "+1-555-0202"), "TECH-008");
                tech2.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
                await db.Technicians.AddAsync(tech2);
            }

            var tech3 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(Technician3Id));
            if (tech3 is null)
            {
                tech3 = Technician.Hire(new TechnicianId(Technician3Id), dealership.Id, ContactInfo.Create("Mike", "FreeTech", "mike.free@dealership.com", "+1-555-0203"), "TECH-009");
                tech3.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
                await db.Technicians.AddAsync(tech3);
            }

            // 3. Occupy BOTH bays (BAY-01 with Tech 1, BAY-02 with Tech 2) for slot: 2026-10-30 14:00 to 15:00 UTC
            var targetSlot = TimeSlot.From(new DateTimeOffset(2026, 10, 30, 14, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(1));
            var brakeService = await db.ServiceTypes.FirstAsync(s => s.Id == new ServiceTypeId(BrakeServiceId));

            if (!bay1!.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
            {
                var appt1Id = AppointmentId.New();
                var techEvidence1 = tech1!.Reserve(appt1Id, brakeService, targetSlot);
                var bayEvidence1 = bay1.Reserve(appt1Id, brakeService, targetSlot);
                var customer = await db.Customers.FirstAsync();
                var vehicle = await db.Vehicles.FirstAsync();
                var appt1 = Appointment.Schedule(appt1Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), techEvidence1, bayEvidence1, brakeService.Id, targetSlot, "Pre-booking Bay 1");
                await db.Appointments.AddAsync(appt1);
            }

            if (!bay2.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
            {
                var appt2Id = AppointmentId.New();
                var techEvidence2 = tech2.Reserve(appt2Id, brakeService, targetSlot);
                var bayEvidence2 = bay2.Reserve(appt2Id, brakeService, targetSlot);
                var customer = await db.Customers.FirstAsync();
                var vehicle = await db.Vehicles.FirstAsync();
                var appt2 = Appointment.Schedule(appt2Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), techEvidence2, bayEvidence2, brakeService.Id, targetSlot, "Pre-booking Bay 2");
                await db.Appointments.AddAsync(appt2);
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Scenario 'Tech Available, Bay Busy' configured successfully!",
                occupiedSlot = "2026-10-30T14:00:00Z to 15:00:00Z",
                bay1Status = "Occupied (by John Smith)",
                bay2Status = "Occupied (by Sarah Connor)",
                availableTech = "Mike FreeTech (ID: " + Technician3Id + ") is certified and 100% FREE during this slot",
                allLiftBaysFull = true,
                testInstructions = "Now try booking at '2026-10-30T14:00:00Z'. The certified tech (Mike FreeTech) is free, but all physical bays are full. You will receive 409 Conflict (BayUnavailable)."
            });
        })
        .WithTags("Development")
        .WithSummary("Prepares 'Tech Available, Bay Busy' scenario for Friday 2026-10-30 14:00 UTC.");

        app.MapPost("/api/v1/dev/seed-transmission-service", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var exists = await db.ServiceTypes.AnyAsync(s => s.Id == new ServiceTypeId(TransmissionServiceId));
            if (!exists)
            {
                var service = ServiceType.Define(
                    new ServiceTypeId(TransmissionServiceId),
                    "Advanced Transmission Overhaul",
                    "Transmission",
                    TimeSpan.FromHours(2),
                    SkillLevel.Master,
                    BayCapability.Lift,
                    "Complete transmission rebuild requiring Master level skill.");
                await db.ServiceTypes.AddAsync(service);
                await db.SaveChangesAsync();
            }
            return Results.Ok(new
            {
                message = "Advanced Transmission Overhaul service type seeded/verified.",
                serviceTypeId = TransmissionServiceId,
                requiredCategory = "Transmission",
                requiredSkillLevel = "Master",
                requiredCapability = "Lift",
                note = "Lift bays are free, technicians are free, but no technician has 'Master' certification in 'Transmission'."
            });
        })
        .WithTags("Development")
        .WithSummary("Seeds service type requiring Master Transmission skill to test 'Tech Available, Unqualified'.");

        app.MapPost("/api/v1/dev/scenario/setup-both-unavailable", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var dealership = await db.Dealerships.FirstOrDefaultAsync(d => d.Id == new DealershipId(DealershipId));
            if (dealership is null)
                return Results.BadRequest(new { message = "Run /api/v1/dev/seed first." });

            var brakeService = await db.ServiceTypes.FirstAsync(s => s.Id == new ServiceTypeId(BrakeServiceId));
            var customer = await db.Customers.FirstAsync();
            var vehicle = await db.Vehicles.FirstAsync();

            // 1. Ensure all bays exist (BAY-01, BAY-02 with Lift, BAY-03 with Standard)
            var bay1 = await db.ServiceBays.Include(b => b.Bookings).FirstAsync(b => b.Id == new ServiceBayId(ServiceBayId));
            var bay2 = await db.ServiceBays.Include(b => b.Bookings).FirstOrDefaultAsync(b => b.Id == new ServiceBayId(ServiceBay2Id));
            if (bay2 is null)
            {
                bay2 = ServiceBay.Commission(new ServiceBayId(ServiceBay2Id), dealership.Id, "BAY-02", BayCapability.Lift);
                await db.ServiceBays.AddAsync(bay2);
            }

            var bay3 = await db.ServiceBays.Include(b => b.Bookings).FirstOrDefaultAsync(b => b.Id == new ServiceBayId(ServiceBay3Id));
            if (bay3 is null)
            {
                bay3 = ServiceBay.Commission(new ServiceBayId(ServiceBay3Id), dealership.Id, "BAY-03", BayCapability.Standard);
                await db.ServiceBays.AddAsync(bay3);
            }

            // 2. Ensure all 3 technicians exist (John Smith, Sarah Connor, Mike FreeTech)
            var tech1 = await db.Technicians.Include(t => t.Bookings).FirstAsync(t => t.Id == new TechnicianId(TechnicianId));
            var tech2 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(Technician2Id));
            if (tech2 is null)
            {
                tech2 = Technician.Hire(new TechnicianId(Technician2Id), dealership.Id, ContactInfo.Create("Sarah", "Connor", "sarah.connor@dealership.com", "+1-555-0202"), "TECH-008");
                tech2.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
                await db.Technicians.AddAsync(tech2);
            }

            var tech3 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(Technician3Id));
            if (tech3 is null)
            {
                tech3 = Technician.Hire(new TechnicianId(Technician3Id), dealership.Id, ContactInfo.Create("Mike", "FreeTech", "mike.free@dealership.com", "+1-555-0203"), "TECH-009");
                tech3.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
                await db.Technicians.AddAsync(tech3);
            }

            // 3. Occupy slot: Wednesday 2026-11-04 10:00 to 11:00 UTC
            var targetSlot = TimeSlot.From(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(1));

            // Book BAY-01 with Tech 1
            if (!bay1.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
            {
                var appt1Id = AppointmentId.New();
                var eTech1 = tech1.Reserve(appt1Id, brakeService, targetSlot);
                var eBay1 = bay1.Reserve(appt1Id, brakeService, targetSlot);
                await db.Appointments.AddAsync(Appointment.Schedule(appt1Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eTech1, eBay1, brakeService.Id, targetSlot, "Pre-booking Bay 1"));
            }

            // Book BAY-02 with Tech 2
            if (!bay2.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
            {
                var appt2Id = AppointmentId.New();
                var eTech2 = tech2.Reserve(appt2Id, brakeService, targetSlot);
                var eBay2 = bay2.Reserve(appt2Id, brakeService, targetSlot);
                await db.Appointments.AddAsync(Appointment.Schedule(appt2Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eTech2, eBay2, brakeService.Id, targetSlot, "Pre-booking Bay 2"));
            }

            // Book Tech 3 with BAY-03 (Standard bay)
            if (!tech3.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
            {
                var appt3Id = AppointmentId.New();
                var tempStandardService = ServiceType.Define(ServiceTypeId.New(), "Basic Check", "Brakes", TimeSpan.FromHours(1), SkillLevel.Journeyman, BayCapability.Standard);
                var eTech3 = tech3.Reserve(appt3Id, tempStandardService, targetSlot);
                var eBay3 = bay3.Reserve(appt3Id, tempStandardService, targetSlot);
                await db.Appointments.AddAsync(Appointment.Schedule(appt3Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eTech3, eBay3, brakeService.Id, targetSlot, "Pre-booking Tech 3"));
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Scenario 'Both Resources Unavailable' configured successfully!",
                occupiedWindow = "2026-11-04T10:00:00Z to 11:00:00Z",
                liftBaysStatus = "All Lift bays (BAY-01, BAY-02) are 100% BOOKED",
                techniciansStatus = "All Brake-qualified technicians (John, Sarah, Mike) are 100% BOOKED",
                testInstructions = "Now try booking Standard Brake Service at '2026-11-04T10:00:00Z'. Both bays and technicians are unavailable. You will receive 409 Conflict (SlotUnavailable)."
            });
        })
        .WithTags("Development")
        .WithSummary("Prepares 'Both Resources Unavailable' scenario for Wednesday 2026-11-04 10:00 UTC.");

        app.MapPost("/api/v1/dev/scenario/setup-last-bay-contention", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var dealership = await db.Dealerships.FirstOrDefaultAsync(d => d.Id == new DealershipId(DealershipId));
            if (dealership is null)
                return Results.BadRequest(new { message = "Run /api/v1/dev/seed first." });

            var bay1 = await db.ServiceBays.Include(b => b.Bookings).FirstAsync(b => b.Id == new ServiceBayId(ServiceBayId));
            var bay2 = await db.ServiceBays.Include(b => b.Bookings).FirstOrDefaultAsync(b => b.Id == new ServiceBayId(ServiceBay2Id));
            if (bay2 is null)
            {
                bay2 = ServiceBay.Commission(new ServiceBayId(ServiceBay2Id), dealership.Id, "BAY-02", BayCapability.Lift);
                await db.ServiceBays.AddAsync(bay2);
            }

            var tech1 = await db.Technicians.Include(t => t.Bookings).FirstAsync(t => t.Id == new TechnicianId(TechnicianId));
            
            // Ensure Tech 2 (Sarah Connor) and Tech 3 (Mike FreeTech) exist and are free
            var tech2 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(Technician2Id));
            if (tech2 is null)
            {
                tech2 = Technician.Hire(new TechnicianId(Technician2Id), dealership.Id, ContactInfo.Create("Sarah", "Connor", "sarah.connor@dealership.com", "+1-555-0202"), "TECH-008");
                tech2.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
                await db.Technicians.AddAsync(tech2);
            }

            var tech3 = await db.Technicians.Include(t => t.Bookings).FirstOrDefaultAsync(t => t.Id == new TechnicianId(Technician3Id));
            if (tech3 is null)
            {
                tech3 = Technician.Hire(new TechnicianId(Technician3Id), dealership.Id, ContactInfo.Create("Mike", "FreeTech", "mike.free@dealership.com", "+1-555-0203"), "TECH-009");
                tech3.AddCertification(Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1), new DateOnly(2028, 1, 1)));
                await db.Technicians.AddAsync(tech3);
            }

            var customer = await db.Customers.FirstAsync();
            var vehicle = await db.Vehicles.FirstAsync();
            var brakeService = await db.ServiceTypes.FirstAsync(s => s.Id == new ServiceTypeId(BrakeServiceId));

            // Target contention slot: Thursday 2026-11-05 14:00 to 15:00 UTC
            var targetSlot = TimeSlot.From(new DateTimeOffset(2026, 11, 5, 14, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(1));

            // Pre-occupy BAY-01 with Tech 1
            if (!bay1.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
            {
                var apptId = AppointmentId.New();
                var eTech = tech1.Reserve(apptId, brakeService, targetSlot);
                var eBay = bay1.Reserve(apptId, brakeService, targetSlot);
                await db.Appointments.AddAsync(Appointment.Schedule(apptId, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eTech, eBay, brakeService.Id, targetSlot, "Pre-booking Bay 1 to leave only 1 bay"));
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Scenario 'Simultaneous Contention (Last Bay)' configured successfully!",
                contentionWindow = "2026-11-05T14:00:00Z to 15:00:00Z",
                bay1Status = "Pre-occupied (Booked)",
                remainingBays = "Only 1 bay remaining: BAY-02",
                techniciansStatus = "Multiple qualified technicians are available (Sarah Connor, Mike FreeTech)",
                testInstructions = "Fire Request A and Request B concurrently at '2026-11-05T14:00:00Z'. Exactly 1 will win (201 Created), and the other will get 409 Conflict."
            });
        })
        .WithTags("Development")
        .WithSummary("Pre-occupies BAY-01 so only 1 bay remains at 2026-11-05 14:00 UTC.");
        app.MapPost("/api/v1/dev/scenario/setup-overlap-scenarios", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var dealership = await db.Dealerships.FirstOrDefaultAsync(d => d.Id == new DealershipId(DealershipId));
            if (dealership is null)
                return Results.BadRequest(new { message = "Run /api/v1/dev/seed first." });

            var customer = await db.Customers.FirstAsync();
            var vehicle = await db.Vehicles.FirstAsync();
            var tech1 = await db.Technicians.Include(t => t.Bookings).FirstAsync(t => t.Id == new TechnicianId(TechnicianId));
            var bay1 = await db.ServiceBays.Include(b => b.Bookings).FirstAsync(b => b.Id == new ServiceBayId(ServiceBayId));
            var brakeService = await db.ServiceTypes.FirstAsync(s => s.Id == new ServiceTypeId(BrakeServiceId));

            // Ensure 30-min inspection service exists for enclosing window test
            var inspectionService = await db.ServiceTypes.FirstOrDefaultAsync(s => s.Id == new ServiceTypeId(QuickInspectionServiceId));
            if (inspectionService is null)
            {
                inspectionService = ServiceType.Define(
                    new ServiceTypeId(QuickInspectionServiceId),
                    "Quick Multi-Point Inspection",
                    "Brakes",
                    TimeSpan.FromMinutes(30),
                    SkillLevel.Journeyman,
                    BayCapability.Lift);
                await db.ServiceTypes.AddAsync(inspectionService);
            }

            // 1. Setup Head Collision Baseline: Book 09:30 - 10:30 UTC on Wednesday 2026-11-11
            var slot1 = TimeSlot.From(new DateTimeOffset(2026, 11, 11, 9, 30, 0, TimeSpan.Zero), TimeSpan.FromHours(1));
            if (!bay1.Bookings.Any(b => b.Slot.OverlapsWith(slot1)))
            {
                var appt1Id = AppointmentId.New();
                var eT = tech1.Reserve(appt1Id, brakeService, slot1);
                var eB = bay1.Reserve(appt1Id, brakeService, slot1);
                await db.Appointments.AddAsync(Appointment.Schedule(appt1Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eT, eB, brakeService.Id, slot1, "Pre-booking for Head Collision"));
            }

            // 2. Setup Tail Collision Baseline: Book 09:00 - 10:00 UTC on Thursday 2026-11-12
            var slot2 = TimeSlot.From(new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(1));
            if (!bay1.Bookings.Any(b => b.Slot.OverlapsWith(slot2)))
            {
                var appt2Id = AppointmentId.New();
                var eT = tech1.Reserve(appt2Id, brakeService, slot2);
                var eB = bay1.Reserve(appt2Id, brakeService, slot2);
                await db.Appointments.AddAsync(Appointment.Schedule(appt2Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eT, eB, brakeService.Id, slot2, "Pre-booking for Tail Collision"));
            }

            // 3. Setup Enclosing Window Baseline: Book 09:30 - 10:00 UTC (30 min) on Friday 2026-11-13
            var slot3 = TimeSlot.From(new DateTimeOffset(2026, 11, 13, 9, 30, 0, TimeSpan.Zero), TimeSpan.FromMinutes(30));
            if (!bay1.Bookings.Any(b => b.Slot.OverlapsWith(slot3)))
            {
                var appt3Id = AppointmentId.New();
                var eT = tech1.Reserve(appt3Id, inspectionService, slot3);
                var eB = bay1.Reserve(appt3Id, inspectionService, slot3);
                await db.Appointments.AddAsync(Appointment.Schedule(appt3Id, dealership.Id, customer.Id, vehicle.TakeSnapshot(), eT, eB, inspectionService.Id, slot3, "Pre-booking for Enclosing Window"));
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "All 3 temporal overlap scenarios configured successfully!",
                scenario1_HeadCollision = new { existingSlot = "2026-11-11 09:30-10:30 UTC", testRequest = "2026-11-11 09:00 UTC (1 hr)", expectedResult = "409 Conflict on 09:30-10:00 shared segment" },
                scenario2_TailCollision = new { existingSlot = "2026-11-12 09:00-10:00 UTC", testRequest = "2026-11-12 09:30 UTC (1 hr)", expectedResult = "409 Conflict on 09:30-10:00 shared segment" },
                scenario3_EnclosingWindow = new { existingSlot = "2026-11-13 09:30-10:00 UTC (30 min)", testRequest = "2026-11-13 09:00 UTC (2 hr transmission overhaul)", expectedResult = "409 Conflict - 2 hr slot encloses existing 30 min booking" }
            });
        })
        .WithTags("Development")
        .WithSummary("Prepares Head Collision, Tail Collision, and Enclosing Window test appointments.");

        app.MapPost("/api/v1/dev/seed-corrupted-zero-duration-service", async (UnifiedServiceSchedulerDbContext db) =>
        {
            var zeroDurationServiceId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var exists = await db.ServiceTypes.AnyAsync(s => s.Id == new ServiceTypeId(zeroDurationServiceId));
            if (!exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"ServiceTypes\" (\"Id\", \"Name\", \"ServiceCategory\", \"BaseDuration\", \"RequiredSkillLevel\", \"RequiredBayCapability\", \"Description\") " +
                    "VALUES ('00000000-0000-0000-0000-000000000001', 'Corrupted Zero Duration Service', 'Brakes', '00:00:00', 0, 1, 'Corrupted service type with 0 duration')");
            }

            return Results.Ok(new
            {
                message = "Corrupted service type with 00:00:00 duration seeded.",
                serviceTypeId = zeroDurationServiceId,
                testInstruction = "Attempt booking with this serviceTypeId to test TimeSlot constructor invariant rejection."
            });
        })
        .WithTags("Development")
        .WithSummary("Simulates corrupted service type with 00:00:00 duration to test domain invariant.");

        // View all database tables and current values directly in your browser
        app.MapGet("/api/v1/dev/tables", async (UnifiedServiceSchedulerDbContext db) =>
        {
            try
            {
                // Fetch tracked data into memory first to avoid EF Core SQL expression translation issues
                var dealershipsRaw = await db.Dealerships.AsNoTracking().ToListAsync();
                var customersRaw = await db.Customers.AsNoTracking().ToListAsync();
                var vehiclesRaw = await db.Vehicles.AsNoTracking().ToListAsync();
                var serviceTypesRaw = await db.ServiceTypes.AsNoTracking().ToListAsync();
                var techniciansRaw = await db.Technicians.Include(t => t.Bookings).AsNoTracking().ToListAsync();
                var serviceBaysRaw = await db.ServiceBays.Include(b => b.Bookings).AsNoTracking().ToListAsync();
                var appointmentsRaw = await db.Appointments.AsNoTracking().ToListAsync();

                // Safe in-memory projection
                var dealerships = dealershipsRaw.Select(d => new { id = d.Id?.Value, name = d.Name, address = d.Address }).ToList();
                var customers = customersRaw.Select(c => new { id = c.Id?.Value, name = c.Contact?.FullName, email = c.Contact?.Email, phone = c.Contact?.PhoneNumber }).ToList();
                var vehicles = vehiclesRaw.Select(v => new { id = v.Id?.Value, make = v.Make, model = v.Model, year = v.Year, vin = v.Vin, plate = v.LicensePlate }).ToList();
                var serviceTypes = serviceTypesRaw.Select(s => new { id = s.Id?.Value, name = s.Name, category = s.ServiceCategory, duration = s.BaseDuration, skill = s.RequiredSkillLevel.ToString(), capability = s.RequiredBayCapability.ToString() }).ToList();
                var technicians = techniciansRaw.Select(t => new { id = t.Id?.Value, name = t.Contact?.FullName, empNo = t.EmployeeNumber, active = t.IsActive, bookingsCount = t.Bookings.Count }).ToList();
                var serviceBays = serviceBaysRaw.Select(b => new { id = b.Id?.Value, code = b.BayCode, capability = b.Capability.ToString(), maintenance = b.IsUnderMaintenance, bookingsCount = b.Bookings.Count }).ToList();
                var appointments = appointmentsRaw.Select(a => new { id = a.Id?.Value, dealership = a.DealershipId?.Value, customer = a.CustomerId?.Value, tech = a.TechnicianId?.Value, bay = a.ServiceBayId?.Value, start = a.Slot?.Start, end = a.Slot?.End, status = a.Status.ToString(), notes = a.Notes }).ToList();

                return Results.Ok(new
                {
                    tableCounts = new
                    {
                        appointments = appointments.Count,
                        technicians = technicians.Count,
                        serviceBays = serviceBays.Count,
                        dealerships = dealerships.Count,
                        customers = customers.Count,
                        vehicles = vehicles.Count,
                        serviceTypes = serviceTypes.Count
                    },
                    tables = new
                    {
                        appointments,
                        technicians,
                        serviceBays,
                        dealerships,
                        customers,
                        vehicles,
                        serviceTypes
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error reading database tables",
                    detail: ex.Message);
            }
        })
        .WithTags("Development")
        .WithSummary("View all database tables, row counts, and current records directly in JSON.");

        return app;
    }
}
