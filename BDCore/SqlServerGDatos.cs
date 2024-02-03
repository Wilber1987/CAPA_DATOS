using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CAPA_DATOS;
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
        protected override IDbConnection SQLMCon
        {
            get
            {
                if (this.MTConnection != null)
                {
                    return this.MTConnection;
                }
                return CrearConexion(ConexionString);
            }
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
        public override DataTable ExecuteProcedure(object Inst, List<object> Params)
        {
            var conec = CrearConexion(ConexionString);
            var Command = ComandoSql(Inst.GetType().Name, conec);
            Command.CommandType = CommandType.StoredProcedure;
            conec.Open();
            SqlCommandBuilder.DeriveParameters((SqlCommand)Command);
            conec.Close();
            if (Params?.Count != 0)
            {
                int i = 0;
                foreach (var param in Params ?? new List<object>())
                {
                    if (Command != null)
                    {
                        SqlParameter? p = (SqlParameter?)Command.Parameters[i + 1];
                        if (p != null)
                            p.Value = param;
                    }
                    i++;
                }
            }
            DataTable Table = TraerDatosSQL(Command);
            return Table;
        }

        protected override List<EntityProps> DescribeEntity(string entityName)
        {
            string DescribeQuery = @"SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE, TABLE_SCHEMA
                                    from [INFORMATION_SCHEMA].[COLUMNS] 
                                    WHERE [TABLE_NAME] = '" + entityName
                                   + "' order by [ORDINAL_POSITION]";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            List<EntityProps> entityProps = AdapterUtil.ConvertDataTable<EntityProps>(Table, new EntityProps());
            if (entityProps.Count == 0)
            {
                throw new Exception("La entidad buscada no existe: " + entityName);
            }
            return entityProps;
        }
        protected override  (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(object Inst, string IdObject)
        {
            return BuildUpdateQueryByObject(Inst, new string[] { IdObject });
        }
        protected override  (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(object Inst, string[] WhereProps)
        {
            string TableName = Inst.GetType().Name;
            string Values = "";
            string Conditions = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = DescribeEntity(Inst.GetType().Name);
            int index = 0;
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);
                if (AtributeValue != null && EntityProp != null)
                {
                    if ((from O in WhereProps where O == AtributeName select O).ToList().Count == 0)
                    {
                        string paramName = "@" + AtributeName;
                        Values = Values + $"{AtributeName} = {paramName},";
                        IDbDataParameter parameter = CreateParameter(paramName, AtributeValue, EntityProp.DATA_TYPE, oProperty);
                        parameters.Add(parameter);
                        //Values = BuildSetsForUpdate(Values, AtributeName, AtributeValue, EntityProp, oProperty);
                    }
                    else WhereConstruction(ref Conditions, ref index, AtributeName, AtributeValue);
                }
                else continue;
            }
            Values = Values.TrimEnd(',');
            if (Values == "")
            {
                return (null, null);
            }
            string strQuery = "UPDATE  " + entityProps[0].TABLE_SCHEMA + "." + TableName + " SET " + Values + Conditions;
            LoggerServices.AddMessageInfo(strQuery);
            return (strQuery, parameters);
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
        protected override (string queryResults, string queryCount) BuildSelectQuery(object Inst, string CondSQL,
         bool fullEntity = true, bool isFind = false, string? orderBy = null, string? orderDir = null)
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
                }
                else if (manyToOne != null && fullEntity)
                {
                    var manyToOneInstance = Activator.CreateInstance(oProperty.PropertyType);
                    string condition = " " + manyToOne.KeyColumn + " = " + tableAlias + "." + manyToOne.ForeignKeyColumn;
                    (string subquery, _) = BuildSelectQuery(manyToOneInstance, condition, false);
                    Columns = Columns + AtributeName
                        + $" = JSON_QUERY(({subquery} FOR JSON PATH,  ROOT('object')),'$.object[0]'),";
                }
                else if (oneToOne != null && fullEntity)
                {
                    var oneToOneInstance = Activator.CreateInstance(oProperty.PropertyType);
                    List<PropertyInfo> pimaryKeyPropiertys = lst.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
                    PrimaryKey? pkInfo = (PrimaryKey?)Attribute.GetCustomAttribute(pimaryKeyPropiertys[0], typeof(PrimaryKey));
                    if (pkInfo != null)
                    {
                        string condition = " " + oneToOne.KeyColumn + " = " + tableAlias + "." + oneToOne.ForeignKeyColumn;
                        (string subquery, _) = BuildSelectQuery(oneToOneInstance, condition, pimaryKeyPropiertys.Find(p => pkInfo.Identity) != null);
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
                    (string subquery, _) = BuildSelectQuery(oneToManyInstance, condition, oneToMany.TableName != Inst.GetType().Name);
                    Columns = Columns + AtributeName
                        + $" = ({subquery} FOR JSON PATH),";
                }

            }
            if (filterData != null && filterData.GetValue(Inst) != null)
            {
                foreach (FilterData filter in (List<FilterData>?)filterData.GetValue(Inst) ?? new List<FilterData>())
                {
                    string filterCond = SetFilterValueCondition(lst, filter);
                    if (filterCond.Length != 0)
                    {
                        WhereOrAnd(ref CondicionString);
                        CondicionString += filterCond;
                    }
                    //string pattern = @"\b(?:AND|OR| AND | OR )\b$";
                    // Reemplazar la coincidencia con una cadena vacía.
                    //CondicionString = Regex.Replace(CondicionString, pattern, string.Empty);
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

            string queryString = $"SELECT {Columns} FROM {entityProps[0].TABLE_SCHEMA}.{Inst.GetType().Name} as {tableAlias} {CondicionString} {CondSQL} ";

            PropertyInfo? primaryKeyPropierty = Inst?.GetType()?.GetProperties()?.ToList()?.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).FirstOrDefault();
            if (orderBy != null)
            {
                queryString = queryString + $" ORDER BY {orderBy} {(orderDir == null ? "ASC" : "DESC")} ";
            }
            if (orderBy == null && primaryKeyPropierty != null)
            {
                queryString = queryString + " ORDER BY " + primaryKeyPropierty.Name + " DESC";
            }
            string queryStringCount = $" SELECT count(*) FROM {entityProps[0].TABLE_SCHEMA}.{Inst?.GetType().Name} as {tableAlias} {CondicionString} {CondSQL};";

            return (queryString, queryStringCount);
        }
        protected override string BuildSetsForUpdate(string Values, string AtributeName,
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
        public override List<EntitySchema> databaseSchemas()
        {
            string DescribeQuery = @"SELECT TABLE_SCHEMA FROM [INFORMATION_SCHEMA].[TABLES]  group by TABLE_SCHEMA";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = AdapterUtil.ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public override List<EntitySchema> databaseTypes()
        {
            string DescribeQuery = @"SELECT TABLE_TYPE FROM [INFORMATION_SCHEMA].[TABLES]  group by TABLE_TYPE";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = AdapterUtil.ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public override List<EntitySchema> describeSchema(string schema, string type)
        {
            string DescribeQuery = @"SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE 
                                    FROM [INFORMATION_SCHEMA].[TABLES]  
                                    where TABLE_SCHEMA = '" + schema + "' and TABLE_TYPE = '" + type + "'";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = AdapterUtil.ConvertDataTable<EntitySchema>(Table, new EntitySchema());
            return es;
        }
        public override EntityColumn? describePrimaryKey(string table, string column)
        {
            string DescribeQuery = @"exec sp_columns'" + table + "'";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            var es = AdapterUtil.ConvertDataTable<EntityColumn>(Table, new EntityColumn());
            return es.Find(e => e.COLUMN_NAME == column && e.TYPE_NAME.Contains("identity"));
        }
        public override List<EntityProps> describeEntity(string entityName)
        {
            string DescribeQuery = @"SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE
                                    from [INFORMATION_SCHEMA].[COLUMNS] 
                                    WHERE [TABLE_NAME] = '" + entityName
                                   + "' order by [ORDINAL_POSITION]";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            return AdapterUtil.ConvertDataTable<EntityProps>(Table, new EntityProps());
        }
        public override List<OneToOneSchema> ManyToOneKeys(string entityName)
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
            return AdapterUtil.ConvertDataTable<OneToOneSchema>(Table, new OneToOneSchema());
        }
        public override Boolean isPrimary(string entityName, string column)
        {
            return evalKeyType(entityName, column, "PRIMARY KEY") > 0;
        }
        public override Boolean isForeinKey(string entityName, string column)
        {
            return evalKeyType(entityName, column, "FOREIGN KEY") > 0;
        }
        public override int evalKeyType(string entityName, string column, string keyType)
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
        public override int keyInformation(string entityName, string keyType)
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
        public override List<OneToManySchema> oneToManyKeys(string entityName, string schema = "dbo")
        {
            string DescribeQuery = $"EXEC sp_fkeys @pktable_name = N'{entityName}' ,@pktable_owner = N'{schema}';";
            //string DescribeQuery = @"exec sp_fkeys '" + entityName + "'";
            DataTable Table = TraerDatosSQL(DescribeQuery);
            return AdapterUtil.ConvertDataTable<OneToManySchema>(Table, new OneToManySchema());
        }
        //PAGINACION
        protected override (string queryResults, string queryCount) BuildSelectQueryPaginated(object Inst, string CondSQL, int pageNum, int pageSize, string orderBy, string orderDir, bool fullEntity = true, bool isFind = false)
        {
            (string queryString, string queryCount) = BuildSelectQuery(Inst, CondSQL, fullEntity, isFind, orderBy, orderDir);
            // paginación
            queryString = queryString + " OFFSET " + (pageNum - 1) * pageSize + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY";
            return (queryString, queryCount);
        }

        public override IDbDataParameter CreateParameter(string name, object value, string dataType, PropertyInfo oProperty)
        {
            SqlDbType sqlDbType;
            switch (dataType)
            {
                case "nvarchar":
                case "varchar":
                case "char":
                    sqlDbType = SqlDbType.NVarChar;
                    break;
                case "int":
                case "float":
                    sqlDbType = SqlDbType.Float;
                    break;
                case "decimal":
                    sqlDbType = SqlDbType.Decimal;
                    break;
                case "bigint":
                case "money":
                case "smallint":
                    sqlDbType = SqlDbType.Int;
                    break;
                case "bit":
                    sqlDbType = SqlDbType.Bit;
                    break;
                case "datetime":
                case "date":
                    sqlDbType = SqlDbType.DateTime;
                    break;
                default:
                    throw new ArgumentException($"Tipo de datos no soportado: {dataType}");
            }

            JsonProp? jsonPropAttribute = (JsonProp?)Attribute.GetCustomAttribute(oProperty, typeof(JsonProp));
            if (jsonPropAttribute != null)
            {
                // Tratar como JSON
                string jsonValue = JsonConvert.SerializeObject(value);
                return new SqlParameter(name, sqlDbType) { Value = JValue.Parse(jsonValue).ToString(Formatting.Indented) };
            }
            else
            {
                return new SqlParameter(name, sqlDbType) { Value = value };
            }
        }

    }
}
