PRAGMA foreign_keys = ON;

-- =========================
-- categories
-- =========================
CREATE TABLE IF NOT EXISTS categories (
    id               TEXT    NOT NULL PRIMARY KEY,
    name             TEXT    NOT NULL,
    is_system        INTEGER NOT NULL,
    is_user_editable INTEGER NOT NULL
);

-- =========================
-- accounts
-- =========================
CREATE TABLE IF NOT EXISTS accounts (
    id                    TEXT    NOT NULL PRIMARY KEY,
    name                  TEXT    NOT NULL,
    type                  INTEGER NOT NULL,  -- Checking|Credit
    is_archived           INTEGER NOT NULL,
    credit_sign_convention INTEGER NULL      -- PositiveIsCharge|PositiveIsPayment (credit only)
    import_profile_key     TEXT    NOT NULL   -- "amex.v1", "sofi.v1", etc
);

-- =========================
-- rules
-- =========================
    CREATE TABLE IF NOT EXISTS rules (
    id          TEXT    NOT NULL PRIMARY KEY,
    match_type  INTEGER NOT NULL, -- Contains (v1)
    match_text  TEXT    NOT NULL,
    category_id TEXT    NOT NULL,
    priority    INTEGER NOT NULL,
    enabled     INTEGER NOT NULL,
    
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- =========================
-- transactions
-- =========================
CREATE TABLE IF NOT EXISTS transactions (
    id                    TEXT    NOT NULL PRIMARY KEY,
    account_id             TEXT    NOT NULL,
    posted_date            TEXT    NOT NULL,  -- YYYY-MM-DD
    amount_cents           INTEGER NOT NULL,
    raw_description        TEXT    NOT NULL,
    normalized_description TEXT    NOT NULL,
    category_id            TEXT    NOT NULL,
    status                 INTEGER NOT NULL,  -- NeedsReview|Reviewed|Ignored
    is_transfer            INTEGER NOT NULL,
    notes                  TEXT    NULL,
    source_file_name       TEXT    NULL,
    import_timestamp       TEXT    NULL,
    fingerprint            TEXT    NOT NULL,
    
    FOREIGN KEY (account_id) REFERENCES accounts(id),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    
    UNIQUE (account_id, fingerprint)
);
-- =========================
-- categories
-- =========================
CREATE TABLE IF NOT EXISTS categories (
    id               TEXT    NOT NULL PRIMARY KEY,
    name             TEXT    NOT NULL,
    is_system        INTEGER NOT NULL,
    is_user_editable INTEGER NOT NULL
);

-- =========================
-- accounts
-- =========================
CREATE TABLE IF NOT EXISTS accounts (
    id                    TEXT    NOT NULL PRIMARY KEY,
    name                  TEXT    NOT NULL,
    type                  INTEGER NOT NULL,  -- Checking|Credit
    is_archived           INTEGER NOT NULL,
    credit_sign_convention INTEGER NULL      -- PositiveIsCharge|PositiveIsPayment (credit only)
);

-- =========================
-- rules
-- =========================
CREATE TABLE IF NOT EXISTS rules (
    id          TEXT    NOT NULL PRIMARY KEY,
    match_type  INTEGER NOT NULL, -- Contains (v1)
    match_text  TEXT    NOT NULL,
    category_id TEXT    NOT NULL,
    priority    INTEGER NOT NULL,
    enabled     INTEGER NOT NULL,
    
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

-- =========================
-- transactions
-- =========================
CREATE TABLE IF NOT EXISTS transactions (
    id                    TEXT    NOT NULL PRIMARY KEY,
    account_id             TEXT    NOT NULL,
    posted_date            TEXT    NOT NULL,  -- YYYY-MM-DD
    amount_cents           INTEGER NOT NULL,
    raw_description        TEXT    NOT NULL,
    normalized_description TEXT    NOT NULL,
    category_id            TEXT    NOT NULL,
    status                 INTEGER NOT NULL,  -- NeedsReview|Reviewed|Ignored
    is_transfer            INTEGER NOT NULL,
    notes                  TEXT    NULL,
    source_file_name       TEXT    NULL,
    import_timestamp       TEXT    NULL,
    fingerprint            TEXT    NOT NULL,
    
    FOREIGN KEY (account_id) REFERENCES accounts(id),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    
    UNIQUE (account_id, fingerprint)
);
