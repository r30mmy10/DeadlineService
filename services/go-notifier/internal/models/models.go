package models

import (
	"github.com/google/uuid"
	"time"
)

type User struct {
	Id           uuid.UUID `db:"id"`
	Email        string    `db:"email"`
	PasswordHash string    `db:"password_hash"`
	Role         string    `db:"role"`
	IsBlocked    bool      `db:"is_blocked"`
}

type Task struct {
	Id          uuid.UUID `db:"id"`
	UserId      uuid.UUID `db:"user_id"`
	Title       string    `db:"title"`
	Description *string   `db:"description"`
	DeadlineAt  time.Time `db:"deadline_at"`
	Status      string    `db:"status"`
	Priority    string    `db:"priority"`
	CreatedAt   time.Time `db:"created_at"`
}

type NotificationSetting struct {
	Id                uuid.UUID `db:"id"`
	TaskId            uuid.UUID `db:"task_id"`
	RemindBeforeValue int       `db:"remind_before_value"`
	RemindBeforeUnit  string    `db:"remind_before_unit"`
	Channel           string    `db:"channel"`
}

// type Notification struct { ... }
// type DeliveryAttempt struct { ... }

type TaskWithSettings struct {
	Task
	Settings  []NotificationSetting
	UserEmail string
}
