using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CAPA_DATOS.BDCore.Abstracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAPA_DATOS.BDCore.Implementations
{
    public class SQLServerQueryBuilder : BDQueryBuilderAbastract
    {      
        public override (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string IdObject)
        {
            return BuildUpdateQueryByObject(Inst, new string[] { IdObject });
        }
        public override (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string[] WhereProps)
        {
            string TableName = Inst.GetType().Name;
            string Values = "";
            string Conditions = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());
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
        public override string BuildDeleteQuery(EntityClass Inst)
        {
            string TableName = Inst.GetType().Name;
            string CondicionString = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            int index = 0;
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());
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
        public override (string queryResults, string queryCount) BuildSelectQuery(EntityClass Inst, string CondSQL,
         bool fullEntity = true, bool isFind = false, string? orderBy = null, string? orderDir = null)
        {
            string CondicionString = "";
            string Columns = "";
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());
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
                    (string subquery, _) = BuildSelectQuery((EntityClass?)manyToOneInstance, condition, false);
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
                        (string subquery, _) = BuildSelectQuery((EntityClass?)oneToOneInstance, condition, pimaryKeyPropiertys.Find(p => pkInfo.Identity) != null);
                        Columns = Columns + AtributeName
                            + " = JSON_QUERY(("
                            + subquery
                            + " FOR JSON PATH,  ROOT('object') ),'$.object[0]'),";
                    }
                }
                else if (oneToMany != null && fullEntity)
                {
                    var oneToManyInstance = Activator.CreateInstance(oProperty.PropertyType.GetGenericArguments()[0]);
                    string condition = " " + oneToMany.ForeignKeyColumn + " = " + tableAlias + "." + oneToMany.KeyColumn;
                    (string subquery, _) = BuildSelectQuery((EntityClass?)oneToManyInstance, condition, oneToMany.TableName != Inst.GetType().Name);
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
            
			FilterData? filterLimit = ((List<FilterData>?)filterData?.GetValue(Inst))?.Find(f =>
					f.FilterType?.ToLower().Contains("limit") == true);

			string queryString = $"SELECT {(filterLimit != null ? $" top {filterLimit?.Values?[0]}" : "")} {Columns} FROM {entityProps[0].TABLE_SCHEMA}.{Inst.GetType().Name} as {tableAlias} {CondicionString} {CondSQL} ";

		
            PropertyInfo? primaryKeyPropierty = Inst?.GetType()?.GetProperties()?.ToList()?.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).FirstOrDefault();
            
            var filterOrders = ((List<FilterData>?)filterData?.GetValue(Inst))?.Where(f =>
					f.FilterType?.ToLower().Contains("asc") == true
					 || f.FilterType?.ToLower().Contains("desc") == true).ToList();
			if (orderBy != null)
			{
				queryString = queryString + $" ORDER BY {orderBy} {(orderDir == null ? "ASC" : "DESC")} ";
			}
			else if (orderBy == null && filterOrders != null && filterOrders.Count != 0)
			{
				queryString = queryString + $" Order by {String.Join(", ", filterOrders.Select(o => $" {o.PropName} {o.FilterType} "))}";
			}
			else if (orderBy == null && primaryKeyPropierty != null)
			{
				queryString = queryString + " ORDER BY " + primaryKeyPropierty.Name + " DESC";
			}
            string queryStringCount = $" SELECT count(*) FROM {entityProps[0].TABLE_SCHEMA}.{Inst?.GetType().Name} as {tableAlias} {CondicionString} {CondSQL};";

            return (queryString, queryStringCount);
        }
        public override string BuildSetsForUpdate(string Values, string AtributeName,
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
        public override (string queryResults, string queryCount) BuildSelectQueryPaginated(EntityClass Inst, string CondSQL, int pageNum, int pageSize, string orderBy, string orderDir, bool fullEntity = true, bool isFind = false)
        {
            (string queryString, string queryCount) = BuildSelectQuery(Inst, CondSQL, fullEntity, isFind, orderBy, orderDir);
            // paginación
            queryString = queryString + " OFFSET " + (pageNum - 1) * pageSize + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY";
            return (queryString, queryCount);
        }

        /*Este método CreateParameter es la implementaciion de su abstracto en sql server y crear un parámetro IDbDataParameter para su uso en consultas
         SQL con SQL Server.
        
        este método toma un nombre, un valor, un tipo de datos y una propiedad de una entidad, y crea un parámetro SqlParameter configurado correctamente 
        para su uso en consultas SQL parametrizadas con SQL Server. Si la propiedad tiene un atributo JsonProp, el valor se trata como JSON; de lo contrario,
        se asigna directamente al parámetro.*/
        public override IDbDataParameter CreateParameter(string name, object value, string dataType, PropertyInfo oProperty)
        {
            // Determinar el tipo de datos SQL correspondiente al tipo de datos proporcionado
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
                    // Lanzar una excepción si el tipo de datos no es compatible
                    throw new ArgumentException($"Tipo de datos no soportado: {dataType}");
            }

            // Verificar si la propiedad tiene el atributo JsonProp
            JsonProp? jsonPropAttribute = (JsonProp?)Attribute.GetCustomAttribute(oProperty, typeof(JsonProp));
            if (jsonPropAttribute != null)
            {
                // Tratar el valor como JSON si la propiedad tiene el atributo JsonProp
                string jsonValue = JsonConvert.SerializeObject(value);
                return new SqlParameter(name, sqlDbType) { Value = JValue.Parse(jsonValue).ToString(Formatting.Indented) };
            }
            else
            {
                // Crear un parámetro normal si la propiedad no tiene el atributo JsonProp
                return new SqlParameter(name, sqlDbType) { Value = value };
            }
        }

        protected override SqlEnumType GetSqlType()
        {
            return SqlEnumType.SQL_SERVER;
        }
    }
}