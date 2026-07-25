CREATE TABLE IF NOT EXISTS outbox_messages
(
    event_id        uuid                        PRIMARY KEY,
    event_name      varchar(100)                NOT NULL,
    aggregate_id    bigint                      NOT NULL,
    payload         text                        NOT NULL,
    occurred_at     timestamp without time zone NOT NULL,
    processed_at    timestamp without time zone NULL,
    last_attempt_at timestamp without time zone NULL,
    failure_count   integer                     NOT NULL DEFAULT 0,
    last_error      varchar(4000)               NULL
);

CREATE INDEX IF NOT EXISTS ix_outbox_messages_pending
    ON outbox_messages (occurred_at, event_id)
    WHERE processed_at IS NULL;
