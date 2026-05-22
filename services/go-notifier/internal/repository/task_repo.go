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
            t."Id", t."UserId", t."Title", t."Description", t."DeadlineAt", t."Status", t."Priority",
            u."Email",
            ns."Id", ns."RemindBeforeValue", ns."RemindBeforeUnit", ns."Channel"
        FROM "Tasks" t
        JOIN "Users" u ON u."Id" = t."UserId"
        JOIN "NotificationSettings" ns ON ns."TaskId" = t."Id"
        WHERE t."Status" != 'Done'
          AND u."IsBlocked" = false
          AND t."DeadlineAt" > NOW() - INTERVAL '30 days'
        ORDER BY t."Id", ns."Id"
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
		setting.TaskId = task.Id

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

// CreateNotification создаёт запись в таблице Notifications
func (r *TaskRepo) CreateNotification(ctx context.Context, notif models.Notification) (uuid.UUID, error) {
	query := `
		INSERT INTO "Notifications" ("Id", "TaskId", "UserId", "Message", "Channel", "DeliveryStatus", "CreatedAt")
		VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, NOW())
		RETURNING "Id"
	`
	var id uuid.UUID
	err := r.db.QueryRowContext(ctx, query,
		notif.TaskId, notif.UserId, notif.Message, notif.Channel, notif.DeliveryStatus).Scan(&id)
	return id, err
}

// UpdateNotificationStatus обновляет статус уведомления и время отправки
func (r *TaskRepo) UpdateNotificationStatus(ctx context.Context, id uuid.UUID, status string, sentAt *time.Time) error {
	query := `UPDATE "Notifications" SET "DeliveryStatus"=$1, "SentAt"=$2 WHERE "Id"=$3`
	_, err := r.db.ExecContext(ctx, query, status, sentAt, id)
	return err
}

// LogDeliveryAttempt записывает попытку отправки в историю
func (r *TaskRepo) LogDeliveryAttempt(ctx context.Context, notifId uuid.UUID, attempt int, status string) error {
	query := `
		INSERT INTO "NotificationDeliveryHistories" ("Id", "NotificationId", "AttemptNumber", "Status", "AttemptedAt")
		VALUES (gen_random_uuid(), $1, $2, $3, NOW())
	`
	_, err := r.db.ExecContext(ctx, query, notifId, attempt, status)
	return err
}

// IsAlreadyNotified проверяет, было ли уведомление для задачи и канала после момента remindAt.
func (r *TaskRepo) IsAlreadyNotified(ctx context.Context, taskId uuid.UUID, channel string, since time.Time) (bool, error) {
	query := `
		SELECT EXISTS(
			SELECT 1 FROM "Notifications"
			WHERE "TaskId" = $1
			  AND LOWER("Channel") = LOWER($2)
			  AND "CreatedAt" >= $3
		)
	`
	var exists bool
	err := r.db.GetContext(ctx, &exists, query, taskId, channel, since)
	return exists, err
}
