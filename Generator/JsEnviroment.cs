using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAPA_DATOS;

namespace AppGenerator
{
    internal class JsEnviroment
    {
        public static void setJsHeaders(out StringBuilder entityString)
        {
            entityString = new StringBuilder();
            entityString.AppendLine("import { EntityClass } from \"../../WDevCore/WModules/EntityClass.js\";");
            entityString.AppendLine("import { WAjaxTools, BasicStates } from \"../../WDevCore/WModules/WComponentsTools.js\";");
            entityString.AppendLine("import { ModelProperty } from \"../../WDevCore/WModules/CommonModel.js\";");
        }
        public static void mapJsModel(StringBuilder entityString, EntitySchema table, string schema, string typeshema)
        {
            entityString.AppendLine("class " + table.TABLE_NAME + "_ModelComponent extends EntityClass {");
            entityString.AppendLine("   constructor(props) {");
            entityString.AppendLine("       super(props, '" + (typeshema == "VIEW" ? "View" : "Entity") + Utility.capitalize(schema) + "');");
            entityString.AppendLine("       for (const prop in props) {");
            entityString.AppendLine("           this[prop] = props[prop];");
            entityString.AppendLine("       }");
            entityString.AppendLine("   }");
            //entityString.AppendLine("   Namespace = '" + (typeshema == "VIEW" ? "View" : "Entity") + Utility.capitalize(schema) + "';");
            foreach (var entity in SqlADOConexion.SQLM.describeEntity($"{table.TABLE_NAME}"))
            {
                string type = "";
                switch (entity.DATA_TYPE)
                {
                    case "int": type = "number"; break;
                    case "smallint": type = "number"; break;
                    case "bigint": type = "number"; break;
                    case "decimal": type = "number"; break;
                    case "money": type = "number"; break;
                    case "float": type = "number"; break;
                    case "char": type = "text"; break;
                    case "nchar": type = "text"; break;
                    case "varchar": type = "text"; break;
                    case "nvarchar": type = "text"; break;
                    case "uniqueidentifier": type = "text"; break;
                    case "datetime": case "datetime2": type = "date"; break;
                    case "date": type = "date"; break;
                    case "bit": case "binary": type = "checkbox"; break;
                }
                if (!SqlADOConexion.SQLM.isForeinKey(table.TABLE_NAME, entity.COLUMN_NAME))
                {
                    entityString.AppendLine("   /**@type {ModelProperty}*/ " + entity.COLUMN_NAME + " = { type: '" + type + "'"
                    + (SqlADOConexion.SQLM.isPrimary(table.TABLE_NAME, entity.COLUMN_NAME) ? ", primary: true" : "") + " };");
                }

            }
            foreach (var entity in SqlADOConexion.SQLM.ManyToOneKeys($"{table.TABLE_SCHEMA}.{table.TABLE_NAME}"))
            {
                var oneToMany = SqlADOConexion.SQLM.oneToManyKeys($"{table.TABLE_NAME}", $"{table.TABLE_SCHEMA}");
                var find = oneToMany.Find(o => o.FKTABLE_NAME == table.TABLE_NAME);
                string controlType = "WSELECT";
                entityString.AppendLine("   /**@type {ModelProperty}*/ " + entity.REFERENCE_TABLE_NAME + " = { type: '" + controlType
                   + "',  ModelObject: ()=> new " + entity.REFERENCE_TABLE_NAME + "_ModelComponent()};");
                continue;

            }
            foreach (var entity in SqlADOConexion.SQLM.oneToManyKeys($"{table.TABLE_NAME}", $"{table.TABLE_SCHEMA}"))
            {
                string mapType = "MasterDetail";
                if ((entity.FKTABLE_NAME.ToLower().StartsWith("catalogo") || (table.TABLE_NAME.ToLower().StartsWith("transaction")
                    && entity.FKTABLE_NAME.ToLower().ToLower().StartsWith("transaction"))))
                {
                    mapType = "MULTYSELECT";
                }
                if (entity.FKTABLE_NAME.ToLower().StartsWith("detail") && table.TABLE_NAME.ToLower().StartsWith("detail"))
                {
                    mapType = "WSELECT";
                }
                if ((!table.TABLE_NAME.ToLower().StartsWith("catalogo")
                    || entity.FKTABLE_NAME.ToLower().StartsWith("relational"))
                    & !entity.FKTABLE_NAME.ToLower().StartsWith("transaction"))
                {
                    entityString.AppendLine("   /**@type {ModelProperty}*/ " + entity.FKTABLE_NAME +
                     " = { type: '" + mapType + "',  ModelObject: ()=> new " + entity.FKTABLE_NAME + "_ModelComponent()};");
                }
            }
            entityString.AppendLine("}");
            entityString.AppendLine("export { " + table.TABLE_NAME + "_ModelComponent }");
        }
        public static void mapJsEntity(StringBuilder entityString, EntitySchema table, string schema, string typeshema)
        {
            entityString.AppendLine("class " + table.TABLE_NAME + " extends EntityClass {");
            entityString.AppendLine("   constructor(props) {");
            entityString.AppendLine("       super(props, '" + (typeshema == "VIEW" ? "View" : "Entity") + Utility.capitalize(schema) + "');");
            entityString.AppendLine("       for (const prop in props) {");
            entityString.AppendLine("           this[prop] = props[prop];");
            entityString.AppendLine("       }");
            entityString.AppendLine("   }");
            //entityString.AppendLine("   Namespace = '" + (typeshema == "VIEW" ? "View" : "Entity") + Utility.capitalize(schema) + "';");
            foreach (var entity in SqlADOConexion.SQLM.describeEntity($"{table.TABLE_NAME}"))
            {
                string type = "";
                switch (entity.DATA_TYPE)
                {
                    case "int": type = "Number"; break;
                    case "smallint": type = "Number"; break;
                    case "bigint": type = "Number"; break;
                    case "decimal": type = "Number"; break;
                    case "money": type = "Number"; break;
                    case "float": type = "Number"; break;
                    case "char": type = "String"; break;
                    case "nchar": type = "String"; break;
                    case "varchar": type = "String"; break;
                    case "nvarchar": type = "String"; break;
                    case "uniqueidentifier": type = "String"; break;
                    case "datetime": case "datetime2": type = "Date"; break;
                    case "date": type = "Date"; break;
                    case "bit": case "binary": type = "Boolean"; break;
                }
                if (!SqlADOConexion.SQLM.isForeinKey(table.TABLE_NAME, entity.COLUMN_NAME))
                {
                    entityString.AppendLine("   /**@type {" + type + "}*/ " + entity.COLUMN_NAME + ";");
                }

            }
            foreach (var entity in SqlADOConexion.SQLM.ManyToOneKeys($"{table.TABLE_SCHEMA}.{table.TABLE_NAME}"))
            {
                var oneToMany = SqlADOConexion.SQLM.oneToManyKeys($"{table.TABLE_NAME}", $"{table.TABLE_SCHEMA}");
                var find = oneToMany.Find(o => o.FKTABLE_NAME == table.TABLE_NAME);
                entityString.AppendLine("   /**@type {" + entity.REFERENCE_TABLE_NAME + "} ManyToOne*/ " + entity.REFERENCE_TABLE_NAME + ";");
                continue;
            }
            foreach (var entity in SqlADOConexion.SQLM.oneToManyKeys($"{table.TABLE_NAME}", $"{table.TABLE_SCHEMA}"))
            {
                entityString.AppendLine("   /**@type {Array<" + entity.FKTABLE_NAME + ">} OneToMany*/ " + entity.FKTABLE_NAME + ";");
            }
            entityString.AppendLine("}");
            entityString.AppendLine("export { " + table.TABLE_NAME + " }");
        }
        public static void setJsViewBuilder(string schema, string name, string type)
        {

        }
        public static void setJsCatalogoBuilder(string schema, List<string> names)
        {


        }
    }
}
