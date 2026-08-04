-- 钱包记录：保存客户在指定币种下的当前余额及公共审计字段。
create table if not exists wallet_record
(
    id             char(36)       not null comment '钱包标识',
    customer_id    char(26)       not null comment '客户标识',
    currency       smallint       not null comment '钱包币种：1 CNY，2 USD，3 EUR，4 GBP，5 JPY，6 HKD',
    balance_amount decimal(18, 2) not null default 0 comment '当前余额',
    enable         boolean        not null default true comment '是否启用',
    delete_by      char(26)       null comment '删除人标识',
    delete_at      datetime(6)    null comment '删除时间',
    created_by     char(26)       not null comment '创建人标识',
    created_at     datetime(6)    not null comment '创建时间',
    updated_by     char(26)       null comment '修改人标识',
    updated_at     datetime(6)    null comment '修改时间',
    org_id         char(26)       not null comment '组织标识',
    version        char(36)       not null comment '乐观锁版本 UUIDv7',
    primary key (id),
    constraint uq_wallet_record_customer_currency unique (customer_id, currency),
    constraint ck_wallet_record_currency check (currency in (1, 2, 3, 4, 5, 6)),
    constraint ck_wallet_record_balance_amount check (balance_amount >= 0)
) comment = '钱包记录';

-- 钱包变更请求：通过唯一请求号保证变更请求的幂等性。
create table if not exists wallet_change_request
(
    request_no char(26)    not null comment '具有唯一性的变更请求号 ULID',
    request_at datetime(6) not null comment '请求时间',
    primary key (request_no)
) comment = '钱包变更请求';

-- 钱包变更锁：按客户维度串行处理钱包余额变更。
create table if not exists wallet_change_lock
(
    lock_key varchar(255) not null comment '客户维度锁键',
    primary key (lock_key)
) comment = '钱包变更锁';

-- 钱包变更记录：保存每次钱包变更前后的余额及变更时间。
create table if not exists wallet_change_record
(
    change_id             char(26)       not null comment '钱包变更标识',
    request_no            char(26)       not null comment '关联的变更请求号 ULID',
    wallet_id             char(36)       not null comment '关联的钱包标识',
    change_type           smallint       not null comment '变更类型：1 表示增加，2 表示减少',
    amount                decimal(18, 2) not null comment '本次变更金额',
    before_balance_amount decimal(18, 2) not null comment '变更前余额',
    after_balance_amount  decimal(18, 2) not null comment '变更后余额',
    changed_at            datetime(6)    not null comment '变更时间',
    primary key (change_id),
    constraint fk_wallet_change_record_request
        foreign key (request_no) references wallet_change_request (request_no),
    constraint fk_wallet_change_record_wallet
        foreign key (wallet_id) references wallet_record (id),
    constraint ck_wallet_change_record_change_type check (change_type in (1, 2)),
    constraint ck_wallet_change_record_amount check (amount > 0),
    constraint ck_wallet_change_record_before_balance check (before_balance_amount >= 0),
    constraint ck_wallet_change_record_after_balance check (after_balance_amount >= 0),
    key ix_wallet_change_record_request_no (request_no),
    key ix_wallet_change_record_wallet_changed_at (wallet_id, changed_at, change_id)
) comment = '钱包变更记录';
