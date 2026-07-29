-- 商品记录：保存商品基础信息及公共审计字段。
create table if not exists product_record
(
    id         bigint primary key,
    name       varchar(255)             not null check (btrim(name) <> ''),
    price      numeric(18, 2)           not null check (price > 0),
    enable     boolean                  not null default true,
    delete_by  bigint                   null,
    delete_at  timestamp with time zone null,
    created_by bigint                   not null,
    created_at timestamp with time zone not null,
    updated_by bigint                   null,
    updated_at timestamp with time zone null,
    org_id     bigint                   not null,
    version    bigint                   not null default 0
);

comment on table product_record is '商品记录';
comment on column product_record.id is '商品标识';
comment on column product_record.name is '商品名称';
comment on column product_record.price is '商品价格';
comment on column product_record.enable is '是否启用';
comment on column product_record.delete_by is '删除人标识';
comment on column product_record.delete_at is '删除时间';
comment on column product_record.created_by is '创建人标识';
comment on column product_record.created_at is '创建时间';
comment on column product_record.updated_by is '修改人标识';
comment on column product_record.updated_at is '修改时间';
comment on column product_record.org_id is '组织标识';
comment on column product_record.version is '乐观锁版本号';
