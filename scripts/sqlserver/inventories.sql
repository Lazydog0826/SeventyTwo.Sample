-- 库存记录：保存商品在指定仓库、库位及入库批次下的当前库存。
if object_id(N'dbo.inventory_record', N'U') is null
    begin
        create table dbo.inventory_record
        (
            id               uniqueidentifier  not null,
            [key]            varchar(128)      not null,
            product_id       char(26)          not null,
            warehouse_id     char(26)          not null,
            location_id      char(26)          not null,
            inbound_batch_no varchar(64)       not null,
            inbound_at       datetimeoffset(6) not null,
            initial_quantity int               not null,
            quantity         int               not null,
            enable           bit               not null
                constraint df_inventory_record_enable default (1),
            delete_by        char(26)          null,
            delete_at        datetimeoffset(6) null,
            created_by       char(26)          not null,
            created_at       datetimeoffset(6) not null,
            updated_by       char(26)          null,
            updated_at       datetimeoffset(6) null,
            org_id           char(26)          not null,
            version          uniqueidentifier  not null,
            constraint pk_inventory_record primary key (id),
            constraint ck_inventory_record_inbound_batch_no check (ltrim(rtrim(inbound_batch_no)) <> ''),
            constraint ck_inventory_record_initial_quantity check (initial_quantity >= 0),
            constraint ck_inventory_record_quantity check (quantity >= 0)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'库存记录', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record';
        execute sys.sp_addextendedproperty N'MS_Description', N'库存记录标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'id';
        execute sys.sp_addextendedproperty N'MS_Description', N'库存记录业务键', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'key';
        execute sys.sp_addextendedproperty N'MS_Description', N'商品标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'product_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'仓库标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'warehouse_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'库位标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'location_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'入库批次号', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'inbound_batch_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'入库时间', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'inbound_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'初始库存数量', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'initial_quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'当前库存数量', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'是否启用', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'enable';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'delete_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除时间', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'delete_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'created_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建时间', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'created_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'updated_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改时间', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'updated_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'组织标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'org_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'乐观锁版本 UUIDv7', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_record', N'COLUMN', N'version';
    end;

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.inventory_record')
                 and name = N'ix_inventory_record_key')
create index ix_inventory_record_key on dbo.inventory_record ([key]);

-- 库存变更请求：通过唯一请求号保证变更请求的幂等性。
if object_id(N'dbo.inventory_change_request', N'U') is null
    begin
        create table dbo.inventory_change_request
        (
            request_no char(26)          not null,
            request_at datetimeoffset(6) not null,
            constraint pk_inventory_change_request primary key (request_no)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'库存变更请求', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_request';
        execute sys.sp_addextendedproperty N'MS_Description', N'具有唯一性的变更请求号 ULID', N'SCHEMA', N'dbo',
                N'TABLE', N'inventory_change_request', N'COLUMN', N'request_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'请求时间', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_request', N'COLUMN', N'request_at';
    end;

-- 库存变更锁：按库存维度串行处理库存变更。
if object_id(N'dbo.inventory_change_lock', N'U') is null
    begin
        create table dbo.inventory_change_lock
        (
            lock_key varchar(255) not null,
            constraint pk_inventory_change_lock primary key (lock_key)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'库存变更锁', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_lock';
        execute sys.sp_addextendedproperty N'MS_Description', N'库存维度锁键', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_lock', N'COLUMN', N'lock_key';
    end;

-- 库存变更记录：保存每次库存变更前后的数量及变更时间。
if object_id(N'dbo.inventory_change_record', N'U') is null
    begin
        create table dbo.inventory_change_record
        (
            change_id       char(26)          not null,
            request_no      char(26)          not null,
            inventory_id    uniqueidentifier  not null,
            change_type     smallint          not null,
            quantity        int               not null,
            before_quantity int               not null,
            after_quantity  int               not null,
            changed_at      datetimeoffset(6) not null,
            constraint pk_inventory_change_record primary key (change_id),
            constraint fk_inventory_change_record_request foreign key (request_no) references dbo.inventory_change_request (request_no),
            constraint fk_inventory_change_record_inventory foreign key (inventory_id) references dbo.inventory_record (id),
            constraint ck_inventory_change_record_change_type check (change_type in (1, 2)),
            constraint ck_inventory_change_record_quantity check (quantity > 0),
            constraint ck_inventory_change_record_before_quantity check (before_quantity >= 0),
            constraint ck_inventory_change_record_after_quantity check (after_quantity >= 0)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'库存变更记录', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record';
        execute sys.sp_addextendedproperty N'MS_Description', N'库存变更标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'change_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'关联的变更请求号 ULID', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'request_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'关联的库存记录标识', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'inventory_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更类型：1 表示增加，2 表示扣减', N'SCHEMA', N'dbo',
                N'TABLE', N'inventory_change_record', N'COLUMN', N'change_type';
        execute sys.sp_addextendedproperty N'MS_Description', N'本次变更数量', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更前库存数量', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'before_quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更后库存数量', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'after_quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'变更时间', N'SCHEMA', N'dbo', N'TABLE',
                N'inventory_change_record', N'COLUMN', N'changed_at';
    end;

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.inventory_change_record')
                 and name = N'ix_inventory_change_record_request_no')
create index ix_inventory_change_record_request_no on dbo.inventory_change_record (request_no);

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.inventory_change_record')
                 and name = N'ix_inventory_change_record_inventory_changed_at')
create index ix_inventory_change_record_inventory_changed_at on dbo.inventory_change_record (inventory_id, changed_at, change_id);
