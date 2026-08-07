-- 库存记录：保存商品在指定仓库、库位及入库批次下的当前库存。
create table if not exists inventory_record
(
    id               uuid primary key,
    key              varchar(128)             not null,
    product_id       uuid                     not null,
    warehouse_id     uuid                     not null,
    location_id      uuid                     not null,
    inbound_batch_no varchar(64)              not null check (btrim(inbound_batch_no) <> ''),
    inbound_at       timestamp with time zone not null,
    initial_quantity integer                  not null check (initial_quantity >= 0),
    quantity         integer                  not null check (quantity >= 0),
    enable           boolean                  not null default true,
    delete_by        uuid                     null,
    delete_at        timestamp with time zone null,
    created_by       uuid                     not null,
    created_at       timestamp with time zone not null,
    updated_by       uuid                     null,
    updated_at       timestamp with time zone null,
    org_id           uuid                     not null,
    version          uuid                     not null
);

comment on table inventory_record is '库存记录';
comment on column inventory_record.id is '库存记录标识';
comment on column inventory_record.key is '库存记录业务键';
comment on column inventory_record.product_id is '商品标识';
comment on column inventory_record.warehouse_id is '仓库标识';
comment on column inventory_record.location_id is '库位标识';
comment on column inventory_record.inbound_batch_no is '入库批次号';
comment on column inventory_record.inbound_at is '入库时间';
comment on column inventory_record.initial_quantity is '初始库存数量';
comment on column inventory_record.quantity is '当前库存数量';
comment on column inventory_record.enable is '是否启用';
comment on column inventory_record.delete_by is '删除人标识';
comment on column inventory_record.delete_at is '删除时间';
comment on column inventory_record.created_by is '创建人标识';
comment on column inventory_record.created_at is '创建时间';
comment on column inventory_record.updated_by is '修改人标识';
comment on column inventory_record.updated_at is '修改时间';
comment on column inventory_record.org_id is '组织标识';
comment on column inventory_record.version is '乐观锁版本 UUIDv7';

-- 加速通过业务键查询库存记录。
create index if not exists ix_inventory_record_key
    on inventory_record (key);

-- 库存变更请求：通过唯一请求号保证变更请求的幂等性。
create table if not exists inventory_change_request
(
    request_no uuid primary key,
    request_at timestamp with time zone not null
);

comment on table inventory_change_request is '库存变更请求';
comment on column inventory_change_request.request_no is '具有唯一性的变更请求号 UUIDv7';
comment on column inventory_change_request.request_at is '请求时间';

-- 库存变更锁：按库存维度串行处理库存变更。
create table if not exists inventory_change_lock
(
    lock_key varchar(255) primary key
);

comment on table inventory_change_lock is '库存变更锁';
comment on column inventory_change_lock.lock_key is '库存维度锁键';

-- 库存变更记录：保存每次库存变更前后的数量及变更时间。
create table if not exists inventory_change_record
(
    change_id       uuid primary key,
    request_no      uuid                     not null,
    inventory_id    uuid                     not null,
    change_type     smallint                 not null check (change_type in (1, 2)),
    quantity        integer                  not null check (quantity > 0),
    before_quantity integer                  not null check (before_quantity >= 0),
    after_quantity  integer                  not null check (after_quantity >= 0),
    changed_at      timestamp with time zone not null,
    constraint fk_inventory_change_record_request
        foreign key (request_no) references inventory_change_request (request_no),
    constraint fk_inventory_change_record_inventory
        foreign key (inventory_id) references inventory_record (id)
);

comment on table inventory_change_record is '库存变更记录';
comment on column inventory_change_record.change_id is '库存变更标识';
comment on column inventory_change_record.request_no is '关联的变更请求号 UUIDv7';
comment on column inventory_change_record.inventory_id is '关联的库存记录标识';
comment on column inventory_change_record.change_type is '变更类型：1 表示增加，2 表示扣减';
comment on column inventory_change_record.quantity is '本次变更数量';
comment on column inventory_change_record.before_quantity is '变更前库存数量';
comment on column inventory_change_record.after_quantity is '变更后库存数量';
comment on column inventory_change_record.changed_at is '变更时间';

-- 加速通过请求号查询库存变更记录。
create index if not exists ix_inventory_change_record_request_no
    on inventory_change_record (request_no);

-- 支持按库存记录及变更时间顺序查询变更历史。
create index if not exists ix_inventory_change_record_inventory_changed_at
    on inventory_change_record (inventory_id, changed_at, change_id);
