package repository

import (
	"context"
	"fmt"
	"notifier/internal/models"
	"time"

	"github.com/google/uuid"
	"github.com/jmoiron/sqlx"
	"go.uber.org/zap"
)

type TaskRepo struct {
	db  *sqlx.DB
	log *zap.Logger
}

func NewTaskRepo(db *sqlx.DB, log *zap.Logger) *TaskRepo {
	return &TaskRepo{db: db, log: log}
}

func (r *TaskRepo) GetTasksNeedingNotification(ctx context.Context) ([]models.TaskWithSettings, error) {
	query := `
        SELECT 
            t.id, t.user_id, t.title, t.description, t.deadline_at, t.status, t.priority,
            u.email,
            ns.id, ns.remind_before_value, ns.remind_before_unit, ns.channel
        FROM tasks t
        JOIN users u ON u.id = t.user_id
        JOIN notification_settings ns ON ns.task_id = t.id
        WHERE t.status != 'done'
          AND t.deadline_at > NOW() - INTERVAL '30 days'
        ORDER BY t.id, ns.id
    `

	rows, err := r.db.QueryxContext(ctx, query)
	if err != nil {
		return nil, fmt.Errorf("query tasks: %w", err)
	}
	defer rows.Close()

	taskMap := make(map[uuid.UUID]*models.TaskWithSettings)

	for rows.Next() {
		var task models.Task
		var email string
		var setting models.NotificationSetting

		err = rows.Scan(
			&task.Id, &task.UserId, &task.Title, &task.Description, &task.DeadlineAt, &task.Status, &task.Priority,
			&email,
			&setting.Id, &setting.RemindBeforeValue, &setting.RemindBeforeUnit, &setting.Channel,
		)
		if err != nil {
			r.log.Warn("scan error", zap.Error(err))
			continue
		}
		setting.TaskId = task.Id // заполняем TaskId для настройки

		if _, ok := taskMap[task.Id]; !ok {
			taskMap[task.Id] = &models.TaskWithSettings{
				Task:      task,
				UserEmail: email,
				Settings:  []models.NotificationSetting{},
			}
		}
		taskMap[task.Id].Settings = append(taskMap[task.Id].Settings, setting)
	}

	result := make([]models.TaskWithSettings, 0, len(taskMap))
	for _, v := range taskMap {
		result = append(result, *v)
	}
	return result, nil
}

// Проверка, отправляли ли уведомление за последние 6 часов (когда появится таблица notifications)
// Пока можно закомментировать или временно всегда возвращать false.
func (r *TaskRepo) IsAlreadyNotified(ctx context.Context, taskID, settingID uuid.UUID) (bool, error) {
	// TODO: после создания таблицы notifications написать реальный запрос.
	return false, nil
}
