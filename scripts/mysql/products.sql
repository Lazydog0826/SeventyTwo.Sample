-- 商品记录：保存商品基础信息及公共审计字段。
create table if not exists product_record
(
    id         char(36)       not null comment '商品标识',
    name       varchar(255)   not null comment '商品名称',
    price      decimal(18, 2) not null comment '商品价格',
    enable     boolean        not null default true comment '是否启用',
    delete_by  char(26)       null comment '删除人标识',
    delete_at  datetime(6)    null comment '删除时间',
    created_by char(26)       not null comment '创建人标识',
    created_at datetime(6)    not null comment '创建时间',
    updated_by char(26)       null comment '修改人标识',
    updated_at datetime(6)    null comment '修改时间',
    org_id     char(26)       not null comment '组织标识',
    version    char(36)       not null comment '乐观锁版本 UUIDv7',
    primary key (id),
    constraint ck_product_record_name check (trim(name) <> ''),
    constraint ck_product_record_price check (price > 0)
) comment = '商品记录';
