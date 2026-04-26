package models

import (
	"github.com/google/uuid"
	"time"
)

type User struct {
	Id           uuid.UUID `db:"Id"`
	Email        string    `db:"Email"`
	PasswordHash string    `db:"PasswordHash"`
	Role         string    `db:"Role"`
	IsBlocked    bool      `db:"IsBlocked"`
}

type Task struct {
	Id          uuid.UUID `db:"Id"`
	UserId      uuid.UUID `db:"UserId"`
	Title       string    `db:"Title"`
	Description *string   `db:"Description"`
	DeadlineAt  time.Time `db:"DeadlineAt"`
	Status      string    `db:"Status"`
	Priority    string    `db:"Priority"`
	CreatedAt   time.Time `db:"CreatedAt"`
}

type NotificationSetting struct {
	Id                uuid.UUID `db:"Id"`
	TaskId            uuid.UUID `db:"TaskId"`
	RemindBeforeValue int       `db:"RemindBeforeValue"`
	RemindBeforeUnit  string    `db:"RemindBeforeUnit"`
	Channel           string    `db:"Channel"`
}

type Notification struct {
	Id             uuid.UUID  `db:"Id"`
	TaskId         uuid.UUID  `db:"TaskId"`
	UserId         uuid.UUID  `db:"UserId"`
	Message        string     `db:"Message"`
	Channel        string     `db:"Channel"`
	DeliveryStatus string     `db:"DeliveryStatus"`
	CreatedAt      time.Time  `db:"CreatedAt"`
	SentAt         *time.Time `db:"SentAt"`
}

type NotificationDeliveryHistory struct {
	Id             uuid.UUID `db:"Id"`
	NotificationId uuid.UUID `db:"NotificationId"`
	AttemptNumber  int       `db:"AttemptNumber"`
	Status         string    `db:"Status"`
	AttemptedAt    time.Time `db:"AttemptedAt"`
}

type TaskWithSettings struct {
	Task
	Settings  []NotificationSetting
	UserEmail string
}
