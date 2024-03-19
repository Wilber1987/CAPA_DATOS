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
    public class SQLServerQueryBuilder : BDQueryBuilderAbstract
    {
        public override (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string IdObject)
        {
            return BuildUpdateQueryByObject(Inst, new string[] { IdObject });
        }
        /*Este método BuildUpdateQueryByObject construye una consulta de actualización SQL basada en un objeto de clase 
        de entidad y un conjunto de propiedades de "donde" (condiciones de actualización). este método recorre las propiedades 
        del objeto de la clase de entidad y construye una consulta de actualización SQL basada en las propiedades proporcionadas
        y las condiciones de actualización. Luego, devuelve la consulta de actualización junto con los parámetros 
        IDbDataParameter necesarios para la ejecución de la consulta.*/
        public override (string?, List<IDbDataParameter>?) BuildUpdateQueryByObject(EntityClass Inst, string[] WhereProps)
        {
            // Nombre de la tabla basado en el tipo de clase de entidad
            string TableName = Inst.GetType().Name;

            // Cadena para almacenar los valores a actualizar
            string Values = "";

            // Cadena para almacenar las condiciones de actualización
            string Conditions = "";

            // Obtener el tipo y las propiedades del objeto de clase de entidad
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();

            // Describir las propiedades de la entidad
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());

            // Contador de índice para parámetros de consulta
            int index = 0;

            // Lista para almacenar parámetros IDbDataParameter
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();

            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var AtributeValue = oProperty.GetValue(Inst);

                // Buscar la propiedad correspondiente en las propiedades de la entidad
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);

                if (AtributeValue != null && EntityProp != null)
                {
                    // Si la propiedad no está en el conjunto de propiedades de "donde",
                    // se agrega a los valores a actualizar y se crea un parámetro
                    // IDbDataParameter correspondiente
                    if ((from O in WhereProps where O == AtributeName select O).ToList().Count == 0)
                    {
                        string paramName = "@" + AtributeName;
                        Values = Values + $"{AtributeName} = {paramName},";
                        IDbDataParameter parameter = CreateParameter(paramName, AtributeValue, EntityProp.DATA_TYPE, oProperty);
                        parameters.Add(parameter);
                    }
                    else
                    {
                        // Si la propiedad está en el conjunto de propiedades de "donde",
                        // se construyen las condiciones de actualización
                        WhereConstruction(ref Conditions, ref index, AtributeName, AtributeValue);
                    }
                }
                else continue;
            }

            // Eliminar la coma final en los valores
            Values = Values.TrimEnd(',');

            // Si no hay valores para actualizar, retorna null
            if (Values == "")
            {
                return (null, null);
            }

            // Construir la consulta de actualización
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
       
       /*Este método BuildSelectQuery se utiliza para construir consultas SELECT basadas en un objeto de clase de entidad, condiciones SQL adicionales y 
        opciones de filtrado. Este método es crucial para construir consultas SELECT complejas basadas en el estado actual del objeto EntityClass, sus 
        propiedades y filtros adicionales proporcionados. Permite una construcción dinámica de consultas que pueden adaptarse a una variedad de escenarios
         de recuperación de datos.*/
        public override (string queryResults, string queryCount) BuildSelectQuery(EntityClass Inst, string CondSQL,
          bool fullEntity = true, bool isFind = false, string? orderBy = null, string? orderDir = null)
        {
            // Inicialización de variables para la construcción de la consulta
            string CondicionString = "";
            string Columns = "";

            // Obtener el tipo y las propiedades del objeto de clase de entidad
            Type _type = Inst.GetType();
            PropertyInfo[] lst = _type.GetProperties();

            // Describir las propiedades de la entidad
            List<EntityProps> entityProps = Inst.DescribeEntity(GetSqlType());

            // Índice para parámetros de consulta
            int index = 0;

            // Generar un alias para la tabla
            string tableAlias = tableAliaGenerator();

            // Obtener la propiedad "filterData" del objeto de clase de entidad
            var filterData = Inst.GetType().GetProperty("filterData");

            // Iterar sobre las propiedades del objeto de clase de entidad
            foreach (PropertyInfo oProperty in lst)
            {
                string AtributeName = oProperty.Name;
                var EntityProp = entityProps.Find(e => e.COLUMN_NAME == AtributeName);

                // Obtener atributos específicos de relación entre entidades
                var oneToOne = (OneToOne?)Attribute.GetCustomAttribute(oProperty, typeof(OneToOne));
                var manyToOne = (ManyToOne?)Attribute.GetCustomAttribute(oProperty, typeof(ManyToOne));
                var oneToMany = (OneToMany?)Attribute.GetCustomAttribute(oProperty, typeof(OneToMany));

                // Si la propiedad pertenece a la entidad
                if (EntityProp != null)
                {
                    // Agregar el nombre de la columna a las columnas seleccionadas
                    Columns = Columns + AtributeName + ",";

                    // Construir condiciones de consulta basadas en el valor de la propiedad
                    var AtributeValue = oProperty.GetValue(Inst);
                    if (AtributeValue != null)
                    {
                        WhereConstruction(ref CondicionString, ref index, AtributeName, AtributeValue);
                    }
                }
                // Si la propiedad es una relación "ManyToOne" y se requiere la entidad completa
                else if (manyToOne != null && fullEntity)
                {
                    // Construir subconsulta JSON para la relación "ManyToOne"
                    var manyToOneInstance = Activator.CreateInstance(oProperty.PropertyType);
                    string condition = " " + manyToOne.KeyColumn + " = " + tableAlias + "." + manyToOne.ForeignKeyColumn;
                    (string subquery, _) = BuildSelectQuery((EntityClass?)manyToOneInstance, condition, false);
                    Columns = Columns + AtributeName
                        + $" = JSON_QUERY(({subquery} FOR JSON PATH,  ROOT('object')),'$.object[0]'),";
                }
                // Si la propiedad es una relación "OneToOne" y se requiere la entidad completa
                else if (oneToOne != null && fullEntity)
                {
                    // Construir subconsulta JSON para la relación "OneToOne"
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
                // Si la propiedad es una relación "OneToMany" y se requiere la entidad completa
                else if (oneToMany != null && fullEntity)
                {
                    // Construir subconsulta para la relación "OneToMany"
                    var oneToManyInstance = Activator.CreateInstance(oProperty.PropertyType.GetGenericArguments()[0]);
                    string condition = " " + oneToMany.ForeignKeyColumn + " = " + tableAlias + "." + oneToMany.KeyColumn;
                    (string subquery, _) = BuildSelectQuery((EntityClass?)oneToManyInstance, condition, oneToMany.TableName != Inst.GetType().Name);
                    Columns = Columns + AtributeName
                        + $" = ({subquery} FOR JSON PATH),";
                }
            }

            // Construir condiciones de consulta basadas en el filtro de datos
            if (filterData != null && filterData.GetValue(Inst) != null)
            {
                foreach (FilterData filter in (List<FilterData>?)filterData.GetValue(Inst) ?? new List<FilterData>())
                {
                    // Construir condiciones de consulta basadas en el filtro de datos
                    string filterCond = SetFilterValueCondition(lst, filter);
                    if (filterCond.Length != 0)
                    {
                        WhereOrAnd(ref CondicionString);
                        CondicionString += filterCond;
                    }
                }
            }

            // Eliminar caracteres innecesarios del final de la cadena de condiciones
            CondicionString = CondicionString.TrimEnd(new char[] { '0', 'R' });

            // Ajustar la cadena de condiciones según lo especificado
            if (CondicionString == "" && CondSQL != "")
            {
                CondicionString = " WHERE ";
            }
            else if (CondicionString != "" && CondSQL != "")
            {
                CondicionString = CondicionString + " AND ";
            }

            // Eliminar la coma final de la lista de columnas
            Columns = Columns.TrimEnd(',');

            // Obtener la propiedad de límite de filtro
            FilterData? filterLimit = ((List<FilterData>?)filterData?.GetValue(Inst))?.Find(f =>
                    f.FilterType?.ToLower().Contains("limit") == true);

            // Construir la consulta SELECT principal
            string queryString = $"SELECT {(filterLimit != null ? $" top {filterLimit?.Values?[0]}" : "")} {Columns} FROM {entityProps[0].TABLE_SCHEMA}.{Inst.GetType().Name} as {tableAlias} {CondicionString} {CondSQL} ";

            // Obtener la propiedad de clave principal
            PropertyInfo? primaryKeyPropierty = Inst?.GetType()?.GetProperties()?.ToList()?.Where(p => Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).FirstOrDefault();

            // Obtener las órdenes de filtro
            var filterOrders = ((List<FilterData>?)filterData?.GetValue(Inst))?.Where(f =>
                    f.FilterType?.ToLower().Contains("asc") == true
                     || f.FilterType?.ToLower().Contains("desc") == true).ToList();

            // Construir la cláusula ORDER BY según sea necesario
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

            // Construir la consulta COUNT para obtener el total de registros
            string queryStringCount = $" SELECT count(*) FROM {entityProps[0].TABLE_SCHEMA}.{Inst?.GetType().Name} as {tableAlias} {CondicionString} {CondSQL};";

            // Devolver la consulta principal y la consulta COUNT
            return (queryString, queryStringCount);
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