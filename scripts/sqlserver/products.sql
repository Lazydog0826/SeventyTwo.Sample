-- 商品记录：保存商品基础信息及公共审计字段。
if object_id(N'dbo.product_record', N'U') is null
    begin
        create table dbo.product_record
        (
            id         uniqueidentifier  not null,
            name       nvarchar(255)     not null,
            price      decimal(18, 2)    not null,
            enable     bit               not null
                constraint df_product_record_enable default (1),
            delete_by  char(26)          null,
            delete_at  datetimeoffset(6) null,
            created_by char(26)          not null,
            created_at datetimeoffset(6) not null,
            updated_by char(26)          null,
            updated_at datetimeoffset(6) null,
            org_id     char(26)          not null,
            version    uniqueidentifier  not null,
            constraint pk_product_record primary key (id),
            constraint ck_product_record_name check (ltrim(rtrim(name)) <> N''),
            constraint ck_product_record_price check (price > 0)
        );

        execute sys.sp_addextendedproperty N'MS_Description', N'商品记录', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record';
        execute sys.sp_addextendedproperty N'MS_Description', N'商品标识', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'id';
        execute sys.sp_addextendedproperty N'MS_Description', N'商品名称', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'name';
        execute sys.sp_addextendedproperty N'MS_Description', N'商品价格', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'price';
        execute sys.sp_addextendedproperty N'MS_Description', N'是否启用', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'enable';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'delete_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'删除时间', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'delete_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'created_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'创建时间', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'created_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改人标识', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'updated_by';
        execute sys.sp_addextendedproperty N'MS_Description', N'修改时间', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'updated_at';
        execute sys.sp_addextendedproperty N'MS_Description', N'组织标识', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'org_id';
        execute sys.sp_addextendedproperty N'MS_Description', N'乐观锁版本 UUIDv7', N'SCHEMA', N'dbo', N'TABLE',
                N'product_record', N'COLUMN', N'version';
    end;
