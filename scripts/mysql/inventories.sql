-- 库存记录：保存商品在指定仓库、库位及入库批次下的当前库存。
create table if not exists inventory_record
(
    id               char(36)     not null comment '库存记录标识',
    `key`            varchar(128) not null comment '库存记录业务键',
    product_id       char(26)     not null comment '商品标识',
    warehouse_id     char(26)     not null comment '仓库标识',
    location_id      char(26)     not null comment '库位标识',
    inbound_batch_no varchar(64)  not null comment '入库批次号',
    inbound_at       datetime(6)  not null comment '入库时间',
    initial_quantity integer      not null comment '初始库存数量',
    quantity         integer      not null comment '当前库存数量',
    enable           boolean      not null default true comment '是否启用',
    delete_by        char(26)     null comment '删除人标识',
    delete_at        datetime(6)  null comment '删除时间',
    created_by       char(26)     not null comment '创建人标识',
    created_at       datetime(6)  not null comment '创建时间',
    updated_by       char(26)     null comment '修改人标识',
    updated_at       datetime(6)  null comment '修改时间',
    org_id           char(26)     not null comment '组织标识',
    version          char(36)     not null comment '乐观锁版本 UUIDv7',
    primary key (id),
    constraint ck_inventory_record_inbound_batch_no check (trim(inbound_batch_no) <> ''),
    constraint ck_inventory_record_initial_quantity check (initial_quantity >= 0),
    constraint ck_inventory_record_quantity check (quantity >= 0),
    key ix_inventory_record_key (`key`)
) comment = '库存记录';

-- 库存变更请求：通过唯一请求号保证变更请求的幂等性。
create table if not exists inventory_change_request
(
    request_no char(26)    not null comment '具有唯一性的变更请求号 ULID',
    request_at datetime(6) not null comment '请求时间',
    primary key (request_no)
) comment = '库存变更请求';

-- 库存变更锁：按库存维度串行处理库存变更。
create table if not exists inventory_change_lock
(
    lock_key varchar(255) not null comment '库存维度锁键',
    primary key (lock_key)
) comment = '库存变更锁';

-- 库存变更记录：保存每次库存变更前后的数量及变更时间。
create table if not exists inventory_change_record
(
    change_id       char(26)    not null comment '库存变更标识',
    request_no      char(26)    not null comment '关联的变更请求号 ULID',
    inventory_id    char(36)    not null comment '关联的库存记录标识',
    change_type     smallint    not null comment '变更类型：1 表示增加，2 表示扣减',
    quantity        integer     not null comment '本次变更数量',
    before_quantity integer     not null comment '变更前库存数量',
    after_quantity  integer     not null comment '变更后库存数量',
    changed_at      datetime(6) not null comment '变更时间',
    primary key (change_id),
    constraint fk_inventory_change_record_request
        foreign key (request_no) references inventory_change_request (request_no),
    constraint fk_inventory_change_record_inventory
        foreign key (inventory_id) references inventory_record (id),
    constraint ck_inventory_change_record_change_type check (change_type in (1, 2)),
    constraint ck_inventory_change_record_quantity check (quantity > 0),
    constraint ck_inventory_change_record_before_quantity check (before_quantity >= 0),
    constraint ck_inventory_change_record_after_quantity check (after_quantity >= 0),
    key ix_inventory_change_record_request_no (request_no),
    key ix_inventory_change_record_inventory_changed_at (inventory_id, changed_at, change_id)
) comment = '库存变更记录';
