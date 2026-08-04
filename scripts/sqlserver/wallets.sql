-- 钱包记录：保存客户在指定币种下的当前余额及公共审计字段。
if object_id(N'dbo.wallet_record', N'U') is null
    begin
        create table dbo.wallet_record
        (
            id             uniqueidentifier  not null,
            customer_id    char(26)          not null,
            currency       smallint          not null,
            balance_amount decimal(18, 2)    not null
                constraint df_wallet_record_balance_amount default (0),
            enable         bit               not null
                constraint df_wallet_record_enable default (1),
            delete_by      char(26)          null,
            delete_at      datetimeoffset(6) null,
            created_by     char(26)          not null,
            created_at     datetimeoffset(6) not null,
            updated_by     char(26)          null,
            updated_at     datetimeoffset(6) null,
            org_id         char(26)          not null,
            version        uniqueidentifier  not null,
            constraint pk_wallet_record primary key (id),
            constraint uq_wallet_record_customer_currency unique (customer_id, currency),
            constraint ck_wallet_record_currency check (currency in (1, 2, 3, 4, 5, 6)),
            constraint ck_wallet_record_balance_amount check (balance_amount >= 0)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'钱包记录', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record';
        execute sys.sp_addextendedproperty N'MS_Description', N'钱包标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'id';
        execute sys.sp_addextendedproperty N'MS_Description', N'客户标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'customer_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'钱包币种：1 CNY，2 USD，3 EUR，4 GBP，5 JPY，6 HKD',
                N'SCHEMA', N'dbo', N'TABLE', N'wallet_record', N'COLUMN', N'currency';
        execute sys.sp_addextendedproperty N'MS_Description', N'当前余额', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'balance_amount';
        execute sys.sp_addextendedproperty N'MS_Description', N'是否启用', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'enable';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'delete_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除时间', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'delete_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'created_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建时间', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'created_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'updated_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改时间', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'updated_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'组织标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'org_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'乐观锁版本 UUIDv7', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_record', N'COLUMN', N'version';
    end;

-- 钱包变更请求：通过唯一请求号保证变更请求的幂等性。
if object_id(N'dbo.wallet_change_request', N'U') is null
    begin
        create table dbo.wallet_change_request
        (
            request_no char(26)          not null,
            request_at datetimeoffset(6) not null,
            constraint pk_wallet_change_request primary key (request_no)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'钱包变更请求', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_request';
        execute sys.sp_addextendedproperty N'MS_Description', N'具有唯一性的变更请求号 ULID', N'SCHEMA', N'dbo',
                N'TABLE', N'wallet_change_request', N'COLUMN', N'request_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'请求时间', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_request', N'COLUMN', N'request_at';
    end;

-- 钱包变更锁：按客户维度串行处理钱包余额变更。
if object_id(N'dbo.wallet_change_lock', N'U') is null
    begin
        create table dbo.wallet_change_lock
        (
            lock_key varchar(255) not null,
            constraint pk_wallet_change_lock primary key (lock_key)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'钱包变更锁', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_lock';
        execute sys.sp_addextendedproperty N'MS_Description', N'客户维度锁键', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_lock', N'COLUMN', N'lock_key';
    end;

-- 钱包变更记录：保存每次钱包变更前后的余额及变更时间。
if object_id(N'dbo.wallet_change_record', N'U') is null
    begin
        create table dbo.wallet_change_record
        (
            change_id             char(26)          not null,
            request_no            char(26)          not null,
            wallet_id             uniqueidentifier  not null,
            change_type           smallint          not null,
            amount                decimal(18, 2)    not null,
            before_balance_amount decimal(18, 2)    not null,
            after_balance_amount  decimal(18, 2)    not null,
            changed_at            datetimeoffset(6) not null,
            constraint pk_wallet_change_record primary key (change_id),
            constraint fk_wallet_change_record_request foreign key (request_no) references dbo.wallet_change_request (request_no),
            constraint fk_wallet_change_record_wallet foreign key (wallet_id) references dbo.wallet_record (id),
            constraint ck_wallet_change_record_change_type check (change_type in (1, 2)),
            constraint ck_wallet_change_record_amount check (amount > 0),
            constraint ck_wallet_change_record_before_balance check (before_balance_amount >= 0),
            constraint ck_wallet_change_record_after_balance check (after_balance_amount >= 0)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'钱包变更记录', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record';
        execute sys.sp_addextendedproperty N'MS_Description', N'钱包变更标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'change_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'关联的变更请求号 ULID', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'request_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'关联的钱包标识', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'wallet_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更类型：1 表示增加，2 表示减少', N'SCHEMA', N'dbo',
                N'TABLE', N'wallet_change_record', N'COLUMN', N'change_type';
        execute sys.sp_addextendedproperty N'MS_Description', N'本次变更金额', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'amount';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更前余额', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'before_balance_amount';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更后余额', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'after_balance_amount';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更时间', N'SCHEMA', N'dbo', N'TABLE',
                N'wallet_change_record', N'COLUMN', N'changed_at';
    end;

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.wallet_change_record')
                 and name = N'ix_wallet_change_record_request_no')
create index ix_wallet_change_record_request_no on dbo.wallet_change_record (request_no);

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.wallet_change_record')
                 and name = N'ix_wallet_change_record_wallet_changed_at')
create index ix_wallet_change_record_wallet_changed_at on dbo.wallet_change_record (wallet_id, changed_at, change_id);
