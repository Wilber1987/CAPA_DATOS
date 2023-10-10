using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAPA_DATOS
{
    public class SqlServerGDatos : GDatosAbstract
    {
        public SqlServerGDatos(string ConexionString)
        {
            this.ConexionString = ConexionString;
        }

        protected override IDbConnection SQLMCon()
        {
            if (this.MTConnection != null)
            {
                return this.MTConnection;
            }
            return CrearConexion(ConexionString);
        }
        protected override IDbConnection CrearConexion(string ConexionString)
        {
            return new SqlConnection(ConexionString);
        }
        protected override IDbCommand ComandoSql(string comandoSql, IDbConnection Conexion)
        {
            var com = new SqlCommand(comandoSql, (SqlConnection)Conexion);
            return com;
        }
        protected override IDataAdapter CrearDataAdapterSql(string comandoSql, IDbConnection Conexion)
        {
            var da = new SqlDataAdapter((SqlCommand)ComandoSql(comandoSql, Conexion));
            return da;
        }
        protected override IDataAdapter CrearDataAdapterSql(IDbCommand comandoSql)
        {
            var da = new SqlDataAdapter((SqlCommand)comandoSql);
            return da;
        }
        protected override List<EntityProps> DescribeEntity(string entityName)
        {
            string DescribeQuery = @"SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE, TABLE_SCHEMA
                                    from [INFORMATION_SCHEMA].[COLUMNS] 
                                    WHERE [TABLE_NAME] = '" + entityName
                                   + "' order by [ORDINAL_POSITION]";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            List<EntityProps> entityProps = ConvertDataTable<EntityProps>(Table, new EntityProps());
            if (entityProps.Count == 0)
            {
                throw new Exception("La entidad buscada no existe: " + entityName);
            }
            return entityProps;
        }
        protected override string BuildInsertQueryByObject(object Inst)
        {
            string ColumnNames = "";
            string Values = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = DescribeEntity(Inst.GetType().Name);

            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);
                if (AtributeValue != null && EntityProp != null)
                {
                    switch (EntityProp.DATA_TYPE)
                    {
                        case "nvarchar":
                        case "varchar":
                        case "char":
                            ColumnNames = ColumnNames + AtributeName.ToString() + ",";
                            JsonProp? json = (JsonProp?)Attribute.GetCustomAttribute(oProperty, typeof(JsonProp));
                            if (json != null)
                            {
                                String jsonV = JsonConvert.SerializeObject(AtributeValue);
                                Values = Values + "'" + JValue.Parse(jsonV).ToString(Formatting.Indented) + "',";
                            }
                            else
                            {
                                Values = Values + "'" + AtributeValue.ToString() + "',";
                            }
                            break;
                        case "int":
                        case "float":
                            ColumnNames = ColumnNames + AtributeName.ToString() + ",";
                            Values = Values + "cast ('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as float),";
                            break;
                        case "decimal":
                            ColumnNames = ColumnNames + AtributeName.ToString() + ",";
                            Values = Values + "cast ('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as decimal),";
                            break;
                        case "bigint":
                        case "money":
                        case "smallint":
                            ColumnNames = ColumnNames + AtributeName.ToString() + ",";
                            Values = Values + AtributeValue.ToString() + ",";
                            break;
                        case "bit":
                            ColumnNames = ColumnNames + AtributeName.ToString() + ",";
                            Values = Values + "'" + (AtributeValue.ToString() == "True" ? "1" : "0") + "',";
                            break;
                        case "datetime":
                        case "date":
                            ColumnNames = ColumnNames + AtributeName.ToString() + ",";
                            Values = Values + "CONVERT(DATETIME,'" + ((DateTime)AtributeValue).ToString("yyyyMMdd HH:mm:ss") + "'),";
                            break;
                    }
                }
                else continue;

            }
            ColumnNames = ColumnNames.TrimEnd(',');
            Values = Values.TrimEnd(',');
            string QUERY = "INSERT INTO " + entityProps[0].TABLE_SCHEMA + "." + Inst.GetType().Name + "(" + ColumnNames + ") VALUES(" + Values + ") SELECT SCOPE_IDENTITY()";
            LoggerServices.AddMessageInfo(QUERY);
            return QUERY;
        }
        protected override string BuildUpdateQueryByObject(object Inst, string IdObject)
        {
            string TableName = Inst.GetType().Name;
            string Values = "";
            string Conditions = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = DescribeEntity(Inst.GetType().Name);
            int index = 0;
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);
                if (AtributeValue != null && EntityProp != null)
                {
                    if (IdObject != AtributeName)
                    {
                        Values = BuildSetsForUpdate(Values, AtributeName, AtributeValue, EntityProp, oProperty);
                    }
                    else WhereConstruction(ref Conditions, ref index, AtributeName, AtributeValue);
                }
                else continue;
            }
            Values = Values.TrimEnd(',');
            string strQuery = "UPDATE  " + entityProps[0].TABLE_SCHEMA + "." + TableName + " SET " + Values + Conditions;
            LoggerServices.AddMessageInfo(strQuery);
            return strQuery;
        }
        protected override string BuildUpdateQueryByObject(object Inst, string[] WhereProps)
        {
            string TableName = Inst.GetType().Name;
            string Values = "";
            string Conditions = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = DescribeEntity(Inst.GetType().Name);
            int index = 0;
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);
                if (AtributeValue != null && EntityProp != null)
                {
                    if ((from O in WhereProps where O == AtributeName select O).ToList().Count == 0)
                    {
                        Values = BuildSetsForUpdate(Values, AtributeName, AtributeValue, EntityProp, oProperty);
                    }
                    else WhereConstruction(ref Conditions, ref index, AtributeName, AtributeValue);
                }
                else continue;
            }
            Values = Values.TrimEnd(',');
            string strQuery = "UPDATE  " + entityProps[0].TABLE_SCHEMA + "." + TableName + " SET " + Values + Conditions;
            LoggerServices.AddMessageInfo(strQuery);
            return strQuery;
        }
        protected override string BuildDeleteQuery(object Inst)
        {
            string TableName = Inst.GetType().Name;
            string CondicionString = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            int index = 0;
            List<EntityProps> entityProps = DescribeEntity(Inst.GetType().Name);
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);
                if (AtributeValue != null)
                {
                    WhereConstruction(ref CondicionString, ref index, AtributeName, AtributeValue);
                }

            }
            CondicionString = CondicionString.TrimEnd(new char[] { '0', 'R' });
            string strQuery = "DELETE FROM  " + entityProps[0].TABLE_SCHEMA + "." + TableName + CondicionString;
            LoggerServices.AddMessageInfo(strQuery);
            return strQuery;
        }
        protected override string BuildSelectQuery(object Inst, string CondSQL, bool fullEntity = true, bool isFind = false)
        {
            string CondicionString = "";
            string Columns = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = DescribeEntity(Inst.GetType().Name);
            int index = 0;
            string tableAlias = tableAliaGenerator();
            var filterData = Inst.GetType().GetProperty("filterData");
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);
                var oneToOne = (OneToOne?)Attribute.GetCustomAttribute(oProperty, typeof(OneToOne));
                var manyToOne = (ManyToOne?)Attribute.GetCustomAttribute(oProperty, typeof(ManyToOne));
                var oneToMany = (OneToMany?)Attribute.GetCustomAttribute(oProperty, typeof(OneToMany));
                if (EntityProp != null)
                {
                    var AtributeValue = oProperty.GetValue(Inst);
                    Columns = Columns + AtributeName + ",";
                    if (AtributeValue != null)
                    {
                        WhereConstruction(ref CondicionString, ref index, AtributeName, AtributeValue);
                    }
                    if (filterData != null && filterData.GetValue(Inst) != null)
                    {
                        WhereConstruction(ref CondicionString, ref index, AtributeName, oProperty, (List<FilterData>?)filterData.GetValue(Inst));
                    }
                }
                else if (manyToOne != null && fullEntity)
                {
                    var manyToOneInstance = Activator.CreateInstance(oProperty.PropertyType);
                    string condition = " " + manyToOne.KeyColumn + " = " + tableAlias + "." + manyToOne.ForeignKeyColumn;
                    Columns = Columns + AtributeName
                        + " = JSON_QUERY(("
                        + BuildSelectQuery(manyToOneInstance, condition, false)
                        + " FOR JSON PATH,  ROOT('object')),'$.object[0]'),";
                }
                else if (oneToOne != null && fullEntity)
                {
                    var oneToOneInstance = Activator.CreateInstance(oProperty.PropertyType);
                    List<PropertyInfo> pimaryKeyPropiertys = lst.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
                    PrimaryKey? pkInfo = (PrimaryKey?)Attribute.GetCustomAttribute(pimaryKeyPropiertys[0], typeof(PrimaryKey));
                    if (pkInfo != null)
                    {
                        string condition = " " + oneToOne.KeyColumn + " = " + tableAlias + "." + oneToOne.ForeignKeyColumn;
                        Columns = Columns + AtributeName
                            + " = JSON_QUERY(("
                            + BuildSelectQuery(oneToOneInstance, condition, pimaryKeyPropiertys.Find(p => pkInfo.Identity) != null)
                            + " FOR JSON PATH,  ROOT('object') ),'$.object[0]'),";
                    }
                }
                else if (oneToMany != null && fullEntity)
                {
                    var oneToManyInstance = Activator.CreateInstance(oProperty.PropertyType.GetGenericArguments()[0]);
                    string condition = " " + oneToMany.ForeignKeyColumn + " = " + tableAlias + "." + oneToMany.KeyColumn;
                    Columns = Columns + AtributeName
                        + " = ("
                        + BuildSelectQuery(oneToManyInstance, condition, oneToMany.TableName != Inst.GetType().Name)
                        + " FOR JSON PATH),";
                }

            }
            CondicionString = CondicionString.TrimEnd(new char[] { '0', 'R' });
            if (CondicionString == "" && CondSQL != "")
            {
                CondicionString = " WHERE ";
            }
            else if (CondicionString != "" && CondSQL != "")
            {
                CondicionString = CondicionString + " AND ";
            }
            Columns = Columns.TrimEnd(',');

            string queryString = "SELECT TOP 100 " + Columns
                + " FROM " + entityProps[0].TABLE_SCHEMA + "." + Inst.GetType().Name + " as " + tableAlias
                + CondicionString + CondSQL;

            PropertyInfo? primaryKeyPropierty = Inst?.GetType()?.GetProperties()?.ToList()?.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).FirstOrDefault();
            if (primaryKeyPropierty != null)
            {
                queryString = queryString + " ORDER BY " + primaryKeyPropierty.Name + " DESC";
            }
            return queryString;
        }
        private static string BuildSetsForUpdate(string Values, string AtributeName,
        object AtributeValue, EntityProps EntityProp, PropertyInfo oProperty)
        {
            switch (EntityProp.DATA_TYPE)
            {
                case "nvarchar":
                case "varchar":
                case "char":
                    JsonProp? json = (JsonProp?)Attribute.GetCustomAttribute(oProperty, typeof(JsonProp));
                    if (json != null)
                    {
                        String jsonV = JsonConvert.SerializeObject(AtributeValue);
                        Values = Values + AtributeName + "= '" + JValue.Parse(jsonV).ToString(Formatting.Indented) + "',";
                    }
                    else
                    {
                        Values = Values + AtributeName + "= '" + AtributeValue.ToString() + "',";
                    }
                    break;
                case "int":
                case "float":
                    Values = Values + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as float),";
                    break;
                case "decimal":
                    Values = Values + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as decimal),";
                    break;
                case "bigint":
                case "money":
                case "smallint":
                    Values = Values + AtributeName + "= " + AtributeValue.ToString() + ",";
                    break;
                case "bit":
                    Values = Values + AtributeName + "= '" + (AtributeValue.ToString() == "True" ? "1" : "0") + "',";
                    break;
                case "datetime":
                case "date":
                    Values = Values + AtributeName + "=  CONVERT(DATETIME,'" + ((DateTime)AtributeValue).ToString("yyyyMMdd HH:mm:ss") + "'),";
                    break;
            }

            return Values;
        }
        private static string tableAliaGenerator()
        {
            char ta = (char)(((int)'A') + new Random().Next(26));
            char ta2 = (char)(((int)'A') + new Random().Next(26));
            char ta3 = (char)(((int)'A') + new Random().Next(26));
            char ta4 = (char)(((int)'A') + new Random().Next(26));
            char ta5 = (char)(((int)'A') + new Random().Next(26));
            return ta.ToString() + ta2 + ta3 + "_" + ta4 + "_" + ta5;
        }
        private static void WhereConstruction(ref string CondicionString, ref int index, string AtributeName, object AtributeValue)
        {
            if (AtributeValue != null)
            {
                if (AtributeValue?.GetType() == typeof(string) && AtributeValue?.ToString()?.Length < 200)
                {
                    WhereOrAnd(ref CondicionString, ref index);
                    CondicionString = CondicionString + AtributeName + " LIKE '%" + AtributeValue.ToString() + "%' ";
                }
                else if (AtributeValue?.GetType() == typeof(DateTime))
                {
                    WhereOrAnd(ref CondicionString, ref index);
                    CondicionString = CondicionString + AtributeName
                        + "= '" + ((DateTime)AtributeValue).ToString("yyyy/MM/dd") + "' ";
                }
                else if (AtributeValue?.GetType() == typeof(int) || AtributeValue?.GetType() == typeof(int?))
                {
                    WhereOrAnd(ref CondicionString, ref index);
                    CondicionString = CondicionString + AtributeName + "=" + AtributeValue?.ToString() + " ";
                }
                else if (AtributeValue?.GetType() == typeof(Double))
                {
                    WhereOrAnd(ref CondicionString, ref index);
                    CondicionString = CondicionString + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as float)  ";
                }
                else if (AtributeValue?.GetType() == typeof(Decimal))
                {
                    WhereOrAnd(ref CondicionString, ref index);
                    CondicionString = CondicionString + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as decimal)  ";
                }
            }
        }

        private static void WhereConstruction(ref string CondicionString, ref int index,
            string AtributeName, PropertyInfo atribute, List<FilterData>? filterData = null)
        {
            if (filterData != null)
            {
                FilterData? filter = filterData?.Find(f => f?.PropName == AtributeName);
                if (filter != null && filter.Values != null && filter.Values.Count > 0)
                {
                    // WhereOrAnd(ref CondicionString, ref index);
                    var propertyType = Nullable.GetUnderlyingType(atribute?.PropertyType) ?? atribute?.PropertyType;
                    string? atributeType = propertyType?.Name;
                    switch (filter.FilterType?.ToUpper())
                    {
                        case "BETWEEN":
                            if (atributeType == "DateTime")
                            {
                                WhereOrAnd(ref CondicionString, ref index);
                                CondicionString = CondicionString + " ( " +
                                    (filter.Values[0] != null ? AtributeName + "  >= '" + filter.Values[0] + "'  " : " ") +
                                    (filter.Values.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                                    (filter.Values.Count > 1 ? AtributeName + " <= '" + filter.Values[1] + "' ) " : ") ");
                            }
                            else if (atributeType == "int"
                                                || atributeType == "Double"
                                                || atributeType == "Decimal"
                                                || atributeType == "int")
                            {
                                WhereOrAnd(ref CondicionString, ref index);
                                CondicionString = CondicionString + " ( " +
                                   (filter.Values[0] != null ? AtributeName + "  >= " + filter.Values[0] + "  " : " ") +
                                   (filter.Values.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                                   (filter.Values.Count > 1 ? AtributeName + " <= " + filter.Values[1] + " ) " : ") ");
                            }
                            break;
                        default:
                            if ((atributeType == "string" || atributeType == "String") && filter.Values[0]?.ToString()?.Length < 200)
                            {
                                WhereOrAnd(ref CondicionString, ref index);
                                CondicionString = CondicionString + AtributeName + " LIKE '%" + filter.Values[0] + "%' ";
                            }
                            else if (atributeType == "DateTime")
                            {
                                WhereOrAnd(ref CondicionString, ref index);
                                CondicionString = CondicionString + AtributeName
                                    + "= '" + filter.Values[0] + "' ";
                            }
                            else if (atributeType == "int"
                                                || atributeType == "Double"
                                                || atributeType == "Decimal"
                                                || atributeType == "int")
                            {
                                WhereOrAnd(ref CondicionString, ref index);
                                CondicionString = CondicionString + AtributeName + "=" + filter.Values[0]?.ToString() + " ";
                            }
                            break;
                    }
                }
            }
        }

        private static void WhereOrAnd(ref string CondicionString, ref int index)
        {

            if (!CondicionString.Contains("WHERE"))
            {
                CondicionString = " WHERE ";
                //index++;
            }
            else
            {
                CondicionString = CondicionString + " AND ";
            }
        }
        //DATA SQUEMA
        public List<EntitySchema> databaseSchemas()
        {
            string DescribeQuery = @"SELECT TABLE_SCHEMA FROM [INFORMATION_SCHEMA].[TABLES]  group by TABLE_SCHEMA";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }

        public List<EntitySchema> databaseTypes()
        {
            string DescribeQuery = @"SELECT TABLE_TYPE FROM [INFORMATION_SCHEMA].[TABLES]  group by TABLE_TYPE";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public List<EntitySchema> describeSchema(string schema, string type)
        {
            string DescribeQuery = @"SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE 
                                    FROM [INFORMATION_SCHEMA].[TABLES]  
                                    where TABLE_SCHEMA = '" + schema + "' and TABLE_TYPE = '" + type + "'";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public EntityColumn? describePrimaryKey(string table, string column)
        {
            string DescribeQuery = @"exec sp_columns'" + table + "'";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = ConvertDataTable<EntityColumn>(Table, new EntityColumn());
            return es.Find(e => e.COLUMN_NAME == column && e.TYPE_NAME.Contains("identity"));
        }

        public List<EntityProps> describeEntity(string entityName)
        {
            string DescribeQuery = @"SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE
                                    from [INFORMATION_SCHEMA].[COLUMNS] 
                                    WHERE [TABLE_NAME] = '" + entityName
                                   + "' order by [ORDINAL_POSITION]";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            return ConvertDataTable<EntityProps>(Table, new EntityProps());
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
            DataTable Table = TraerDatosSQL(DescribeQuery);
            return ConvertDataTable<OneToOneSchema>(Table, new OneToOneSchema());
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
            DataTable Table = TraerDatosSQL(DescribeQuery);
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
            DataTable Table = TraerDatosSQL(DescribeQuery);
            return Table.Rows.Count;
        }
        public List<OneToManySchema> oneToManyKeys(string entityName, string schema = "dbo")
        {
            string DescribeQuery = $"EXEC sp_fkeys @pktable_name = N'{entityName}' ,@pktable_owner = N'{schema}';";
            //string DescribeQuery = @"exec sp_fkeys '" + entityName + "'";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            return ConvertDataTable<OneToManySchema>(Table, new OneToManySchema());
        }
    }
}
