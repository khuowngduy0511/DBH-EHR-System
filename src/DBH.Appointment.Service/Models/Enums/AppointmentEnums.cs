namespace DBH.Appointment.Service.Models.Enums;

/// <summary>
/// Appointment status lifecycle
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Appointment has been requested but not yet confirmed</summary>
    PENDING,

    /// <summary>Appointment is confirmed by the provider</summary>
    CONFIRMED,

    /// <summary>Patient has checked in</summary>
    CHECKED_IN,

    /// <summary>Appointment is currently in progress</summary>
    IN_PROGRESS,

    /// <summary>Appointment has been completed</summary>
    COMPLETED,

    /// <summary>Appointment was cancelled</summary>
    CANCELLED,

    /// <summary>Patient did not show up</summary>
    NO_SHOW,

    /// <summary>Appointment was rescheduled (new appointment created)</summary>
    RESCHEDULED
}

/// <summary>
/// Type of appointment
/// </summary>
public enum AppointmentType
{
    /// <summary>Regular check-up or consultation</summary>
    CONSULTATION,

    /// <summary>Follow-up visit</summary>
    FOLLOW_UP,

    /// <summary>Urgent/emergency visit</summary>
    URGENT,

    /// <summary>Routine health screening</summary>
    SCREENING,

    /// <summary>Procedure or treatment</summary>
    PROCEDURE,

    /// <summary>Telemedicine/virtual visit</summary>
    TELEMEDICINE,

    /// <summary>Lab work or diagnostic test</summary>
    LAB_WORK
}

/// <summary>
/// Priority level for the appointment
/// </summary>
public enum AppointmentPriority
{
    LOW,
    NORMAL,
    HIGH,
    URGENT
}
