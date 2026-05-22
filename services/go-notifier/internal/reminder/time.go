package reminder

import (
	"strings"
	"time"
)

// CalculateRemindAt возвращает момент отправки напоминания относительно дедлайна.
// unit согласован с C# API: Hours, Minutes, Days (регистр не важен).
func CalculateRemindAt(deadline time.Time, value int, unit string) time.Time {
	if value <= 0 {
		return time.Time{}
	}

	switch strings.ToLower(strings.TrimSpace(unit)) {
	case "minute", "minutes":
		return deadline.Add(-time.Duration(value) * time.Minute)
	case "hour", "hours":
		return deadline.Add(-time.Duration(value) * time.Hour)
	case "day", "days":
		return deadline.Add(-time.Duration(value) * 24 * time.Hour)
	default:
		return time.Time{}
	}
}

// IsDue возвращает true, если наступило время напоминания.
func IsDue(now, remindAt time.Time) bool {
	if remindAt.IsZero() {
		return false
	}
	return !now.Before(remindAt)
}
