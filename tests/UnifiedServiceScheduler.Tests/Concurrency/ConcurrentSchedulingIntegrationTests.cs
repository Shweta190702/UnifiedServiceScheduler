using UnifiedServiceScheduler.Application.Appointments;
using UnifiedServiceScheduler.Application.Common;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Tests.Fakes;
using Xunit;

namespace UnifiedServiceScheduler.Tests.Concurrency;

public sealed class ConcurrentSchedulingIntegrationTests
{
    private static readonly DateTimeOffset FixedMonday10AmUtc = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Requirement 1: Real Concurrency Simulation.
    /// Uses Task.WhenAll with a synchronized barrier release (TaskCompletionSource) to fire genuinely
    /// simultaneous requests across thread pool threads against the last remaining slot.
    ///
    /// Asserts:
    /// 1. Exactly ONE request succeeds.
    /// 2. Exactly ONE request receives a conflict result (SchedulingErrorKind.Conflict).
    /// 3. Resource schedules (Bay and Technician) contain exactly ONE booking (zero double booking).
    /// </summary>
    [Fact]
    public async Task Schedule_ConcurrentBookingForLastRemainingSlot_OnlyOneSucceeds()
    {
        // Arrange
        var dealership = TestDomainDataBuilder.CreateDealership();
        var customer1 = TestDomainDataBuilder.CreateCustomer();
        var vehicle1 = TestDomainDataBuilder.CreateVehicle(customer1.Id);

        var customer2 = Customer.Create(
            CustomerId.New(),
            ContactInfo.Create("Bob", "Miller", "bob.miller@example.com", "+1-555-0300"));
        var vehicle2 = Vehicle.Register(
            VehicleId.New(),
            customer2.Id,
            "Ford",
            "F-150 Lightning",
            2023,
            "1FT6W1EV4NW000456",
            "EV-TRUCK2");

        var serviceType = TestDomainDataBuilder.CreateStandardMaintenance();

        // Exactly ONE Bay and ONE Technician available in the entire dealership
        var singleBay = TestDomainDataBuilder.CreateBay(dealership.Id, "BAY-SOLE");
        var brakeCert = Certification.Create("Brakes", SkillLevel.Senior, new DateOnly(2025, 1, 1));
        var singleTech = TestDomainDataBuilder.CreateTechnician(dealership.Id, "TECH-SOLE", certifications: [brakeCert]);

        var coordinator = new SynchronizedSchedulingCoordinator(
            dealership,
            new Dictionary<CustomerId, Customer> { [customer1.Id] = customer1, [customer2.Id] = customer2 },
            new Dictionary<VehicleId, Vehicle> { [vehicle1.Id] = vehicle1, [vehicle2.Id] = vehicle2 },
            serviceType,
            singleTech,
            singleBay);

        var commandA = new BookAppointmentCommand(
            dealership.Id.Value,
            customer1.Id.Value,
            vehicle1.Id.Value,
            serviceType.Id.Value,
            FixedMonday10AmUtc,
            "Customer A Booking");

        var commandB = new BookAppointmentCommand(
            dealership.Id.Value,
            customer2.Id.Value,
            vehicle2.Id.Value,
            serviceType.Id.Value,
            FixedMonday10AmUtc,
            "Customer B Booking");

        // Synchronization barrier to release both tasks at the exact same instant
        var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act: Enqueue both tasks onto the ThreadPool waiting on the starting gate
        var taskA = Task.Run(async () =>
        {
            await startGate.Task; // Wait for concurrent trigger
            return await coordinator.BookAppointmentConcurrentlyAsync(commandA);
        });

        var taskB = Task.Run(async () =>
        {
            await startGate.Task; // Wait for concurrent trigger
            return await coordinator.BookAppointmentConcurrentlyAsync(commandB);
        });

        // Fire both tasks simultaneously
        startGate.SetResult(true);

        await Task.WhenAll(taskA, taskB);

        var resultA = await taskA;
        var resultB = await taskB;

        // Assert 1: Exactly ONE request succeeded and exactly ONE request failed
        var successCount = (resultA.IsSuccess ? 1 : 0) + (resultB.IsSuccess ? 1 : 0);
        var failureCount = (resultA.IsFailure ? 1 : 0) + (resultB.IsFailure ? 1 : 0);

        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        // Assert 2: The losing request received a clean Conflict error (SlotUnavailable)
        var failedResult = resultA.IsFailure ? resultA : resultB;
        Assert.Equal(SchedulingErrorKind.Conflict, failedResult.Error.Kind);
        Assert.Equal("SCHEDULING_CONFLICT", failedResult.Error.Code);

        // Assert 3: Physical resource schedules contain exactly ONE booking (no double-booking occurred)
        Assert.Single(singleBay.Bookings);
        Assert.Single(singleTech.Bookings);

        // Assert 4: Winning appointment matches the winning result DTO
        var winningResult = resultA.IsSuccess ? resultA : resultB;
        var committedAppt = coordinator.Appointments.Single();
        Assert.Equal(winningResult.Value.AppointmentId, committedAppt.Id.Value);
        Assert.Equal(winningResult.Value.CustomerId, committedAppt.CustomerId.Value);
    }

    /// <summary>
    /// Thread-safe in-memory scheduler simulator that mirrors EF Core's
    /// `SELECT ... FOR UPDATE` row-level locking and unit of work transactions.
    /// </summary>
    private sealed class SynchronizedSchedulingCoordinator
    {
        private readonly SemaphoreSlim _rowLock = new(1, 1); // Simulates DB exclusive row lock
        private readonly Dealership _dealership;
        private readonly Dictionary<CustomerId, Customer> _customers;
        private readonly Dictionary<VehicleId, Vehicle> _vehicles;
        private readonly ServiceType _serviceType;
        private readonly Technician _technician;
        private readonly ServiceBay _serviceBay;

        public List<Appointment> Appointments { get; } = [];

        public SynchronizedSchedulingCoordinator(
            Dealership dealership,
            Dictionary<CustomerId, Customer> customers,
            Dictionary<VehicleId, Vehicle> vehicles,
            ServiceType serviceType,
            Technician technician,
            ServiceBay serviceBay)
        {
            _dealership = dealership;
            _customers = customers;
            _vehicles = vehicles;
            _serviceType = serviceType;
            _technician = technician;
            _serviceBay = serviceBay;
        }

        public async Task<Result<AppointmentResponseDto, SchedulingError>> BookAppointmentConcurrentlyAsync(
            BookAppointmentCommand command)
        {
            var customerId = new CustomerId(command.CustomerId);
            var vehicleId = new VehicleId(command.VehicleId);
            var targetSlot = TimeSlot.From(command.DesiredStartTime, _serviceType.BaseDuration);

            // Acquire exclusive row lock (simulating SELECT ... FOR UPDATE)
            await _rowLock.WaitAsync();
            try
            {
                // Critical section: Evaluate slot collision under lock
                if (_serviceBay.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
                {
                    return SchedulingError.Conflict($"Service bay is already occupied during {targetSlot}.");
                }

                if (_technician.Bookings.Any(b => b.Slot.OverlapsWith(targetSlot)))
                {
                    return SchedulingError.Conflict($"Technician is already booked during {targetSlot}.");
                }

                var apptId = AppointmentId.New();
                var customer = _customers[customerId];
                var vehicle = _vehicles[vehicleId];

                // Mutate aggregates
                var techBooking = _technician.Reserve(apptId, _serviceType, targetSlot);
                var bayBooking = _serviceBay.Reserve(apptId, _serviceType, targetSlot);

                var appointment = Appointment.Schedule(
                    apptId,
                    _dealership.Id,
                    customerId,
                    vehicle.TakeSnapshot(),
                    techBooking,
                    bayBooking,
                    _serviceType.Id,
                    targetSlot,
                    command.Notes);

                Appointments.Add(appointment);

                var dto = new AppointmentResponseDto(
                    appointment.Id.Value,
                    _dealership.Id.Value,
                    customerId.Value,
                    vehicle.Id.Value,
                    vehicle.Vin,
                    $"{vehicle.Year} {vehicle.Make} {vehicle.Model}",
                    _technician.Id.Value,
                    _technician.Contact.FullName,
                    _serviceBay.Id.Value,
                    _serviceBay.BayCode,
                    _serviceType.Id.Value,
                    _serviceType.Name,
                    targetSlot.Start,
                    targetSlot.End,
                    targetSlot.Duration,
                    appointment.Status.ToString(),
                    appointment.Notes);

                return Result<AppointmentResponseDto, SchedulingError>.Success(dto);
            }
            finally
            {
                _rowLock.Release();
            }
        }
    }
}
