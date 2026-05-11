CREATE TABLE IF NOT EXISTS schema_migrations (
    version    VARCHAR(50)  NOT NULL PRIMARY KEY,
    name       VARCHAR(255) NOT NULL,
    applied_at TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO schema_migrations (version, name)
VALUES ('001', 'create_schema_migrations');
