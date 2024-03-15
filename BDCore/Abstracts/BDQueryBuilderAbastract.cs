using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAPA_DATOS.BDCore.Abstracts
{
    public abstract class BDQueryBuilderAbastract
    {
        public abstract (string queryResults, string queryCount) BuildSelectQuery(EntityClass Inst, string CondSQL,
            bool fullEntity = true, bool isFind = true, string? orderBy = null, string? orderDir = null);

        public abstract (string queryResults, string queryCount) BuildSelectQueryPaginated(EntityClass Inst, string CondSQL,
            int pageNum, int pageSize, string orderBy, string orderDir, bool fullEntity = true, bool isFind = true);

        public abstract (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string IdObject);
        public abstract (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string[] WhereProps);
        public abstract string BuildDeleteQuery(EntityClass Inst);
        public abstract string BuildSetsForUpdate(string Values, string AtributeName,
            object AtributeValue, EntityProps EntityProp, PropertyInfo oProperty);

     
        #region  QUERYBUILDER IMPLEMANTATIONS
        /*Este método BuildInsertQueryByObject se encarga de construir una consulta de inserción SQL basada en los datos proporcionados 
		por una instancia de objeto Inst.
		
		Este método es crucial para la generación de consultas SQL de inserción dinámicas, adaptadas según las propiedades y valores de 
		la instancia de objeto proporcionada. Incorpora la lógica necesaria para manejar diferentes tipos de datos y conversiones apropiadas. 
		Además, garantiza que la consulta sea segura y funcional para insertar datos en la base de datos correspondiente.*/
        [Obsolete("metodo no se esta utilizando")]
        protected string? BuildInsertQueryByObject(EntityClass Inst)
        {
            // Variables para almacenar los nombres de columnas y los valores correspondientes en la consulta INSERT
            string ColumnNames = "";
            string Values = "";

            // Obtiene el tipo de la instancia del objeto
            Type _type = Inst.GetType();

            // Obtiene todas las propiedades del objeto
            PropertyInfo[] lst = _type.GetProperties();

            // Describe las propiedades de la entidad utilizando el nombre del tipo del objeto
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());

            // Itera sobre todas las propiedades del objeto
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);

                // Busca la propiedad en la descripción de la entidad
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);

                // Verifica si la propiedad y su valor no son nulos y existen en la descripción de la entidad
                if (AtributeValue != null && EntityProp != null)
                {
                    // Determina cómo se debe tratar el tipo de datos de la propiedad
                    switch (EntityProp.DATA_TYPE)
                    {
                        case "nvarchar":
                        case "varchar":
                        case "char":
                            ColumnNames += AtributeName + ",";
                            JsonProp? json = (JsonProp?)Attribute.GetCustomAttribute(oProperty, typeof(JsonProp));
                            if (json != null)
                            {
                                string jsonV = JsonConvert.SerializeObject(AtributeValue);
                                Values += "'" + JValue.Parse(jsonV).ToString(Formatting.Indented) + "',";
                            }
                            else
                            {
                                Values += "'" + AtributeValue.ToString() + "',";
                            }
                            break;
                        case "int":
                        case "float":
                            ColumnNames += AtributeName + ",";
                            Values += "cast ('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as float),";
                            break;
                        case "decimal":
                            ColumnNames += AtributeName + ",";
                            Values += "cast ('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as decimal),";
                            break;
                        case "bigint":
                        case "money":
                        case "smallint":
                            ColumnNames += AtributeName + ",";
                            Values += AtributeValue.ToString() + ",";
                            break;
                        case "bit":
                            ColumnNames += AtributeName + ",";
                            Values += "'" + (AtributeValue.ToString() == "True" ? "1" : "0") + "',";
                            break;
                        case "datetime":
                        case "date":
                            ColumnNames += AtributeName + ",";
                            Values = BuildSqlDateConverterQuery(Values, AtributeValue);
                            break;
                    }
                }
                else
                {
                    // Si la propiedad o su valor son nulos o no existen en la descripción de la entidad, continúa con la siguiente iteración
                    continue;
                }
            }

            // Elimina la coma adicional al final de las listas de nombres de columnas y valores
            ColumnNames = ColumnNames.TrimEnd(',');
            Values = Values.TrimEnd(',');

            // Si no hay valores para insertar, retorna null
            if (Values == "")
            {
                return null;
            }

            // Construye y retorna la consulta INSERT completa, incluyendo la obtención del identificador insertado (SCOPE_IDENTITY)
            string QUERY = "INSERT INTO " + entityProps[0].TABLE_SCHEMA + "." + Inst.GetType().Name + "(" + ColumnNames + ") VALUES(" + Values + ") SELECT SCOPE_IDENTITY()";

            // Registra un mensaje de información con la consulta construida
            LoggerServices.AddMessageInfo(QUERY);

            return QUERY;
        }

        /*Este método abstracto CreateParameter se utiliza para crear un parámetro IDbDataParameter, que representa un parámetro 
		para una consulta SQL parametrizada. 
		
		Este método debe ser implementado en una clase concreta que herede de la clase que contiene este método abstracto. 
		La implementación específica puede variar dependiendo del proveedor de base de datos utilizado (por ejemplo, SQL Server, MySQL, etc.).
		Aquí está una breve descripción de los parámetros:

			name: El nombre del parámetro.
			value: El valor del parámetro.
			dataType: El tipo de datos de la columna correspondiente en la base de datos.
			oProperty: La propiedad de la entidad que corresponde al parámetro.
		
		La implementación de este método debe crear y retornar un objeto IDbDataParameter configurado adecuadamente con los valores proporcionados.
		Esto generalmente implica crear un objeto del tipo específico de parámetro para el proveedor de base de datos que estás utilizando, 
		establecer su nombre, valor y otros atributos según sea necesario, y luego devolverlo.

		Por ejemplo, si estás trabajando con SQL Server, la implementación de este método podría crear un SqlParameter y configurarlo con el nombre,
		valor y tipo de datos proporcionados. Si estás trabajando con MySQL, la implementación podría crear un MySqlParameter de manera similar.*/
        public abstract IDbDataParameter CreateParameter(string name, object value, string dataType, PropertyInfo oProperty);

        /*Este método BuildInsertQueryByObjectParameters es similar al anterior, pero en lugar de construir la consulta de inserción directamente,
		crea parámetros SQL para ser utilizados con comandos parametrizados.
		
		Este método es esencial para generar consultas de inserción seguras y eficientes, utilizando parámetros SQL para prevenir ataques de inyección SQL.
		Cada propiedad del objeto se mapea a un parámetro SQL correspondiente, lo que garantiza una inserción segura de datos en la base de datos.
		*/
        public (string?, List<IDbDataParameter>?) BuildInsertQueryByObjectParameters(EntityClass Inst)
        {
            // Variables para almacenar los nombres de columnas y los valores correspondientes en la consulta INSERT
            string ColumnNames = "";
            string Values = "";

            // Lista para almacenar los parámetros SQL
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();

            // Obtiene el tipo de la instancia del objeto
            Type _type = Inst.GetType();

            // Obtiene todas las propiedades del objeto
            PropertyInfo[] lst = _type.GetProperties();

            // Describe las propiedades de la entidad utilizando el nombre del tipo del objeto
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());

            // Itera sobre todas las propiedades del objeto
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);

                // Busca la propiedad en la descripción de la entidad
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);

                // Verifica si la propiedad y su valor no son nulos y existen en la descripción de la entidad
                if (AtributeValue != null && EntityProp != null)
                {
                    string paramName = "@" + AtributeName;
                    ColumnNames += AtributeName + ",";
                    Values += paramName + ",";

                    // Crea un parámetro SQL correspondiente a la propiedad
                    IDbDataParameter parameter = CreateParameter(paramName, AtributeValue, EntityProp.DATA_TYPE, oProperty);
                    parameters.Add(parameter);
                }
                else
                {
                    // Si la propiedad o su valor son nulos o no existen en la descripción de la entidad, continúa con la siguiente iteración
                    continue;
                }
            }

            // Elimina la coma adicional al final de las listas de nombres de columnas y valores
            ColumnNames = ColumnNames.TrimEnd(',');
            Values = Values.TrimEnd(',');

            // Si no hay valores para insertar, retorna null
            if (Values == "")
            {
                return (null, null);
            }

            // Construye la consulta INSERT completa, incluyendo la obtención del identificador insertado (SCOPE_IDENTITY)
            string QUERY = $"INSERT INTO {entityProps[0].TABLE_SCHEMA}.{Inst.GetType().Name} ({ColumnNames}) VALUES({Values}) SELECT SCOPE_IDENTITY()";

            // Registra un mensaje de información con la consulta construida
            LoggerServices.AddMessageInfo(QUERY);

            // Retorna la consulta INSERT y los parámetros SQL creados
            return (QUERY, parameters);
        }

        protected abstract SqlEnumType GetSqlType();

        /*BuildSqlDateConverterQuery debe ser reescrita segun la implementacion
del motor de base de datos, este ejemplo es para sql server*/
        private static string BuildSqlDateConverterQuery(string Values, object AtributeValue)
        {
            return Values + "CONVERT(DATETIME,'" + ((DateTime)AtributeValue).ToString("yyyyMMdd HH:mm:ss") + "'),";
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
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + " LIKE '%" + AtributeValue.ToString() + "%' ";
                }
                else if (AtributeValue?.GetType() == typeof(DateTime))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName
                        + "= '" + ((DateTime)AtributeValue).ToString("yyyy/MM/dd") + "' ";
                }
                else if (AtributeValue?.GetType() == typeof(int) || AtributeValue?.GetType() == typeof(int?))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + "=" + AtributeValue?.ToString() + " ";
                }
                else if (AtributeValue?.GetType() == typeof(Double))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as float)  ";
                }
                else if (AtributeValue?.GetType() == typeof(Decimal))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as decimal)  ";
                }
            }
        }

        protected string SetFilterValueCondition(PropertyInfo[] props, FilterData filter)
        {
            string CondicionString = "";
            PropertyInfo? prop = props.ToList().Find(p => p.Name.Equals(filter?.PropName));
            string? atributeType = "";
            string AtributeName = "";
            if (prop != null)
            {
                AtributeName = prop.Name;
                var propertyType = Nullable.GetUnderlyingType(prop?.PropertyType) ?? prop?.PropertyType;
                atributeType = propertyType?.Name;
            }
            switch (filter.FilterType?.ToUpper())
            {
                case "AND":
                case "OR":
                    if (filter.Filters != null && filter.Filters.Count != 0)
                    {
                        CondicionString += $" ({string.Join($" {filter.FilterType} ", filter.Filters.Select(f => SetFilterValueCondition(props, f)))})";
                    }
                    break;
                case "BETWEEN":
                    if (filter?.Values?.Count > 0)
                    {
                        // WhereOrAnd(ref CondicionString);
                        if (atributeType == "DateTime")
                        {
                            CondicionString = CondicionString + " ( " +
                               (filter?.Values?[0] != null ? AtributeName + "  >= " + "CONVERT(DATETIME,'" + Convert.ToDateTime(filter.Values[0]).ToString("yyyyMMdd HH:mm:ss") + "')" + "  " : " ") +
                               (filter?.Values?.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                               (filter?.Values?.Count > 1 ? AtributeName + " <= " + "CONVERT(DATETIME,'" + Convert.ToDateTime(filter.Values[1]).ToString("yyyyMMdd HH:mm:ss") + "')" + " ) " : ") ");
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
                    if (filter?.Values?.Count > 0)
                    {
                        CondicionString = CondicionString + AtributeName + " LIKE '%" + filter?.Values?[0] + "%' ";
                    }
                    break;
                case "LIMIT":
                case "PAGINATE":
                case "ASC":
                case "DESC":
                    break;
                default:
                    if (filter?.Values?.Count > 0)
                    {
                        if (atributeType == "int" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "int")
                            CondicionString += $" {AtributeName} {filter?.FilterType} {filter?.Values?[0]} ";
                        else
                            CondicionString += $" {AtributeName} {filter.FilterType} '{filter?.Values?[0]}' ";
                    }
                    break;
            }

            return CondicionString;
        }


        protected void WhereOrAnd(ref string CondicionString)
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
                if (atributeType == "int" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "Int32" || atributeType == "Int16")
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