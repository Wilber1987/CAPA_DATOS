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
    public abstract class BDQueryBuilderAbstract
    {
        public abstract (string queryResults, string queryCount) BuildSelectQuery(EntityClass Inst, string CondSQL,
            bool fullEntity = true, bool isFind = true, string? orderBy = null, string? orderDir = null);

        public abstract (string queryResults, string queryCount) BuildSelectQueryPaginated(EntityClass Inst, string CondSQL,
            int pageNum, int pageSize, string orderBy, string orderDir, bool fullEntity = true, bool isFind = true);

        public abstract (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string IdObject);
        public abstract (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string[] WhereProps);
        public abstract string BuildDeleteQuery(EntityClass Inst);


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
        [Obsolete("metodo no se esta utilizando")]
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
        /*Este método WhereConstruction se encarga de construir dinámicamente las condiciones de una cláusula
        WHERE en una consulta SQL, basándose en el nombre y el valor de una propiedad del objeto EntityClass.
        este método es responsable de construir las condiciones de una cláusula WHERE de una consulta SQL, teniendo en 
        cuenta el tipo de valor de la propiedad y agregándolas a la cadena de condiciones CondicionString.*/
        protected void WhereConstruction(ref string CondicionString, ref int index, string AtributeName, object AtributeValue)
        {
            if (AtributeValue != null)
            {
                // Verifica si el valor es una cadena corta y construye una condición LIKE para buscar coincidencias parciales
                if (AtributeValue?.GetType() == typeof(string) && AtributeValue?.ToString()?.Length < 200)
                {
                    WhereOrAnd(ref CondicionString); // Agrega AND u OR si es necesario
                    CondicionString = CondicionString + AtributeName + " LIKE '%" + AtributeValue.ToString() + "%' ";
                }
                // Verifica si el valor es del tipo DateTime y construye una condición para comparar fechas
                else if (AtributeValue?.GetType() == typeof(DateTime))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName
                        + "= '" + ((DateTime)AtributeValue).ToString("yyyy/MM/dd") + "' ";
                }
                // Verifica si el valor es del tipo int o int? y construye una condición de igualdad
                else if (AtributeValue?.GetType() == typeof(int) || AtributeValue?.GetType() == typeof(int?))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + "=" + AtributeValue?.ToString() + " ";
                }
                // Verifica si el valor es del tipo Double y lo convierte en formato float para la comparación
                else if (AtributeValue?.GetType() == typeof(Double))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as float)  ";
                }
                // Verifica si el valor es del tipo Decimal y lo convierte en formato decimal para la comparación
                else if (AtributeValue?.GetType() == typeof(Decimal))
                {
                    WhereOrAnd(ref CondicionString);
                    CondicionString = CondicionString + AtributeName + "= cast('" + AtributeValue?.ToString()?.Replace(",", ".") + "' as decimal)  ";
                }
            }
        }
        /*Este método construye una condición SQL basada en el filtro proporcionado y el tipo de datos de la propiedad.
        Los comentarios explican cada sección del código y su propósito.*/
        protected string SetFilterValueCondition(PropertyInfo[] props, FilterData filter)
        {
            string CondicionString = ""; // String donde se construirá la condición SQL
            PropertyInfo? prop = props.ToList().Find(p => p.Name.Equals(filter?.PropName)); // Obtiene la propiedad correspondiente al nombre proporcionado en el filtro
            string? atributeType = ""; // Tipo de datos de la propiedad
            string AtributeName = ""; // Nombre de la propiedad

            // Verifica si la propiedad existe
            if (prop != null)
            {
                AtributeName = prop.Name; // Obtiene el nombre de la propiedad
                var propertyType = Nullable.GetUnderlyingType(prop?.PropertyType) ?? prop?.PropertyType; // Obtiene el tipo de datos de la propiedad
                atributeType = propertyType?.Name; // Obtiene el nombre del tipo de datos
            }

            // Evalúa el tipo de filtro
            switch (filter.FilterType?.ToUpper())
            {
                case "AND":
                case "OR":
                    // Si el filtro es AND u OR y tiene subfiltros, construye una condición compuesta recursivamente
                    if (filter.Filters != null && filter.Filters.Count != 0)
                    {
                        CondicionString += $" ({string.Join($" {filter.FilterType} ", filter.Filters.Select(f => SetFilterValueCondition(props, f)))})";
                    }
                    break;
                case "BETWEEN":
                    // Si el filtro es BETWEEN, construye una condición para un rango de valores
                    if (filter?.Values?.Count > 0)
                    {
                        if (atributeType == "DateTime")
                        {
                            // Para valores DateTime, construye la condición con conversiones adecuadas
                            CondicionString = CondicionString + " ( " +
                                (filter?.Values?[0] != null ? AtributeName + "  >= " + "CONVERT(DATETIME,'" + Convert.ToDateTime(filter.Values[0]).ToString("yyyyMMdd HH:mm:ss") + "')" + "  " : " ") +
                                (filter?.Values?.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                                (filter?.Values?.Count > 1 ? AtributeName + " <= " + "CONVERT(DATETIME,'" + Convert.ToDateTime(filter.Values[1]).ToString("yyyyMMdd HH:mm:ss") + "')" + " ) " : ") ");
                        }
                        else if (atributeType == "Int32" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "int")
                        {
                            // Para otros tipos numéricos, construye la condición directamente
                            CondicionString = CondicionString + " ( " +
                                (filter?.Values?[0] != null ? AtributeName + "  >= " + filter.Values[0] + "  " : " ") +
                                (filter?.Values?.Count > 1 && filter.Values[0] != null ? " AND " : " ") +
                                (filter?.Values?.Count > 1 ? AtributeName + " <= " + filter.Values[1] + " ) " : ") ");
                        }
                    }
                    break;
                case "IN":
                case "NOT IN":
                    // Si el filtro es IN o NOT IN, construye una condición para comparar con una lista de valores
                    if (filter?.Values?.Count > 0)
                    {
                        CondicionString = CondicionString + AtributeName + $" {filter?.FilterType} (" + BuildArrayIN(filter?.Values, atributeType) + ") ";
                    }
                    break;
                case "LIKE":
                    // Si el filtro es LIKE, construye una condición para buscar coincidencias parciales
                    if (filter?.Values?.Count > 0)
                    {
                        CondicionString = CondicionString + AtributeName + " LIKE '%" + filter?.Values?[0] + "%' ";
                    }
                    break;
                case "LIMIT":
                case "PAGINATE":
                case "ASC":
                case "DESC":
                    // Estos tipos de filtro no requieren construcción de condición
                    break;
                default:
                    // Para otros tipos de filtro, construye una condición de comparación simple
                    if (filter?.Values?.Count > 0)
                    {
                        if (atributeType == "int" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "int")
                            CondicionString += $" {AtributeName} {filter?.FilterType} {filter?.Values?[0]} ";
                        else
                            CondicionString += $" {AtributeName} {filter.FilterType} '{filter?.Values?[0]}' ";
                    }
                    break;
            }

            return CondicionString; // Devuelve la condición construida
        }
        //TODO VERIFICAR LUEGO
        protected (string condition, List<IDbDataParameter> parameters) SetFilterValueConditionWithParameters(PropertyInfo[] props, FilterData filter)
        {
            string condition = ""; // String donde se construirá la condición SQL
            List<IDbDataParameter> parameters = new List<IDbDataParameter>(); // Lista de parámetros SQL

            PropertyInfo? prop = props.ToList().Find(p => p.Name.Equals(filter?.PropName)); // Obtiene la propiedad correspondiente al nombre proporcionado en el filtro
            string? attributeType = ""; // Tipo de datos de la propiedad
            string attributeName = ""; // Nombre de la propiedad

            // Verifica si la propiedad existe
            if (prop != null)
            {
                attributeName = prop.Name; // Obtiene el nombre de la propiedad
                var propertyType = Nullable.GetUnderlyingType(prop?.PropertyType) ?? prop?.PropertyType; // Obtiene el tipo de datos de la propiedad
                attributeType = propertyType?.Name; // Obtiene el nombre del tipo de datos
            }

            // Evalúa el tipo de filtro
            switch (filter.FilterType?.ToUpper())
            {
                case "AND":
                case "OR":
                    // Si el filtro es AND u OR y tiene subfiltros, construye una condición compuesta recursivamente
                    if (filter.Filters != null && filter.Filters.Count != 0)
                    {
                        condition += $" ({string.Join($" {filter.FilterType} ", filter.Filters.Select(f => SetFilterValueCondition(props, f)))})";
                    }
                    break;
                case "BETWEEN":
                    // Si el filtro es BETWEEN, construye una condición para un rango de valores
                    if (filter?.Values?.Count == 2)
                    {
                        condition += $" {attributeName} BETWEEN @param{parameters.Count} AND @param{parameters.Count + 1}";
                        parameters.Add(CreateParameter($"@param{parameters.Count}", filter.Values[0], attributeType, prop));
                        parameters.Add(CreateParameter($"@param{parameters.Count}", filter.Values[1], attributeType, prop));
                    }
                    break;
                case "IN":
                case "NOT IN":
                    // Si el filtro es IN o NOT IN, construye una condición para comparar con una lista de valores
                    if (filter?.Values?.Count > 0)
                    {
                        condition += $" {attributeName} {filter.FilterType} ({BuildArrayIN(filter?.Values, attributeType)})";
                    }
                    break;
                case "LIKE":
                    // Si el filtro es LIKE, construye una condición para buscar coincidencias parciales
                    if (filter?.Values?.Count > 0)
                    {
                        condition += $" {attributeName} LIKE @param{parameters.Count}";
                        parameters.Add(CreateParameter($"@param{parameters.Count}", $"%{filter.Values[0]}%", "nvarchar", prop));
                    }
                    break;
                case "LIMIT":
                case "PAGINATE":
                case "ASC":
                case "DESC":
                    // Estos tipos de filtro no requieren construcción de condición
                    break;
                default:
                    // Para otros tipos de filtro, construye una condición de comparación simple
                    if (filter?.Values?.Count > 0)
                    {
                        condition += $" {attributeName} {filter.FilterType} @param{parameters.Count}";
                        parameters.Add(CreateParameter($"@param{parameters.Count}", filter.Values[0], attributeType, prop));
                    }
                    break;
            }

            return (condition, parameters); // Devuelve la condición construida y la lista de parámetros SQL
        }


        protected void WhereOrAnd(ref string CondicionString)
        {
            if (!CondicionString.Contains("WHERE"))
                CondicionString = " WHERE ";
            else
                CondicionString += " AND ";
        }
        /*Este método recorre una lista de valores y los agrega a una cadena separados por comas. Si los valores son numéricos, no se agregan comillas
         alrededor de ellos; de lo contrario, se agregan comillas simples. La función luego elimina la coma final antes de devolver la cadena construida.*/
        public static string BuildArrayIN(List<string?> conditions, string atributeType = "string")
        {
            string CondicionString = ""; // Cadena donde se construirán los valores para la cláusula IN
            foreach (string? Value in conditions)
            {
                // Verifica el tipo de datos de la propiedad para formatear adecuadamente los valores
                if (atributeType == "int" || atributeType == "Double" || atributeType == "Decimal" || atributeType == "Int32" || atributeType == "Int16")
                    CondicionString = CondicionString + Value?.ToString() + ","; // Agrega el valor a la cadena
                else
                    CondicionString = CondicionString + "'" + Value + "',"; // Agrega el valor entre comillas simples a la cadena
            }
            CondicionString = CondicionString.TrimEnd(','); // Elimina la última coma de la cadena
            return CondicionString; // Devuelve la cadena de valores
        }

        //DATA SQUEMA
        #endregion
    }
}