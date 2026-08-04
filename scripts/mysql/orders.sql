-- 订单主表：保存订单基础信息、处理状态、收货信息及公共审计字段。
create table if not exists orders
(
    id             char(36)     not null comment '订单标识',
    order_no       varchar(32)  not null comment '订单编号',
    customer_id    char(26)     not null comment '客户标识',
    warehouse_id   char(26)     not null comment '仓库标识',
    order_type     smallint     not null comment '订单类型：1 销售订单，2 退货订单，3 调拨订单',
    order_status   smallint     not null comment '订单状态：0 待处理，1 处理中，2 已处理，3 已取消',
    receiver_name  varchar(50)  null comment '收货人姓名',
    receiver_phone varchar(20)  null comment '收货人手机号',
    province       varchar(30)  null comment '收货地址所在省份',
    city           varchar(30)  null comment '收货地址所在城市',
    district       varchar(30)  null comment '收货地址所在区县',
    detail_address varchar(200) null comment '收货详细地址',
    remark         varchar(500) null comment '订单备注',
    enable         boolean      not null default true comment '是否启用',
    delete_by      char(26)     null comment '删除人标识',
    delete_at      datetime(6)  null comment '删除时间',
    created_by     char(26)     not null comment '创建人标识',
    created_at     datetime(6)  not null comment '创建时间',
    updated_by     char(26)     null comment '修改人标识',
    updated_at     datetime(6)  null comment '修改时间',
    org_id         char(26)     not null comment '组织标识',
    version        char(36)     not null comment '乐观锁版本 UUIDv7',
    primary key (id),
    constraint uq_orders_order_no unique (order_no),
    constraint ck_orders_order_no check (trim(order_no) <> ''),
    constraint ck_orders_order_type check (order_type in (1, 2, 3)),
    constraint ck_orders_order_status check (order_status in (0, 1, 2, 3)),
    key ix_orders_created_at_id (delete_at, created_at desc, id desc),
    key ix_orders_receiver_phone (delete_at, receiver_phone)
) comment = '订单';

-- 订单明细表：保存订单内的商品、数量、价格及履约数量。
create table if not exists order_items
(
    id                char(26)       not null comment '订单明细标识',
    order_id          char(36)       not null comment '所属订单标识',
    line_no           integer        not null comment '订单内明细行号',
    product_id        char(26)       not null comment '商品标识',
    product_name      varchar(255)   not null comment '下单时的商品名称快照',
    unit              varchar(20)    null comment '商品计量单位',
    quantity          integer        not null comment '购买数量',
    unit_price        decimal(18, 2) not null comment '下单时的商品单价',
    shipped_quantity  integer        not null default 0 comment '已发货数量',
    returned_quantity integer        not null default 0 comment '已退货数量',
    remark            varchar(300)   null comment '订单明细备注',
    primary key (id),
    constraint uq_order_items_order_line unique (order_id, line_no),
    constraint fk_order_items_order foreign key (order_id) references orders (id),
    constraint ck_order_items_line_no check (line_no > 0),
    constraint ck_order_items_product_name check (trim(product_name) <> ''),
    constraint ck_order_items_quantity check (quantity > 0),
    constraint ck_order_items_unit_price check (unit_price > 0),
    constraint ck_order_items_shipped_quantity check (shipped_quantity >= 0),
    constraint ck_order_items_returned_quantity check (returned_quantity >= 0),
    key ix_order_items_product_id (product_id)
) comment = '订单明细';
