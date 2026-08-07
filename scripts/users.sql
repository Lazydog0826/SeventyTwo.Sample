-- 用户账户：保存用户身份信息、登录凭证及公共审计字段。
create table if not exists user_account
(
    id            uuid primary key,
    username      varchar(255)             not null check (btrim(username) <> ''),
    password_hash varchar(255)             not null check (btrim(password_hash) <> ''),
    display_name  varchar(255)             not null check (btrim(display_name) <> ''),
    phone         varchar(255)             null,
    email         varchar(255)             null,
    enable        boolean                  not null default true,
    delete_by     uuid                     null,
    delete_at     timestamp with time zone null,
    created_by    uuid                     not null,
    created_at    timestamp with time zone not null,
    updated_by    uuid                     null,
    updated_at    timestamp with time zone null,
    org_id        uuid                     not null,
    version       uuid                     not null,
    constraint uq_user_account_username unique (username)
);

comment on table user_account is '用户账户';
comment on column user_account.id is '用户标识';
comment on column user_account.username is '用户名';
comment on column user_account.password_hash is '密码摘要';
comment on column user_account.display_name is '用户姓名';
comment on column user_account.phone is '手机号';
comment on column user_account.email is '电子邮箱';
comment on column user_account.enable is '是否启用';
comment on column user_account.delete_by is '删除人标识';
comment on column user_account.delete_at is '删除时间';
comment on column user_account.created_by is '创建人标识';
comment on column user_account.created_at is '创建时间';
comment on column user_account.updated_by is '修改人标识';
comment on column user_account.updated_at is '修改时间';
comment on column user_account.org_id is '组织标识';
comment on column user_account.version is '乐观锁版本 UUIDv7';

-- 支持通过用户名和密码摘要查询用户账户。
create index if not exists ix_user_account_username_password_hash
    on user_account (username, password_hash);
