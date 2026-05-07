CREATE TABLE IF NOT EXISTS players (
    player_id VARCHAR(64) PRIMARY KEY,
    device_id VARCHAR(128) NOT NULL,
    platform VARCHAR(32) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS player_sessions (
    token VARCHAR(128) PRIMARY KEY,
    player_id VARCHAR(64) NOT NULL REFERENCES players(player_id),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS roles (
    role_id VARCHAR(64) PRIMARY KEY,
    player_id VARCHAR(64) NOT NULL REFERENCES players(player_id),
    name VARCHAR(16) NOT NULL,
    level INT NOT NULL DEFAULT 1,
    class_id INT NOT NULL DEFAULT 1,
    gold BIGINT NOT NULL DEFAULT 100,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS inventory_items (
    id SERIAL PRIMARY KEY,
    player_id VARCHAR(64) NOT NULL,
    template_id INT NOT NULL,
    name VARCHAR(64) NOT NULL,
    type VARCHAR(16) NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    slot_index INT NOT NULL DEFAULT 0,
    is_equipped BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_roles_player ON roles(player_id);
CREATE INDEX IF NOT EXISTS idx_sessions_player ON player_sessions(player_id);
CREATE INDEX IF NOT EXISTS idx_inventory_player ON inventory_items(player_id);
