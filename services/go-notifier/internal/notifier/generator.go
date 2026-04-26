package notifier

import (
	"fmt"
	"notifier/internal/models"
)

// GenerateMessage создаёт текст уведомления для задачи.
func GenerateMessage(task models.Task) string {
	localDeadline := task.DeadlineAt.Local()
	return fmt.Sprintf(
		"Напоминание: задача '%s' должна быть выполнена до %s. Не пропустите дедлайн!",
		task.Title,
		localDeadline.Format("02.01.2006 15:04"),
	)
}
