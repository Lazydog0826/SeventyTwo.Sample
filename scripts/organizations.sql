-- 机构：保存机构层级、基础信息及公共审计字段。
create table if not exists organization
(
    id         uuid primary key,
    code       varchar(255)             not null check (btrim(code) <> ''),
    name       varchar(255)             not null check (btrim(name) <> ''),
    parent_id  uuid                     null,
    enable     boolean                  not null default true,
    delete_by  uuid                     null,
    delete_at  timestamp with time zone null,
    created_by uuid                     not null,
    created_at timestamp with time zone not null,
    updated_by uuid                     null,
    updated_at timestamp with time zone null,
    org_id     uuid                     not null,
    version    uuid                     not null,
    constraint uq_organization_org_code unique (org_id, code),
    constraint ck_organization_parent check (parent_id is null or parent_id <> id),
    constraint fk_organization_parent foreign key (parent_id) references organization (id)
);

comment on table organization is '机构';
comment on column organization.id is '机构标识';
comment on column organization.code is '机构编码';
comment on column organization.name is '机构名称';
comment on column organization.parent_id is '上级机构标识';
comment on column organization.enable is '是否启用';
comment on column organization.delete_by is '删除人标识';
comment on column organization.delete_at is '删除时间';
comment on column organization.created_by is '创建人标识';
comment on column organization.created_at is '创建时间';
comment on column organization.updated_by is '修改人标识';
comment on column organization.updated_at is '修改时间';
comment on column organization.org_id is '组织标识';
comment on column organization.version is '乐观锁版本 UUIDv7';

-- 加速按上级机构查询未删除的下级机构。
create index if not exists ix_organization_parent_id
    on organization (parent_id)
    where delete_at is null;

-- 机构成员：保存用户与机构的归属关系及公共审计字段。
create table if not exists organization_member
(
    id              uuid primary key,
    organization_id uuid                     not null,
    user_id         uuid                     not null,
    is_primary      boolean                  not null default false,
    enable          boolean                  not null default true,
    delete_by       uuid                     null,
    delete_at       timestamp with time zone null,
    created_by      uuid                     not null,
    created_at      timestamp with time zone not null,
    updated_by      uuid                     null,
    updated_at      timestamp with time zone null,
    org_id          uuid                     not null,
    version         uuid                     not null,
    constraint uq_organization_member_organization_user unique (organization_id, user_id),
    constraint fk_organization_member_organization
        foreign key (organization_id) references organization (id)
);

comment on table organization_member is '机构成员';
comment on column organization_member.id is '机构成员标识';
comment on column organization_member.organization_id is '所属机构标识';
comment on column organization_member.user_id is '用户标识';
comment on column organization_member.is_primary is '是否为用户的主机构';
comment on column organization_member.enable is '是否启用';
comment on column organization_member.delete_by is '删除人标识';
comment on column organization_member.delete_at is '删除时间';
comment on column organization_member.created_by is '创建人标识';
comment on column organization_member.created_at is '创建时间';
comment on column organization_member.updated_by is '修改人标识';
comment on column organization_member.updated_at is '修改时间';
comment on column organization_member.org_id is '组织标识';
comment on column organization_member.version is '乐观锁版本 UUIDv7';

-- 加速按用户查询其所属机构。
create index if not exists ix_organization_member_user_id
    on organization_member (user_id)
    where delete_at is null;


