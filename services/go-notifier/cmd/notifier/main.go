package main

import (
	"context"
	"fmt"
	"notifier/internal/email"
	"notifier/internal/notifier"
	"notifier/internal/repository"
	"notifier/internal/worker"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"github.com/jmoiron/sqlx"
	_ "github.com/lib/pq"
	"github.com/spf13/viper"
	"go.uber.org/zap"
)

func main() {
	logger, _ := zap.NewProduction()
	defer logger.Sync()

	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	viper.AddConfigPath(".")
	viper.AutomaticEnv()
	viper.SetEnvKeyReplacer(strings.NewReplacer(".", "_"))
	bindEnvVars()

	if err := viper.ReadInConfig(); err != nil {
		logger.Fatal("failed to read config", zap.Error(err))
	}

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

	repo := repository.NewTaskRepo(db, logger)

	emailSender, err := email.NewSenderFromConfig(viper.GetViper(), logger)
	if err != nil {
		logger.Fatal("failed to init email sender", zap.Error(err))
	}

	notif := notifier.NewNotifier(repo, emailSender, logger)
	proc := worker.NewProcessor(repo, notif, logger)
	interval := viper.GetInt("worker.interval_seconds")
	sched := worker.NewScheduler(interval, proc, logger)

	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	go sched.Run(ctx)

	<-ctx.Done()
	logger.Info("shutting down gracefully, waiting for current iteration")
	time.Sleep(2 * time.Second)
}

func bindEnvVars() {
	_ = viper.BindEnv("database.host", "DB_HOST")
	_ = viper.BindEnv("database.port", "DB_PORT")
	_ = viper.BindEnv("database.user", "DB_USER")
	_ = viper.BindEnv("database.password", "DB_PASSWORD")
	_ = viper.BindEnv("database.dbname", "DB_NAME")
	_ = viper.BindEnv("database.sslmode", "DB_SSLMODE")
	_ = viper.BindEnv("worker.interval_seconds", "WORKER_INTERVAL")
	_ = viper.BindEnv("email.mode", "EMAIL_MODE")
	_ = viper.BindEnv("email.smtp.host", "SMTP_HOST")
	_ = viper.BindEnv("email.smtp.port", "SMTP_PORT")
	_ = viper.BindEnv("email.smtp.username", "SMTP_USERNAME")
	_ = viper.BindEnv("email.smtp.password", "SMTP_PASSWORD")
	_ = viper.BindEnv("email.smtp.from", "SMTP_FROM")
	_ = viper.BindEnv("email.smtp.tls", "SMTP_TLS")
}
