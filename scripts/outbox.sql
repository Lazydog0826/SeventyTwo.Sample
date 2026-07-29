-- 事务发件箱：保存待投递的领域事件及其处理状态。
create table if not exists outbox_messages
(
    event_id        bigint primary key,
    event_name      varchar(100)             not null,
    aggregate_id    bigint                   not null,
    payload         text                     not null,
    occurred_at     timestamp with time zone not null,
    processed_at    timestamp with time zone null,
    last_attempt_at timestamp with time zone null,
    failure_count   integer                  not null default 0,
    last_error      varchar(4000)            null
);

comment on table outbox_messages is '事务发件箱消息';
comment on column outbox_messages.event_id is '事件雪花标识';
comment on column outbox_messages.event_name is '事件名称';
comment on column outbox_messages.aggregate_id is '聚合根标识';
comment on column outbox_messages.payload is '事件消息内容';
comment on column outbox_messages.occurred_at is '事件发生时间';
comment on column outbox_messages.processed_at is '处理完成时间，未处理时为空';
comment on column outbox_messages.last_attempt_at is '最近一次处理尝试时间';
comment on column outbox_messages.failure_count is '处理失败次数';
comment on column outbox_messages.last_error is '最近一次处理失败信息';

-- 加速查询尚未处理的发件箱消息，并保证稳定的处理顺序。
create index if not exists ix_outbox_messages_pending
    on outbox_messages (occurred_at, event_id)
    where processed_at is null;
