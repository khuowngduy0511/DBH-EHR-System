namespace DBH.Appointment.Service.Models.Enums;

/// <summary>
/// Appointment status lifecycle — stored as varchar in ERD
/// </summary>
public enum AppointmentStatus
{
    PENDING,
    CONFIRMED,
    CHECKED_IN,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED,
    NO_SHOW,
    RESCHEDULED
}
