using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Tests.Fakes;

/// <summary>
/// Domain test data factory for predictable, realistic domain entities and value objects.
/// </summary>
public static class TestDomainDataBuilder
{
    public static Dealership CreateDealership(DealershipId? id = null)
    {
        return Dealership.Register(
            id ?? DealershipId.New(),
            "Downtown Automotive Center",
            "100 Motorway Ave, Detroit, MI",
            TimeZoneInfo.Utc);
    }

    public static Customer CreateCustomer(CustomerId? id = null)
    {
        return Customer.Create(
            id ?? CustomerId.New(),
            ContactInfo.Create("Jane", "Doe", "jane.doe@example.com", "+1-555-0199"));
    }

    public static Vehicle CreateVehicle(CustomerId ownerId, VehicleId? id = null)
    {
        return Vehicle.Register(
            id ?? VehicleId.New(),
            ownerId,
            "Tesla",
            "Model Y",
            2024,
            "1HGCR2F83HA000123",
            "EV-SCHED1");
    }

    public static ServiceType CreateStandardMaintenance(ServiceTypeId? id = null)
    {
        return ServiceType.Define(
            id ?? ServiceTypeId.New(),
            "Standard Maintenance",
            "Brakes",
            TimeSpan.FromHours(1),
            SkillLevel.Journeyman,
            BayCapability.Lift,
            "Comprehensive 1-hour brake inspection and pad replacement.");
    }

    public static ServiceType CreateHighVoltageService(ServiceTypeId? id = null)
    {
        return ServiceType.Define(
            id ?? ServiceTypeId.New(),
            "HV Battery Service",
            "EV Powertrain",
            TimeSpan.FromHours(2),
            SkillLevel.Specialist,
            BayCapability.EvCharging,
            "High-voltage battery diagnostics and contactor replacement.");
    }

    public static ServiceBay CreateBay(
        DealershipId dealershipId,
        string bayCode = "BAY-01",
        BayCapability capability = BayCapability.Lift,
        ServiceBayId? id = null)
    {
        return ServiceBay.Commission(
            id ?? ServiceBayId.New(),
            dealershipId,
            bayCode,
            capability);
    }

    public static Technician CreateTechnician(
        DealershipId dealershipId,
        string employeeNumber = "TECH-007",
        TechnicianId? id = null,
        IEnumerable<Certification>? certifications = null)
    {
        var tech = Technician.Hire(
            id ?? TechnicianId.New(),
            dealershipId,
            ContactInfo.Create("John", "Smith", "john.smith@dealership.com", "+1-555-0200"),
            employeeNumber);

        if (certifications is not null)
        {
            foreach (var cert in certifications)
            {
                tech.AddCertification(cert);
            }
        }

        return tech;
    }
}
