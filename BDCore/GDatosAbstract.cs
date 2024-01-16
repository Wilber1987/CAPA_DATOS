using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAPA_DATOS
{
    public abstract class GDatosAbstract
    {
        protected abstract IDbConnection SQLMCon { get; }
        protected String? ConexionString;
        protected IDbTransaction? MTransaccion;
        protected bool globalTransaction;
        protected IDbConnection? MTConnection;
        protected abstract IDbConnection CrearConexion(string cadena);
        protected abstract IDbCommand ComandoSql(string comandoSql,
                                                 IDbConnection connection);
        protected abstract IDataAdapter CrearDataAdapterSql(string comandoSql, IDbConnection connection);
        protected abstract IDataAdapter CrearDataAdapterSql(IDbCommand comandoSql);
        protected abstract List<EntityProps> DescribeEntity(string entityName);
        public abstract DataTable ExecuteProcedure(object Inst, List<object> Params);
        protected abstract (string queryResults, string queryCount) BuildSelectQuery(object Inst, string CondSQL,
            bool fullEntity = true, bool isFind = true, string? orderBy = null, string? orderDir = null);

        protected abstract (string queryResults, string queryCount) BuildSelectQueryPaginated(object Inst, string CondSQL,
            int pageNum, int pageSize, string orderBy, string orderDir, bool fullEntity = true, bool isFind = true);

        protected abstract string? BuildUpdateQueryByObject(object Inst, string IdObject);
        protected abstract string? BuildUpdateQueryByObject(object Inst, string[] WhereProps);
        protected abstract string BuildDeleteQuery(object Inst);
        protected abstract string BuildSetsForUpdate(string Values, string AtributeName,
        object AtributeValue, EntityProps EntityProp, PropertyInfo oProperty);

        public abstract List<EntitySchema> databaseSchemas();
        public abstract List<EntitySchema> databaseTypes();
        public abstract List<EntitySchema> describeSchema(string schema, string type);
        public abstract EntityColumn? describePrimaryKey(string table, string column);
        public abstract List<EntityProps> describeEntity(string entityName);
        public abstract List<OneToOneSchema> ManyToOneKeys(string entityName);
        public abstract Boolean isPrimary(string entityName, string column);
        public abstract Boolean isForeinKey(string entityName, string column);
        public abstract int evalKeyType(string entityName, string column, string keyType);
        public abstract int keyInformation(string entityName, string keyType);
        public abstract List<OneToManySchema> oneToManyKeys(string entityName, string schema = "dbo");


        #region ADO.NET METHODS
        public bool TestConnection()
        {
            try
            {
                SQLMCon.Open();
                SQLMCon.Close();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void BeginTransaction()
        {
            try
            {
                if (this.globalTransaction)
                {
                    return;
                }
                this.MTConnection = null;
                LoggerServices.AddMessageInfo("-- > BEGIN TRANSACTION <=================");
                MTConnection = SQLMCon;
                SQLMCon.Open();
                this.MTransaccion = SQLMCon.BeginTransaction();
            }
            catch (TransactionException e)
            {
                LoggerServices.AddMessageError("BEGIN TRANSACTION ERROR", e);
            }
            catch (Exception e)
            {
                LoggerServices.AddMessageError("BEGIN TRANSACTION ERROR", e);
            }
        }
        public void CommitTransaction()
        {
            if (this.globalTransaction)
            {
                return;
            }
            LoggerServices.AddMessageInfo("-- > COMMIT TRANSACTION <=================");
            this.MTransaccion?.Commit();
            SQLMCon.Close();
            MTConnection = null;
        }
        public void RollBackTransaction()
        {
            if (this.globalTransaction)
            {
                return;
            }
            this.MTransaccion?.Rollback();
            SQLMCon.Close();
            MTConnection = null;
            LoggerServices.AddMessageInfo("-- > ROOLBACK TRANSACTION <=================");
        }
        public void BeginGlobalTransaction()
        {
            this.globalTransaction = true;
            this.MTConnection = null;
            MTConnection = SQLMCon;
            SQLMCon.Open();
            this.MTransaccion = SQLMCon.BeginTransaction();
            LoggerServices.AddMessageInfo("-- > BEGIN TRANSACTION <=================");
        }
        public void CommitGlobalTransaction()
        {
            this.globalTransaction = false;
            this.MTransaccion?.Commit();
            SQLMCon.Close();
            MTConnection = null;
            LoggerServices.AddMessageInfo("-- > COMMIT TRANSACTION <=================");
        }
        public void RollBackGlobalTransaction()
        {
            this.globalTransaction = false;
            LoggerServices.AddMessageInfo("-- > ROOLBACK TRANSACTION <=================");
            this.MTransaccion?.Rollback();
            SQLMCon.Close();
            MTConnection = null;
        }
        public object ExcuteSqlQuery(string? strQuery)
        {
            var com = ComandoSql(strQuery, SQLMCon);
            com.Transaction = this.MTransaccion;
            var scalar = com.ExecuteScalar();
            if (scalar == (object)DBNull.Value) return true;
            else return Convert.ToInt32(scalar);
        }
        public DataTable TraerDatosSQL(string queryString)
        {
            try
            {
                DataSet ObjDS = new DataSet();
                var comando = ComandoSql(queryString, SQLMCon);
                comando.Transaction = this.MTransaccion;
                CrearDataAdapterSql(comando).Fill(ObjDS);
                return ObjDS.Tables[0].Copy();
            }
            catch (System.Exception)
            {
                Console.Write(queryString);
                throw;
            }

        }
        public DataTable TraerDatosSQL(IDbCommand Command)
        {
            DataSet ObjDS = new DataSet();
            Command.Transaction = this.MTransaccion;
            CrearDataAdapterSql(Command).Fill(ObjDS);
            return ObjDS.Tables[0].Copy();
        }
        #endregion

        #region ORM INSERT, DELETE, UPDATES METHODS
        public object? InsertObject(Object entity)
        {
            LoggerServices.AddMessageInfo("-- >  InsertObject(" + entity.GetType().Name + ")");
            List<PropertyInfo> entityProps = entity.GetType().GetProperties().ToList();
            List<PropertyInfo> pimaryKeyPropiertys = entityProps.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
            List<PropertyInfo> manyToOneProps = entityProps.Where(p => Attribute.GetCustomAttribute(p, typeof(ManyToOne)) != null).ToList();
            // SELECCIONAR LOS VALORES DE LAS LLAVES PRIMARIAS DE LOS MANYTOONE
            SetManyToOnePropiertys(entity, manyToOneProps);
            string? strQuery = BuildInsertQueryByObject(entity);
            if (strQuery == null)
            {
                return null;
            }
            object idGenerated = ExcuteSqlQuery(strQuery);

            if (pimaryKeyPropiertys.Count == 1)
            {
                PrimaryKey? pkInfo = (PrimaryKey?)Attribute.GetCustomAttribute(pimaryKeyPropiertys[0], typeof(PrimaryKey));
                if (pkInfo?.Identity == true)
                {
                    Type? pkType = Nullable.GetUnderlyingType(pimaryKeyPropiertys[0].PropertyType);
                    pimaryKeyPropiertys[0].SetValue(entity, Convert.ChangeType(idGenerated, pkType));
                }
            }
            List<PropertyInfo> oneToOnePropiertys = entityProps.Where(p => Attribute.GetCustomAttribute(p, typeof(OneToOne)) != null).ToList();
            foreach (var oneToOneProp in oneToOnePropiertys)
            {
                string? atributeName = oneToOneProp.Name;
                var atributeValue = oneToOneProp.GetValue(entity);
                if (atributeValue != null)
                {
                    OneToOne? oneToOne = (OneToOne?)Attribute.GetCustomAttribute(oneToOneProp, typeof(OneToOne));//TODO revisar relaciones
                    PropertyInfo? KeyColumn = entity?.GetType().GetProperty(oneToOne?.KeyColumn);
                    PropertyInfo? ForeignKeyColumn = atributeValue.GetType().GetProperty(oneToOne?.ForeignKeyColumn);
                    if (ForeignKeyColumn != null)
                    {
                        var primaryKeyValue = entity?.GetType()?.GetProperty(KeyColumn?.Name)?.GetValue(entity);
                        ForeignKeyColumn.SetValue(atributeValue, primaryKeyValue);
                        InsertObject(atributeValue);
                    }

                }
            }

            List<PropertyInfo> oneToManyPropiertys = entityProps.Where(p => Attribute.GetCustomAttribute(p, typeof(OneToMany)) != null).ToList();
            foreach (var oneToManyProp in oneToManyPropiertys)
            {
                string? atributeName = oneToManyProp.Name;
                var atributeValue = oneToManyProp.GetValue(entity);
                if (atributeValue != null)
                {
                    OneToMany? oneToMany = (OneToMany?)Attribute.GetCustomAttribute(oneToManyProp, typeof(OneToMany));
                    foreach (var value in ((IEnumerable)atributeValue))
                    {
                        PropertyInfo? KeyColumn = value.GetType().GetProperty(oneToMany?.KeyColumn);
                        PropertyInfo? ForeignKeyColumn = value.GetType().GetProperty(oneToMany?.ForeignKeyColumn);
                        if (ForeignKeyColumn != null)
                        {
                            var primaryKeyValue = entity?.GetType()?.GetProperty(KeyColumn?.Name)?.GetValue(entity);
                            InsertRelationatedObject(primaryKeyValue, value, ForeignKeyColumn);
                        }
                    }
                }
            }
            return entity;
        }

        private void SetManyToOnePropiertys(object entity, List<PropertyInfo> manyToOneProps)
        {
            if (manyToOneProps == null) return;
            foreach (var manyToOneProp in manyToOneProps)
            {
                var atributeValue = manyToOneProp.GetValue(entity);
                if (atributeValue != null)
                {
                    ManyToOne? manyToOne = (ManyToOne?)Attribute.GetCustomAttribute(manyToOneProp, typeof(ManyToOne));
                    PropertyInfo? KeyColumn = atributeValue.GetType().GetProperty(manyToOne?.KeyColumn);
                    PropertyInfo? ForeignKeyColumn = entity.GetType().GetProperty(manyToOne?.ForeignKeyColumn);
                    if (KeyColumn != null)
                    {
                        if (KeyColumn?.GetValue(atributeValue) == null)
                        {
                            this.InsertObject(atributeValue);
                        }
                    }
                    if (KeyColumn != null && ForeignKeyColumn != null)
                    {
                        var FK = entity.GetType().GetProperty(ForeignKeyColumn.Name);
                        var keyVal = atributeValue?.GetType()?.GetProperty(KeyColumn?.Name)?.GetValue(atributeValue);
                        if (keyVal != null)
                        {
                            FK?.SetValue(entity, keyVal);
                        }
                    }
                }
            }
        }

        private void InsertRelationatedObject(object foreingKeyValue, object entity, PropertyInfo foreignKeyColumn)
        {
            LoggerServices.AddMessageInfo("-- > InsertRelationatedObject( -> " + entity.GetType().Name + "): ");
            foreignKeyColumn.SetValue(entity, foreingKeyValue);
            List<PropertyInfo> entityProps = entity.GetType().GetProperties().ToList();
            var pkPropiertys = entityProps.Where(p => (PrimaryKey?)Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
            var values = pkPropiertys.Where(p => p.GetValue(entity) != null).ToList();
            if (pkPropiertys.Count == values.Count)
            {
                UpdateObject(entity, pkPropiertys.Select(p => p.Name).ToArray());
            }
            else this.InsertObject(entity);

        }

        public object? UpdateObject(Object entity, string[] IdObject)
        {
            LoggerServices.AddMessageInfo("-- > UpdateObject(Object Inst, string[] IdObject)");
            List<PropertyInfo> entityProps = entity.GetType().GetProperties().ToList();
            List<PropertyInfo> pimaryKeyPropiertys = entityProps.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
            List<PropertyInfo> manyToOneProps = entityProps.Where(p => Attribute.GetCustomAttribute(p, typeof(ManyToOne)) != null).ToList();
            // SELECCIONAR LOS VALORES DE LAS LLAVES PRIMARIAS DE LOS MANYTOONE
            SetManyToOnePropiertys(entity, manyToOneProps);
            string? strQuery = BuildUpdateQueryByObject(entity, IdObject);

            List<PropertyInfo> oneToManyPropiertys = entityProps.Where(p =>
                Attribute.GetCustomAttribute(p, typeof(OneToMany)) != null).ToList();
            foreach (var oneToManyProp in oneToManyPropiertys)
            {
                string? atributeName = oneToManyProp.Name;
                var atributeValue = oneToManyProp.GetValue(entity);
                if (atributeValue != null)
                {
                    List<PropertyInfo> atributeValueManyToOneProps =
                        atributeValue.GetType().GetProperties().Where(p =>
                        Attribute.GetCustomAttribute(p, typeof(ManyToOne)) != null).ToList();
                    SetManyToOnePropiertys(atributeValue, atributeValueManyToOneProps);
                    OneToMany? oneToMany = (OneToMany?)Attribute.GetCustomAttribute(oneToManyProp, typeof(OneToMany));
                    foreach (var value in (IEnumerable)atributeValue)
                    {
                        PropertyInfo? KeyColumn = value.GetType().GetProperty(oneToMany?.KeyColumn);
                        PropertyInfo? ForeignKeyColumn = value.GetType().GetProperty(oneToMany?.ForeignKeyColumn);
                        if (ForeignKeyColumn != null)
                        {
                            var primaryKeyValue = entity?.GetType()?.GetProperty(KeyColumn?.Name)?.GetValue(entity);
                            InsertRelationatedObject(primaryKeyValue, value, ForeignKeyColumn);
                        }
                    }
                }
            }

            if (strQuery != null)
            {
                ExcuteSqlQuery(strQuery);
            }
            return entity;
        }
        public object UpdateObject(Object Inst, string IdObject)
        {
            LoggerServices.AddMessageInfo("-- > UpdateObject(Object Inst, string IdObject)");
            if (Inst.GetType().GetProperty(IdObject)?.GetValue(Inst) == null)
            {
                throw new Exception("Valor de la propiedad "
                    + IdObject + " en la instancia "
                    + Inst.GetType().Name + " esta en nulo y no es posible actualizar");
            }
            string? strQuery = BuildUpdateQueryByObject(Inst, IdObject);
            return ExcuteSqlQuery(strQuery);
        }
        public object Delete(Object Inst)
        {
            LoggerServices.AddMessageInfo("-- > Delete(Object Inst)");
            string? strQuery = BuildDeleteQuery(Inst);
            return ExcuteSqlQuery(strQuery);
        }

        #endregion

        #region LECTURA DE OBJETOS
        public List<T> TakeList<T>(Object Inst, bool fullEntity, string CondSQL = "")
        {
            try
            {
                DataTable Table = BuildTable(Inst, ref CondSQL, fullEntity, false);
                List<T> ListD = AdapterUtil.ConvertDataTable<T>(Table, Inst);
                return ListD;
            }
            catch (Exception e)
            {
                SQLMCon.Close();
                LoggerServices.AddMessageError($"ERROR: TakeList - {Inst.GetType().Name} - {fullEntity} - {CondSQL}", e);
                throw;
            }
        }
        public List<T> TakeList<T>(Object Inst, string queryString)
        {
            try
            {
                DataTable Table = TraerDatosSQL(queryString);
                List<T> ListD = AdapterUtil.ConvertDataTable<T>(Table, Inst);
                return ListD;
            }
            catch (Exception e)
            {
                SQLMCon.Close();
                LoggerServices.AddMessageError($"ERROR: TakeList - {Inst.GetType().Name} - {queryString}", e);
                throw;
            }
        }

        public T? TakeObject<T>(Object Inst, string CondSQL = "")
        {
            (string queryString, string queryCount) = BuildSelectQuery(Inst, CondSQL, true, true);
            if (!queryString.ToUpper().Contains(" WHERE "))
            {
                throw new Exception($"No es posible buscar el objeto la entidad {Inst.GetType().Name} requiere filtros o parametros con valores para hacer la compariva");
            }
            DataTable Table = TraerDatosSQL(queryString);
            if (Table.Rows.Count != 0)
            {
                var CObject = AdapterUtil.ConvertRow<T>(Inst, Table.Rows[0]);
                return CObject;
            }
            else
            {
                return default;
            }
        }

        protected private DataTable BuildTable(object Inst, ref string CondSQL, bool fullEntity = true, bool isFind = true)
        {
            (string queryString, string queryCount) = BuildSelectQuery(Inst, CondSQL, fullEntity, isFind);
            DataTable Table = TraerDatosSQL(queryString);
            return Table;
        }
        /*
        * Interfaz de tipo que permitirá retornar una lista del tipo específico de la entidad 
        * que correspondería, en lugar de una lista genérica.
        *
        * A diferencia del anterior de paginación, acá se reciben los dos string de SQL directo para ejecutarse
        * (Raw SQL).
        */
        public void TakeListPaginated<T>(Object Inst, string queryString, string queryCount, out List<T> data, out int totalRecordsQuery)
        {
            try
            {
                (DataTable Table, int totalRecords) = BuildTablePaginated(queryString, queryCount);
                List<T> ListD = AdapterUtil.ConvertDataTable<T>(Table, Inst);
                data = ListD;
                totalRecordsQuery = totalRecords;
            }
            catch (Exception)
            {
                SQLMCon.Close();
                throw;
            }
        }
        /*
        * Utlizado para la lectura de los datos
        */
        protected (DataTable, int) BuildTablePaginated(object Inst, ref string CondSQL, int pageNum, int pageSize, string orderBy, string orderDir,
            bool fullEntity = true, bool isFind = true)
        {
            (string queryString, string queryCount) = BuildSelectQueryPaginated(Inst, CondSQL, pageNum, pageSize, orderBy, orderDir, fullEntity, isFind);
            return BuildTablePaginated(queryString, queryCount);
        }

        /*
        * Utlizado para la lectura de los datos (Esta variante sirve para cuando ya viene el string SQL)
        */
        protected (DataTable, int) BuildTablePaginated(string queryString, string queryCount)
        {
            LoggerServices.AddMessageInfo(queryString);
            DataTable Table = TraerDatosSQL(queryString);
            int totalRecords = TraerDatosSQL(queryCount).Rows.Count;
            return (Table, totalRecords);
        }

        public List<T> TakeListWithProcedure<T>(Object Inst, List<Object> Params)
        {
            try
            {
                DataTable Table = ExecuteProcedure(Inst, Params);
                List<T> ListD = AdapterUtil.ConvertDataTable<T>(Table, Inst);
                return ListD;
            }
            catch (Exception e)
            {
                LoggerServices.AddMessageError("ERROR: TakeListWithProcedure", e);
                throw;
            }
        }

        #endregion

        #region  QUERYBUILDER IMPLEMANTATIONS
        protected string? BuildInsertQueryByObject(object Inst)
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
            if (Values == "")
            {
                return null;
            }
            string QUERY = "INSERT INTO " + entityProps[0].TABLE_SCHEMA + "." + Inst.GetType().Name + "(" + ColumnNames + ") VALUES(" + Values + ") SELECT SCOPE_IDENTITY()";
            LoggerServices.AddMessageInfo(QUERY);
            return QUERY;
        }
        protected string tableAliaGenerator()
        {
            char ta = (char)(((int)'A') + new Random().Next(26));
            char ta2 = (char)(((int)'A') + new Random().Next(26));
            char ta3 = (char)(((int)'A') + new Random().Next(26));
            char ta4 = (char)(((int)'A') + new Random().Next(26));
            char ta5 = (char)(((int)'A') + new Random().Next(26));
            return ta.ToString() + ta2 + ta3 + "_" + ta4 + "_" + ta5;
        }
        protected void WhereConstruction(ref string CondicionString, ref int index, string AtributeName, object AtributeValue)
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

        protected string SetFilterValueCondition(PropertyInfo[] props, FilterData filter)
        {
            string CondicionString = "";
            var prop = props.ToList().Find(p => p.Name.Equals(filter?.PropName));
            string atributeType = "";
            string AtributeName = "";
            if (prop != null)
            {
                atributeType = prop.GetType().Name;
                AtributeName = prop.Name;
            }
            switch (filter.FilterType?.ToUpper())
            {
                case "AND" :case "OR":
                    if (filter.Filters != null && filter.Filters.Count != 0)
                    {
                        CondicionString += $" ({string.Join($" {filter.FilterType} ", filter.Filters.Select(f => SetFilterValueCondition(props, f)))})";
                    }
                    break;
                case "BETWEEN":
                    if (atributeType == "DateTime")
                    {
                        CondicionString = CondicionString + " ( " +
                            (filter?.Values?[0] != null ? AtributeName + "  >= '" + filter.Values[0] + "'  " : " ") +
                            (filter?.Values?.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                            (filter?.Values?.Count > 1 ? AtributeName + " <= '" + filter.Values[1] + "' ) " : ") ");
                    }
                    else if (atributeType == "Int32"
                                        || atributeType == "Double"
                                        || atributeType == "Decimal"
                                        || atributeType == "int")
                    {
                        CondicionString = CondicionString + " ( " +
                           (filter?.Values?[0] != null ? AtributeName + "  >= " + filter.Values[0] + "  " : " ") +
                           (filter?.Values?.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                           (filter?.Values?.Count > 1 ? AtributeName + " <= " + filter.Values[1] + " ) " : ") ");
                    }
                    break;
                case "IN":
                case "NOT IN":
                    if (filter?.Values?.Count > 0)
                    {
                         CondicionString = CondicionString + AtributeName + $" {filter?.FilterType} (" + BuildArrayIN(filter?.Values, atributeType) + ") ";
                    }                   
                    break;
                case "LIKE":
                    CondicionString = CondicionString + AtributeName + " LIKE '%" + filter?.Values?[0] + "%' ";
                    break;
                default:                    
                    //if ((atributeType == "string" || atributeType == "String" || atributeType == "DateTime") && Value?.Length < 200)
                    if (atributeType == "int" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "int")
                        CondicionString += $" {AtributeName} {filter?.FilterType} {filter?.Values?[0]} ";
                    else
                        CondicionString += $" {AtributeName} {filter.FilterType} '{filter?.Values?[0]}' ";
                    break;
            }

            return CondicionString;
        }


        protected void WhereOrAnd(ref string CondicionString, ref int index)
        {
            if (!CondicionString.Contains("WHERE"))
                CondicionString = " WHERE ";
            else
                CondicionString += " AND ";
        }
        public static string BuildArrayIN(List<string?> conditions, string atributeType = "string")
        {
            string CondicionString = "";
            foreach (string? Value in conditions)
            {
                //if ((atributeType == "string" || atributeType == "String" || atributeType == "DateTime") && Value?.Length < 200)
                if (atributeType == "int" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "int")
                    CondicionString = CondicionString + Value?.ToString() + ",";
                else
                    CondicionString = CondicionString + "'" + Value + "',";

            }
            CondicionString = CondicionString.TrimEnd(',');
            return CondicionString;
        }
        //DATA SQUEMA
        #endregion


    }

}
