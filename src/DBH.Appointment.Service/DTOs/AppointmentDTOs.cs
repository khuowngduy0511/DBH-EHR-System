using System.ComponentModel.DataAnnotations;
using DBH.Appointment.Service.Models.Enums;

namespace DBH.Appointment.Service.DTOs;

// =============================================================================
// Appointment DTOs
// =============================================================================

public class CreateAppointmentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public Guid OrgId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }
}

public class UpdateAppointmentRequest
{
    public AppointmentStatus? Status { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class AppointmentResponse
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid OrgId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Optional navigation summary
    public int EncounterCount { get; set; }
}

// =============================================================================
// Encounter DTOs
// =============================================================================

public class CreateEncounterRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public Guid AppointmentId { get; set; }

    [Required]
    public Guid OrgId { get; set; }

    public string? Notes { get; set; }
}

public class UpdateEncounterRequest
{
    public string? Notes { get; set; }
}

public class EncounterResponse
{
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid OrgId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// =============================================================================
// Common Response
// =============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
