namespace UnifiedServiceScheduler.Domain.Enums;

public enum AppointmentStatus
{
    Requested,      // Created but not yet confirmed
    Confirmed,      // Technician + Bay locked; resources reserved
    InProgress,     // Technician has checked in the vehicle
    Completed,      // Service finished, bay released
    Cancelled,      // Customer or dealership cancelled
    NoShow,         // Customer did not arrive
}
