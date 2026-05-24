-- Создание ролей PostgreSQL, таблиц, функции для watcher и начальных данных.
-- Выполнять файл - после подключения к базе notes_db.

BEGIN;

-- Технические роли PostgreSQL.

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notes_auth') THEN
        CREATE ROLE notes_auth LOGIN PASSWORD 'notes_auth_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notes_user') THEN
        CREATE ROLE notes_user LOGIN PASSWORD 'notes_user_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notes_admin') THEN
        CREATE ROLE notes_admin LOGIN PASSWORD 'notes_admin_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notes_analyst') THEN
        CREATE ROLE notes_analyst LOGIN PASSWORD 'notes_analyst_password';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'notes_watcher') THEN
        CREATE ROLE notes_watcher LOGIN PASSWORD 'notes_watcher_password';
    END IF;
END $$;

ALTER ROLE notes_auth LOGIN PASSWORD 'notes_auth_password';
ALTER ROLE notes_user LOGIN PASSWORD 'notes_user_password';
ALTER ROLE notes_admin LOGIN PASSWORD 'notes_admin_password';
ALTER ROLE notes_analyst LOGIN PASSWORD 'notes_analyst_password';
ALTER ROLE notes_watcher LOGIN PASSWORD 'notes_watcher_password';

-- Роли и учетные записи приложения.

CREATE TABLE IF NOT EXISTS app_roles (
    id SERIAL PRIMARY KEY,
    role_code VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(100) NOT NULL
);

INSERT INTO app_roles (role_code, title)
VALUES
    ('user', 'Пользователь'),
    ('admin', 'Администратор'),
    ('analyst', 'Аналитик')
ON CONFLICT (role_code) DO UPDATE
SET title = EXCLUDED.title;

CREATE TABLE IF NOT EXISTS app_users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NOT NULL REFERENCES app_roles(id),
    blocked BOOLEAN NOT NULL DEFAULT FALSE,
    registered_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_app_users_role_id ON app_users(role_id);

-- Начальный администратор для первого запуска.
-- Логин: admin
-- Пароль: admin123
-- Пароль хранится в виде BCrypt-хэша.
INSERT INTO app_users (username, password_hash, role_id, blocked)
VALUES (
    'admin',
    '$2a$11$RHQJbQxFTwLc1KaH7WJigOLvFMVMNmOQR65egg3dqyJWfQvmxm0RG',
    (SELECT id FROM app_roles WHERE role_code = 'admin'),
    FALSE
)
ON CONFLICT (username) DO NOTHING;

-- Пользовательские заметки.

CREATE TABLE IF NOT EXISTS notes (
    id SERIAL PRIMARY KEY,
    owner_id INT NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    modified_at TIMESTAMP NULL
);

CREATE INDEX IF NOT EXISTS idx_notes_owner_id ON notes(owner_id);
CREATE INDEX IF NOT EXISTS idx_notes_owner_created_at
ON notes(owner_id, created_at DESC, id DESC);

-- Журнал действий и событий безопасности.

CREATE TABLE IF NOT EXISTS audit_events (
    id SERIAL PRIMARY KEY,
    account_id INT NULL REFERENCES app_users(id) ON DELETE SET NULL,
    account_name VARCHAR(100) NULL,
    action_code VARCHAR(100) NOT NULL,
    details TEXT NOT NULL,
    object_name VARCHAR(100) NULL,
    event_time TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_events_event_time ON audit_events(event_time DESC, id DESC);
CREATE INDEX IF NOT EXISTS idx_audit_events_action_code ON audit_events(action_code);
CREATE INDEX IF NOT EXISTS idx_audit_events_account_id ON audit_events(account_id);

-- Устройства watcher-а и собранные метрики.

CREATE TABLE IF NOT EXISTS watcher_devices (
    id SERIAL PRIMARY KEY,
    device_uid VARCHAR(100) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    network_address VARCHAR(255) NULL,
    description TEXT NULL,
    enabled BOOLEAN NOT NULL DEFAULT TRUE,
    last_contact_at TIMESTAMP NULL,
    added_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_watcher_devices_display_name ON watcher_devices(display_name);
CREATE INDEX IF NOT EXISTS idx_watcher_devices_enabled ON watcher_devices(enabled);

CREATE TABLE IF NOT EXISTS device_metrics (
    id SERIAL PRIMARY KEY,
    device_id INT NOT NULL REFERENCES watcher_devices(id) ON DELETE CASCADE,
    cpu_load NUMERIC(5,2) NOT NULL,
    ram_load NUMERIC(5,2) NOT NULL,
    disk_load NUMERIC(5,2) NOT NULL,
    captured_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_device_metrics_cpu_load CHECK (cpu_load >= 0 AND cpu_load <= 100),
    CONSTRAINT chk_device_metrics_ram_load CHECK (ram_load >= 0 AND ram_load <= 100),
    CONSTRAINT chk_device_metrics_disk_load CHECK (disk_load >= 0 AND disk_load <= 100)
);

CREATE INDEX IF NOT EXISTS idx_device_metrics_device_captured_at
ON device_metrics(device_id, captured_at DESC, id DESC);

-- Функция приема метрик от watcher-а.

CREATE OR REPLACE FUNCTION record_watcher_metric(
    p_device_uid VARCHAR,
    p_display_name VARCHAR,
    p_cpu_load NUMERIC,
    p_ram_load NUMERIC,
    p_disk_load NUMERIC
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_device_id INT;
BEGIN
    IF p_device_uid IS NULL OR btrim(p_device_uid) = '' THEN
        RAISE EXCEPTION 'device_uid is required';
    END IF;

    INSERT INTO watcher_devices (device_uid, display_name, last_contact_at, enabled)
    VALUES (
        btrim(p_device_uid),
        COALESCE(NULLIF(btrim(p_display_name), ''), btrim(p_device_uid)),
        NOW(),
        TRUE
    )
    ON CONFLICT (device_uid) DO UPDATE
    SET display_name = COALESCE(NULLIF(EXCLUDED.display_name, ''), watcher_devices.display_name),
        last_contact_at = NOW(),
        enabled = TRUE
    RETURNING id INTO v_device_id;

    INSERT INTO device_metrics (device_id, cpu_load, ram_load, disk_load)
    VALUES (
        v_device_id,
        LEAST(GREATEST(COALESCE(p_cpu_load, 0), 0), 100),
        LEAST(GREATEST(COALESCE(p_ram_load, 0), 0), 100),
        LEAST(GREATEST(COALESCE(p_disk_load, 0), 0), 100)
    );
END;
$$;

-- Права доступа.

REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO notes_auth, notes_user, notes_admin, notes_analyst, notes_watcher;

REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON FUNCTION record_watcher_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) FROM PUBLIC;

-- Роль для регистрации и входа в приложение.
GRANT SELECT (id, role_code, title) ON app_roles TO notes_auth;
GRANT SELECT (id, username, password_hash, role_id, blocked, registered_at) ON app_users TO notes_auth;
GRANT INSERT (username, password_hash, role_id, blocked) ON app_users TO notes_auth;
GRANT INSERT (account_id, account_name, action_code, details, object_name) ON audit_events TO notes_auth;
GRANT USAGE, SELECT ON SEQUENCE app_users_id_seq, audit_events_id_seq TO notes_auth;

-- Роль обычного пользователя.
GRANT SELECT (id, role_code, title) ON app_roles TO notes_user;
GRANT SELECT (id, username, role_id, blocked, registered_at) ON app_users TO notes_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON notes TO notes_user;
GRANT INSERT (account_id, account_name, action_code, details, object_name) ON audit_events TO notes_user;
GRANT USAGE, SELECT ON SEQUENCE notes_id_seq, audit_events_id_seq TO notes_user;

-- Роль администратора.
GRANT SELECT, INSERT, UPDATE, DELETE ON app_roles TO notes_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON app_users TO notes_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON notes TO notes_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON audit_events TO notes_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON watcher_devices TO notes_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON device_metrics TO notes_admin;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO notes_admin;

-- Роль аналитика для просмотра статистики.
GRANT SELECT (id, role_code, title) ON app_roles TO notes_analyst;
GRANT SELECT (id, username, role_id, blocked, registered_at) ON app_users TO notes_analyst;
GRANT SELECT, INSERT, UPDATE, DELETE ON watcher_devices TO notes_analyst;
GRANT SELECT ON device_metrics TO notes_analyst;
GRANT SELECT, INSERT ON audit_events TO notes_analyst;
GRANT USAGE, SELECT ON SEQUENCE watcher_devices_id_seq, audit_events_id_seq TO notes_analyst;

-- Роль watcher-а. Агент может только передавать метрики через функцию.
GRANT EXECUTE ON FUNCTION record_watcher_metric(VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC) TO notes_watcher;

COMMIT;
