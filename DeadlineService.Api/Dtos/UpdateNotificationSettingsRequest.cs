namespace DeadlineService.Api.Dtos;

public class UpdateNotificationSettingsRequest
{
    public int RemindBeforeValue { get; set; }
    public string RemindBeforeUnit { get; set; } = "Hours";
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyInApp { get; set; } = true;
}
