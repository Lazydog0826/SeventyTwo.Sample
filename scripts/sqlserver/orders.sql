-- 订单主表：保存订单基础信息、处理状态、收货信息及公共审计字段。
if object_id(N'dbo.orders', N'U') is null
    begin
        create table dbo.orders
        (
            id             uniqueidentifier  not null,
            order_no       varchar(32)       not null,
            customer_id    char(26)          not null,
            warehouse_id   char(26)          not null,
            order_type     smallint          not null,
            order_status   smallint          not null,
            receiver_name  nvarchar(50)      null,
            receiver_phone varchar(20)       null,
            province       nvarchar(30)      null,
            city           nvarchar(30)      null,
            district       nvarchar(30)      null,
            detail_address nvarchar(200)     null,
            remark         nvarchar(500)     null,
            enable         bit               not null
                constraint df_orders_enable default (1),
            delete_by      char(26)          null,
            delete_at      datetimeoffset(6) null,
            created_by     char(26)          not null,
            created_at     datetimeoffset(6) not null,
            updated_by     char(26)          null,
            updated_at     datetimeoffset(6) null,
            org_id         char(26)          not null,
            version        uniqueidentifier  not null,
            constraint pk_orders primary key (id),
            constraint uq_orders_order_no unique (order_no),
            constraint ck_orders_order_no check (ltrim(rtrim(order_no)) <> ''),
            constraint ck_orders_order_type check (order_type in (1, 2, 3)),
            constraint ck_orders_order_status check (order_status in (0, 1, 2, 3))
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'订单', N'SCHEMA', N'dbo', N'TABLE', N'orders';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'id';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单编号', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'order_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'客户标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'customer_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'仓库标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'warehouse_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单类型：1 销售订单，2 退货订单，3 调拨订单', N'SCHEMA',
                N'dbo', N'TABLE', N'orders', N'COLUMN', N'order_type';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单状态：0 待处理，1 处理中，2 已处理，3 已取消',
                N'SCHEMA', N'dbo', N'TABLE', N'orders', N'COLUMN', N'order_status';
        execute sys.sp_addextendedproperty N'MS_Description', N'收货人姓名', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'receiver_name';
        execute sys.sp_addextendedproperty N'MS_Description', N'收货人手机号', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'receiver_phone';
        execute sys.sp_addextendedproperty N'MS_Description', N'收货地址所在省份', N'SCHEMA', N'dbo', N'TABLE',
                N'orders', N'COLUMN', N'province';
        execute sys.sp_addextendedproperty N'MS_Description', N'收货地址所在城市', N'SCHEMA', N'dbo', N'TABLE',
                N'orders', N'COLUMN', N'city';
        execute sys.sp_addextendedproperty N'MS_Description', N'收货地址所在区县', N'SCHEMA', N'dbo', N'TABLE',
                N'orders', N'COLUMN', N'district';
        execute sys.sp_addextendedproperty N'MS_Description', N'收货详细地址', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'detail_address';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单备注', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'remark';
        execute sys.sp_addextendedproperty N'MS_Description', N'是否启用', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'enable';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除人标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'delete_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除时间', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'delete_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建人标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'created_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建时间', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'created_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改人标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'updated_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改时间', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'updated_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'组织标识', N'SCHEMA', N'dbo', N'TABLE', N'orders',
                N'COLUMN', N'org_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'乐观锁版本 UUIDv7', N'SCHEMA', N'dbo', N'TABLE',
                N'orders', N'COLUMN', N'version';
    end;

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.orders')
                 and name = N'ix_orders_created_at_id')
create index ix_orders_created_at_id on dbo.orders (created_at desc, id desc) where delete_at is null;

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.orders')
                 and name = N'ix_orders_receiver_phone')
create index ix_orders_receiver_phone on dbo.orders (receiver_phone) where delete_at is null;

-- 订单明细表：保存订单内的商品、数量、价格及履约数量。
if object_id(N'dbo.order_items', N'U') is null
    begin
        create table dbo.order_items
        (
            id                char(26)         not null,
            order_id          uniqueidentifier not null,
            line_no           int              not null,
            product_id        char(26)         not null,
            product_name      nvarchar(255)    not null,
            unit              nvarchar(20)     null,
            quantity          int              not null,
            unit_price        decimal(18, 2)   not null,
            shipped_quantity  int              not null
                constraint df_order_items_shipped_quantity default (0),
            returned_quantity int              not null
                constraint df_order_items_returned_quantity default (0),
            remark            nvarchar(300)    null,
            constraint pk_order_items primary key (id),
            constraint uq_order_items_order_line unique (order_id, line_no),
            constraint fk_order_items_order foreign key (order_id) references dbo.orders (id),
            constraint ck_order_items_line_no check (line_no > 0),
            constraint ck_order_items_product_name check (ltrim(rtrim(product_name)) <> N''),
            constraint ck_order_items_quantity check (quantity > 0),
            constraint ck_order_items_unit_price check (unit_price > 0),
            constraint ck_order_items_shipped_quantity check (shipped_quantity >= 0),
            constraint ck_order_items_returned_quantity check (returned_quantity >= 0)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'订单明细', N'SCHEMA', N'dbo', N'TABLE', N'order_items';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单明细标识', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'id';
        execute sys.sp_addextendedproperty N'MS_Description', N'所属订单标识', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'order_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单内明细行号', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'line_no';
        execute sys.sp_addextendedproperty N'MS_Description', N'商品标识', N'SCHEMA', N'dbo', N'TABLE', N'order_items',
                N'COLUMN', N'product_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'下单时的商品名称快照', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'product_name';
        execute sys.sp_addextendedproperty N'MS_Description', N'商品计量单位', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'unit';
        execute sys.sp_addextendedproperty N'MS_Description', N'购买数量', N'SCHEMA', N'dbo', N'TABLE', N'order_items',
                N'COLUMN', N'quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'下单时的商品单价', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'unit_price';
        execute sys.sp_addextendedproperty N'MS_Description', N'已发货数量', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'shipped_quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'已退货数量', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'returned_quantity';
        execute sys.sp_addextendedproperty N'MS_Description', N'订单明细备注', N'SCHEMA', N'dbo', N'TABLE',
                N'order_items', N'COLUMN', N'remark';
    end;

if not exists (select 1
               from sys.indexes
               where object_id = object_id(N'dbo.order_items')
                 and name = N'ix_order_items_product_id')
create index ix_order_items_product_id on dbo.order_items (product_id);
