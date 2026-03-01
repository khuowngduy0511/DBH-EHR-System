namespace DBH.Notification.Service.DTOs;

public class UpdatePreferencesRequest
{
    public bool? EhrAccessEnabled { get; set; }
    public bool? ConsentRequestEnabled { get; set; }
    public bool? EhrUpdateEnabled { get; set; }
    public bool? AppointmentReminderEnabled { get; set; }
    public bool? SecurityAlertEnabled { get; set; }
    public bool? SystemNotificationEnabled { get; set; }
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public string? QuietTimeStart { get; set; } 
    public string? QuietTimeEnd { get; set; }    
}

public class PreferencesResponse
{
    public Guid Id { get; set; }
    public string UserDid { get; set; }
    public bool EhrAccessEnabled { get; set; }
    public bool ConsentRequestEnabled { get; set; }
    public bool EhrUpdateEnabled { get; set; }
    public bool AppointmentReminderEnabled { get; set; }
    public bool SecurityAlertEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public string? QuietTimeStart { get; set; }
    public string? QuietTimeEnd { get; set; }
}
