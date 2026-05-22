namespace DeadlineService.Api.Dtos;

public class TaskReminderDto
{
    public int RemindBeforeValue { get; set; } = 1;
    public string RemindBeforeUnit { get; set; } = "Hours";
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyInApp { get; set; } = true;
}
