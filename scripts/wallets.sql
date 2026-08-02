-- 钱包记录：保存客户在指定币种下的当前余额及公共审计字段。
create table if not exists wallet_record
(
    id             char(26) primary key,
    customer_id    char(26)                 not null,
    currency       smallint                 not null check (currency in (1, 2, 3, 4, 5, 6)),
    balance_amount numeric(18, 2)           not null default 0 check (balance_amount >= 0),
    enable         boolean                  not null default true,
    delete_by      char(26)                 null,
    delete_at      timestamp with time zone null,
    created_by     char(26)                 not null,
    created_at     timestamp with time zone not null,
    updated_by     char(26)                 null,
    updated_at     timestamp with time zone null,
    org_id         char(26)                 not null,
    version        char(26)                 not null,
    constraint uq_wallet_record_customer_currency unique (customer_id, currency)
);

comment on table wallet_record is '钱包记录';
comment on column wallet_record.id is '钱包标识';
comment on column wallet_record.customer_id is '客户标识';
comment on column wallet_record.currency is '钱包币种：1 CNY，2 USD，3 EUR，4 GBP，5 JPY，6 HKD';
comment on column wallet_record.balance_amount is '当前余额';
comment on column wallet_record.enable is '是否启用';
comment on column wallet_record.delete_by is '删除人标识';
comment on column wallet_record.delete_at is '删除时间';
comment on column wallet_record.created_by is '创建人标识';
comment on column wallet_record.created_at is '创建时间';
comment on column wallet_record.updated_by is '修改人标识';
comment on column wallet_record.updated_at is '修改时间';
comment on column wallet_record.org_id is '组织标识';
comment on column wallet_record.version is '乐观锁版本 ULID';

-- 钱包变更请求：通过唯一请求号保证变更请求的幂等性。
create table if not exists wallet_change_request
(
    request_no char(26) primary key,
    request_at timestamp with time zone not null
);

comment on table wallet_change_request is '钱包变更请求';
comment on column wallet_change_request.request_no is '具有唯一性的变更请求号 ULID';
comment on column wallet_change_request.request_at is '请求时间';

-- 钱包变更锁：按客户维度串行处理钱包余额变更。
create table if not exists wallet_change_lock
(
    lock_key varchar(255) primary key
);

comment on table wallet_change_lock is '钱包变更锁';
comment on column wallet_change_lock.lock_key is '客户维度锁键';

-- 钱包变更记录：保存每次钱包变更前后的余额及变更时间。
create table if not exists wallet_change_record
(
    change_id             char(26) primary key,
    request_no            char(26)                 not null,
    wallet_id             char(26)                 not null,
    change_type           smallint                 not null check (change_type in (1, 2)),
    amount                numeric(18, 2)           not null check (amount > 0),
    before_balance_amount numeric(18, 2)           not null check (before_balance_amount >= 0),
    after_balance_amount  numeric(18, 2)           not null check (after_balance_amount >= 0),
    changed_at            timestamp with time zone not null,
    constraint fk_wallet_change_record_request
        foreign key (request_no) references wallet_change_request (request_no),
    constraint fk_wallet_change_record_wallet
        foreign key (wallet_id) references wallet_record (id)
);

comment on table wallet_change_record is '钱包变更记录';
comment on column wallet_change_record.change_id is '钱包变更标识';
comment on column wallet_change_record.request_no is '关联的变更请求号 ULID';
comment on column wallet_change_record.wallet_id is '关联的钱包标识';
comment on column wallet_change_record.change_type is '变更类型：1 表示增加，2 表示减少';
comment on column wallet_change_record.amount is '本次变更金额';
comment on column wallet_change_record.before_balance_amount is '变更前余额';
comment on column wallet_change_record.after_balance_amount is '变更后余额';
comment on column wallet_change_record.changed_at is '变更时间';

-- 加速通过请求号查询钱包变更记录。
create index if not exists ix_wallet_change_record_request_no
    on wallet_change_record (request_no);

-- 支持按钱包及变更时间顺序查询变更历史。
create index if not exists ix_wallet_change_record_wallet_changed_at
    on wallet_change_record (wallet_id, changed_at, change_id);
