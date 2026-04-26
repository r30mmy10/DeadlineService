package main

import (
	"context"
	"fmt"
	"notifier/internal/email"
	"notifier/internal/notifier"
	"notifier/internal/repository"
	"notifier/internal/worker"
	"os/signal"
	"syscall"
	"time"

	"github.com/jmoiron/sqlx"
	_ "github.com/lib/pq"
	"github.com/spf13/viper"
	"go.uber.org/zap"
)

func main() {
	// 1. Логгер
	logger, _ := zap.NewProduction()
	defer logger.Sync()

	// 2. Конфиг
	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	viper.AddConfigPath(".")
	if err := viper.ReadInConfig(); err != nil {
		logger.Fatal("failed to read config", zap.Error(err))
	}

	// 3. Подключение к БД
	dsn := fmt.Sprintf("host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
		viper.GetString("database.host"),
		viper.GetInt("database.port"),
		viper.GetString("database.user"),
		viper.GetString("database.password"),
		viper.GetString("database.dbname"),
		viper.GetString("database.sslmode"),
	)
	db, err := sqlx.Connect("postgres", dsn)
	if err != nil {
		logger.Fatal("failed to connect to db", zap.Error(err))
	}
	defer db.Close()
	logger.Info("connected to database")

	// 4. Инициализация компонентов
	repo := repository.NewTaskRepo(db, logger)
	emailSender := email.NewMockEmailSender(logger) // пока мок, потом заменим на реальный SMTP
	notif := notifier.NewNotifier(repo, emailSender, logger)
	proc := worker.NewProcessor(repo, notif, logger)
	interval := viper.GetInt("worker.interval_seconds")
	sched := worker.NewScheduler(interval, proc, logger)

	// 5. Graceful shutdown
	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	// 6. Запуск шедулера в отдельной горутине
	go sched.Run(ctx)

	// 7. Ожидание сигнала завершения
	<-ctx.Done()
	logger.Info("shutting down gracefully, waiting for current iteration")
	time.Sleep(2 * time.Second)
}
