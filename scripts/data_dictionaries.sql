-- 数据字典：保存字典定义及公共审计字段。
create table if not exists data_dictionary
(
    id          uuid primary key,
    code        varchar(255)             not null check (btrim(code) <> ''),
    name        varchar(255)             not null check (btrim(name) <> ''),
    description varchar(255)             null,
    enable      boolean                  not null default true,
    delete_by   uuid                     null,
    delete_at   timestamp with time zone null,
    created_by  uuid                     not null,
    created_at  timestamp with time zone not null,
    updated_by  uuid                     null,
    updated_at  timestamp with time zone null,
    org_id      uuid                     not null,
    version     uuid                     not null,
    constraint uq_data_dictionary_org_code unique (org_id, code)
);

comment on table data_dictionary is '数据字典';
comment on column data_dictionary.id is '数据字典标识';
comment on column data_dictionary.code is '数据字典编码';
comment on column data_dictionary.name is '数据字典名称';
comment on column data_dictionary.description is '数据字典说明';
comment on column data_dictionary.enable is '是否启用';
comment on column data_dictionary.delete_by is '删除人标识';
comment on column data_dictionary.delete_at is '删除时间';
comment on column data_dictionary.created_by is '创建人标识';
comment on column data_dictionary.created_at is '创建时间';
comment on column data_dictionary.updated_by is '修改人标识';
comment on column data_dictionary.updated_at is '修改时间';
comment on column data_dictionary.org_id is '组织标识';
comment on column data_dictionary.version is '乐观锁版本 UUIDv7';

-- 数据字典项：保存字典下的值、显示文本和排序号。
create table if not exists data_dictionary_item
(
    id            uuid primary key,
    dictionary_id uuid         not null,
    value         varchar(255) not null check (btrim(value) <> ''),
    label         varchar(255) not null check (btrim(label) <> ''),
    sort_order    integer      not null check (sort_order >= 0),
    constraint uq_data_dictionary_item_dictionary_value unique (dictionary_id, value),
    constraint fk_data_dictionary_item_dictionary
        foreign key (dictionary_id) references data_dictionary (id)
);

comment on table data_dictionary_item is '数据字典项';
comment on column data_dictionary_item.id is '数据字典项标识';
comment on column data_dictionary_item.dictionary_id is '所属数据字典标识';
comment on column data_dictionary_item.value is '数据字典项值';
comment on column data_dictionary_item.label is '数据字典项文本';
comment on column data_dictionary_item.sort_order is '数据字典项排序号';

-- 支持按字典和排序号查询字典项。
create index if not exists ix_data_dictionary_item_dictionary_sort_order
    on data_dictionary_item (dictionary_id, sort_order, id);
