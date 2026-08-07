-- 订单主表：保存订单基础信息、处理状态、收货信息及公共审计字段。
create table if not exists orders
(
    id             uuid primary key,
    order_no       varchar(32)              not null check (btrim(order_no) <> ''),
    customer_id    uuid                     not null,
    warehouse_id   uuid                     not null,
    order_type     smallint                 not null check (order_type in (1, 2, 3)),
    order_status   smallint                 not null check (order_status in (0, 1, 2, 3)),
    receiver_name  varchar(50)              null,
    receiver_phone varchar(20)              null,
    province       varchar(30)              null,
    city           varchar(30)              null,
    district       varchar(30)              null,
    detail_address varchar(200)             null,
    remark         varchar(500)             null,
    enable         boolean                  not null default true,
    delete_by      uuid                     null,
    delete_at      timestamp with time zone null,
    created_by     uuid                     not null,
    created_at     timestamp with time zone not null,
    updated_by     uuid                     null,
    updated_at     timestamp with time zone null,
    org_id         uuid                     not null,
    version        uuid                     not null,
    constraint uq_orders_order_no unique (order_no)
);

comment on table orders is '订单';
comment on column orders.id is '订单标识';
comment on column orders.order_no is '订单编号';
comment on column orders.customer_id is '客户标识';
comment on column orders.warehouse_id is '仓库标识';
comment on column orders.order_type is '订单类型：1 销售订单，2 退货订单，3 调拨订单';
comment on column orders.order_status is '订单状态：0 待处理，1 处理中，2 已处理，3 已取消';
comment on column orders.receiver_name is '收货人姓名';
comment on column orders.receiver_phone is '收货人手机号';
comment on column orders.province is '收货地址所在省份';
comment on column orders.city is '收货地址所在城市';
comment on column orders.district is '收货地址所在区县';
comment on column orders.detail_address is '收货详细地址';
comment on column orders.remark is '订单备注';
comment on column orders.enable is '是否启用';
comment on column orders.delete_by is '删除人标识';
comment on column orders.delete_at is '删除时间';
comment on column orders.created_by is '创建人标识';
comment on column orders.created_at is '创建时间';
comment on column orders.updated_by is '修改人标识';
comment on column orders.updated_at is '修改时间';
comment on column orders.org_id is '组织标识';
comment on column orders.version is '乐观锁版本 UUIDv7';

-- 支持按创建时间和订单标识倒序分页查询未删除订单。
create index if not exists ix_orders_created_at_id
    on orders (created_at desc, id desc)
    where delete_at is null;

-- 加速按收货人手机号前缀查询未删除订单。
create index if not exists ix_orders_receiver_phone
    on orders (receiver_phone varchar_pattern_ops)
    where delete_at is null;

-- 订单明细表：保存订单内的商品、数量、价格及履约数量。
create table if not exists order_items
(
    id                uuid primary key,
    order_id          uuid           not null,
    line_no           integer        not null check (line_no > 0),
    product_id        uuid           not null,
    product_name      varchar(255)   not null check (btrim(product_name) <> ''),
    unit              varchar(20)    null,
    quantity          integer        not null check (quantity > 0),
    unit_price        numeric(18, 2) not null check (unit_price > 0),
    shipped_quantity  integer        not null default 0 check (shipped_quantity >= 0),
    returned_quantity integer        not null default 0 check (returned_quantity >= 0),
    remark            varchar(300)   null,
    constraint uq_order_items_order_line unique (order_id, line_no),
    constraint fk_order_items_order foreign key (order_id) references orders (id)
);

comment on table order_items is '订单明细';
comment on column order_items.id is '订单明细标识';
comment on column order_items.order_id is '所属订单标识';
comment on column order_items.line_no is '订单内明细行号';
comment on column order_items.product_id is '商品标识';
comment on column order_items.product_name is '下单时的商品名称快照';
comment on column order_items.unit is '商品计量单位';
comment on column order_items.quantity is '购买数量';
comment on column order_items.unit_price is '下单时的商品单价';
comment on column order_items.shipped_quantity is '已发货数量';
comment on column order_items.returned_quantity is '已退货数量';
comment on column order_items.remark is '订单明细备注';

-- 加速通过商品查询关联订单明细。
create index if not exists ix_order_items_product_id
    on order_items (product_id);
