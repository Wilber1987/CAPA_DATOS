using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CAPA_DATOS.Generator
{
    public class SQLDatabaseDescriptor
    {
        public SQLDatabaseDescriptor(GDatosAbstract Connection){
            this.Connection = Connection;
        }
        public GDatosAbstract Connection { get; private set; }

        public List<EntitySchema> databaseSchemas()
        {
            string DescribeQuery = @"SELECT TABLE_SCHEMA FROM [INFORMATION_SCHEMA].[TABLES]  group by TABLE_SCHEMA";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            var es = AdapterUtil.ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public List<EntitySchema> databaseTypes()
        {
            string DescribeQuery = @"SELECT TABLE_TYPE FROM [INFORMATION_SCHEMA].[TABLES]  group by TABLE_TYPE";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            var es = AdapterUtil.ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public List<EntitySchema> describeSchema(string schema, string type)
        {
            string DescribeQuery = @"SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE  FROM [INFORMATION_SCHEMA].[TABLES]  
                                    where TABLE_SCHEMA = '" + schema + "' and TABLE_TYPE = '" + type + "'";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            var es = AdapterUtil.ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public EntityColumn? describePrimaryKey(string table, string column)
        {
            string DescribeQuery = @"exec sp_columns'" + table + "'";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            var es = AdapterUtil.ConvertDataTable<EntityColumn>(Table, new EntityColumn());
            return es.Find(e => e.COLUMN_NAME == column && e.TYPE_NAME.Contains("identity"));
        }
        public List<EntityProps> describeEntity(string entityName)
        {
            string DescribeQuery = @"SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE  from [INFORMATION_SCHEMA].[COLUMNS] 
                                    WHERE [TABLE_NAME] = '" + entityName + "' order by [ORDINAL_POSITION]";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            return AdapterUtil.ConvertDataTable<EntityProps>(Table, new EntityProps());
        }

        public List<OneToOneSchema> ManyToOneKeys(string entityName)
        {
            string DescribeQuery = @"SELECT   
                    f.name AS foreign_key_name  
                   ,OBJECT_NAME(f.parent_object_id) AS TABLE_NAME  
                   ,COL_NAME(fc.parent_object_id, fc.parent_column_id) AS CONSTRAINT_COLUMN_NAME  
                   ,OBJECT_NAME (f.referenced_object_id) AS REFERENCE_TABLE_NAME  
                   ,COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS REFERENCE_COLUMN_NAME  
                   ,f.is_disabled, f.is_not_trusted
                   ,f.delete_referential_action_desc  
                   ,f.update_referential_action_desc  
                FROM sys.foreign_keys AS f  
                INNER JOIN sys.foreign_key_columns AS fc   
                   ON f.object_id = fc.constraint_object_id   
                WHERE f.parent_object_id = OBJECT_ID('" + entityName + "')";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            return AdapterUtil.ConvertDataTable<OneToOneSchema>(Table, new OneToOneSchema());
        }
        public Boolean isPrimary(string entityName, string column)
        {
            return evalKeyType(entityName, column, "PRIMARY KEY") > 0;
        }
        public Boolean isForeinKey(string entityName, string column)
        {
            return evalKeyType(entityName, column, "FOREIGN KEY") > 0;
        }
        public int evalKeyType(string entityName, string column, string keyType)
        {
            string DescribeQuery = @"SELECT
                    Col.Column_Name,  *
                from
                    INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab
                    join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col
                        on Col.Constraint_Name = Tab.Constraint_Name
                           and Col.Table_Name = Tab.Table_Name
                where
                    Constraint_Type = '" + keyType + @"'
                    and Tab.TABLE_NAME = '" + entityName + @"'
                    and Col.Column_Name = '" + column + "';";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            return Table.Rows.Count;
        }
        public int keyInformation(string entityName, string keyType)
        {
            string DescribeQuery = @"SELECT
                    Col.Column_Name,  *
                from
                    INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab
                    join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col
                        on Col.Constraint_Name = Tab.Constraint_Name
                           and Col.Table_Name = Tab.Table_Name
                where
                    Constraint_Type = '" + keyType + @"'
                    and Tab.TABLE_NAME = '" + entityName + "';";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            return Table.Rows.Count;
        }
        public List<OneToManySchema> oneToManyKeys(string entityName, string schema = "dbo")
        {
            string DescribeQuery = $"EXEC sp_fkeys @pktable_name = N'{entityName}' ,@pktable_owner = N'{schema}';";
            //string DescribeQuery = @"exec sp_fkeys '" + entityName + "'";
            DataTable Table = Connection.TraerDatosSQL(DescribeQuery, null);
            return AdapterUtil.ConvertDataTable<OneToManySchema>(Table, new OneToManySchema());
        }
    }
}