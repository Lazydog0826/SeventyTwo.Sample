CREATE TABLE IF NOT EXISTS inventory_record
(
    id                  bigint                      PRIMARY KEY,
    key                 varchar(128)                NOT NULL,
    product_id          bigint                      NOT NULL,
    warehouse_id        bigint                      NOT NULL,
    location_id         bigint                      NOT NULL,
    inbound_batch_no    varchar(64)                 NOT NULL,
    inbound_at          timestamp with time zone    NOT NULL,
    initial_quantity    integer                     NOT NULL CHECK (initial_quantity >= 0),
    quantity            integer                     NOT NULL CHECK (quantity >= 0),
    enable              boolean                     NOT NULL DEFAULT true,
    deleter             bigint                      NULL,
    delete_date         timestamp without time zone NULL,
    creator             bigint                      NOT NULL,
    create_date         timestamp without time zone NOT NULL,
    modifier            bigint                      NULL,
    modify_date         timestamp without time zone NULL,
    org_id              bigint                      NOT NULL,
    version             bigint                      NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_inventory_record_key
    ON inventory_record (key);

CREATE UNIQUE INDEX IF NOT EXISTS uq_inventory_record_product_warehouse_location_batch
    ON inventory_record (product_id, warehouse_id, location_id, inbound_batch_no);

CREATE TABLE IF NOT EXISTS inventory_change_request
(
    request_id          bigint                      PRIMARY KEY,
    request_no          varchar(64)                 NOT NULL,
    request_at          timestamp with time zone    NOT NULL,
    CONSTRAINT uq_inventory_change_request_request_no UNIQUE (request_no)
);

CREATE TABLE IF NOT EXISTS inventory_change_record
(
    change_id           bigint                      PRIMARY KEY,
    request_no          varchar(64)                 NOT NULL,
    inventory_id        bigint                      NOT NULL,
    change_type         smallint                    NOT NULL CHECK (change_type IN (1, 2)),
    quantity            integer                     NOT NULL CHECK (quantity > 0),
    before_quantity     integer                     NOT NULL CHECK (before_quantity >= 0),
    after_quantity      integer                     NOT NULL CHECK (after_quantity >= 0),
    changed_at          timestamp with time zone    NOT NULL,
    CONSTRAINT fk_inventory_change_record_request
        FOREIGN KEY (request_no) REFERENCES inventory_change_request (request_no),
    CONSTRAINT fk_inventory_change_record_inventory
        FOREIGN KEY (inventory_id) REFERENCES inventory_record (id)
        DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX IF NOT EXISTS ix_inventory_change_record_request_no
    ON inventory_change_record (request_no);

CREATE INDEX IF NOT EXISTS ix_inventory_change_record_inventory_changed_at
    ON inventory_change_record (inventory_id, changed_at, change_id);

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
