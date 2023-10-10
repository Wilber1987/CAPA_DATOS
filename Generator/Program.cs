using System;
using System.IO;
using System.Text;
using CAPA_DATOS;

namespace AppGenerate
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                SqlADOConexion.IniciarConexionAnonima();
                StringBuilder indexBuilder = AppGenerator.CSharpEnviroment.CSharpIndexBuilder();
                foreach (var schema in SqlADOConexion.SQLM.databaseSchemas())
                {
                    foreach (var schemaType in SqlADOConexion.SQLM.databaseTypes())
                    {
                        StringBuilder entityString, controllerString, jsEntityString, jsEntityComponentString;
                        AppGenerator.CSharpEnviroment.setCSharpHeaders(out entityString, out controllerString, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        AppGenerator.JsEnviroment.setJsHeaders(out jsEntityString);
                        AppGenerator.JsEnviroment.setJsHeaders(out jsEntityComponentString);
                        var describeSchema = SqlADOConexion.SQLM.describeSchema(schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        foreach (var table in describeSchema)
                        {
                            if (table.TABLE_NAME == "sysdiagrams")
                            {
                                continue;
                            }
                            //BUILD ENTITY
                            AppGenerator.CSharpEnviroment.mapCSharpEntity(entityString, table);

                            AppGenerator.JsEnviroment.mapJsEntity(jsEntityString, table, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                            AppGenerator.JsEnviroment.mapJsModel(jsEntityComponentString, table, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);

                            //BUILD ENTITY CONTROLLER
                            AppGenerator.CSharpEnviroment.buildApiController(schemaType, controllerString, table);
                            if (!table.TABLE_NAME.ToLower().StartsWith("catalogo"))
                            {
                                AppGenerator.JsEnviroment.setJsViewBuilder(schema.TABLE_SCHEMA, table.TABLE_NAME, schemaType.TABLE_TYPE);
                            }
                            AppGenerator.CSharpEnviroment.CSharpIndexBuilder(indexBuilder, table);

                        }
                        entityString.AppendLine("}");
                        controllerString.AppendLine("   }");
                        controllerString.AppendLine("}");
                        AppGenerator.JsEnviroment.setJsCatalogoBuilder(schema.TABLE_SCHEMA,
                            describeSchema.Where(t => t.TABLE_NAME.ToLower().StartsWith("catalogo")).Select(s => s.TABLE_NAME).ToList());
                        createDataBaseModelFile(entityString.ToString(), schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        createDataBaseJSModelFile(jsEntityString.ToString(), schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        createDataBaseJSModelCompFile(jsEntityComponentString.ToString(), schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        createApiControllerFile(controllerString.ToString(), schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                    }
                }
                AppGenerator.Utility.createFile(@"../AppGenerateFiles/Controllers\SecurityController.cs", AppGenerator.CSharpEnviroment.buildApiSecurityController());
                AppGenerator.Utility.createFile(@"../AppGenerateFiles/Security\AuthNetcore.cs", AppGenerator.CSharpEnviroment.body);
                AppGenerator.Utility.createFile(@"../AppGenerateFiles/Pages\LoginView.cshtml", AppGenerator.CSharpEnviroment.loginString);
                indexBuilder.Append(AppGenerator.CSharpEnviroment.transactionalMenu);
                indexBuilder.Append(AppGenerator.CSharpEnviroment.catalogoMenu);
                AppGenerator.Utility.createFile(@"../AppGenerateFiles/Pages\Index.cshtml", indexBuilder.ToString());
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void createDataBaseModelFile(string contain, string name, string type)
        {
            AppGenerator.Utility.createFile(@"../AppGenerateFiles/Model\" + AppGenerator.Utility.capitalize(name) + (type == "VIEW" ? "ViewModel.cs" : "DataBaseModel.cs"), contain);

        }

        public static void createDataBaseJSModelFile(string contain, string name, string type)
        {
            AppGenerator.Utility.createFile(@"../AppGenerateFiles/FrontModel\" + AppGenerator.Utility.capitalize(name) + (type == "VIEW" ? "ViewModel.js" : "DataBaseModel.js"), contain);

        }
         public static void createDataBaseJSModelCompFile(string contain, string name, string type)
        {
            AppGenerator.Utility.createFile(@"../AppGenerateFiles/FrontModel\" + AppGenerator.Utility.capitalize(name) + (type == "VIEW" ? "ViewModel.js" : "ComponentsModel.js"), contain);

        }

        public static void createApiControllerFile(string contain, string name, string type)
        {
            AppGenerator.Utility.createFile(@"../AppGenerateFiles/Controllers\Api" + (type == "VIEW" ? "View" : "Entity") + AppGenerator.Utility.capitalize(name) + "Controller.cs", contain);

        }

    }
}
