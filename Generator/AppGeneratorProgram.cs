using System;
using System.IO;
using System.Text;
using CAPA_DATOS;
using CAPA_DATOS.Generator;

namespace AppGenerate
{
    public class AppGeneratorProgram
    {
        public static SQLDatabaseDescriptor SQLDatabaseDescriptor = new SQLDatabaseDescriptor(SqlADOConexion.SQLM?.GDatos);

        public static void Main()
        {
            try
            {
                var SQLDatabaseDescriptor = new SQLDatabaseDescriptor(SqlADOConexion.SQLM?.GDatos);
                //SqlADOConexion.IniciarConexionAnonima();
                StringBuilder indexBuilder = AppGenerator.CSharpEnviroment.CSharpIndexBuilder();
                foreach (var schema in SQLDatabaseDescriptor.databaseSchemas())
                {
                    foreach (var schemaType in SQLDatabaseDescriptor.databaseTypes())
                    {
                        StringBuilder controllerString;
                        AppGenerator.CSharpEnviroment.setControllerCSharpHeaders(out controllerString, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        var describeSchema = SQLDatabaseDescriptor.describeSchema(schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                        foreach (var table in describeSchema)
                        {
                            StringBuilder entityString, jsEntityString, jsEntityComponentString, jsEntityHeaderString, jsEntityHeaderComponentString;
                            jsEntityString = new StringBuilder();
                            jsEntityComponentString = new StringBuilder();
                            AppGenerator.CSharpEnviroment.setCSharpHeaders(out entityString, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                            AppGenerator.JsEnviroment.setJsHeaders(out jsEntityHeaderString);
                            AppGenerator.JsEnviroment.setJsHeaders(out jsEntityHeaderComponentString);
                            if (table.TABLE_NAME == "sysdiagrams")
                            {
                                continue;
                            }
                            //BUILD ENTITY
                            AppGenerator.CSharpEnviroment.mapCSharpEntity(entityString, table);

                            AppGenerator.JsEnviroment.mapJsEntity(jsEntityString, jsEntityHeaderString, table, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);
                            AppGenerator.JsEnviroment.mapJsModel(jsEntityComponentString, jsEntityHeaderComponentString, table, schema.TABLE_SCHEMA, schemaType.TABLE_TYPE);

                            //BUILD ENTITY CONTROLLER
                            AppGenerator.CSharpEnviroment.buildApiController(schemaType, controllerString, table);
                            if (!table.TABLE_NAME.ToLower().StartsWith("catalogo"))
                            {
                                // AppGenerator.JsEnviroment.setJsViewBuilder(schema.TABLE_SCHEMA, table.TABLE_NAME, schemaType.TABLE_TYPE);
                            }
                            AppGenerator.CSharpEnviroment.CSharpIndexBuilder(indexBuilder, table);
                            entityString.AppendLine("}");
                            AppGenerator.JsEnviroment.setJsCatalogoBuilder(schema.TABLE_SCHEMA,
                                describeSchema.Where(t => t.TABLE_NAME.ToLower().StartsWith("catalogo")).Select(s => s.TABLE_NAME).ToList());

                            createFile(entityString.ToString(), table.TABLE_NAME, schemaType.TABLE_TYPE, schema.TABLE_SCHEMA);
                            createDataBaseJSModelFile(jsEntityHeaderString.ToString() + jsEntityString.ToString(), table.TABLE_NAME, schemaType.TABLE_TYPE, schema.TABLE_SCHEMA);
                            createDataBaseJSModelCompFile(jsEntityHeaderComponentString.ToString() + jsEntityComponentString.ToString(), table.TABLE_NAME, schemaType.TABLE_TYPE, schema.TABLE_SCHEMA);

                        }

                        controllerString.AppendLine("   }");
                        controllerString.AppendLine("}");
                        createApiControllerFile(controllerString.ToString(), schema.TABLE_SCHEMA, schemaType.TABLE_TYPE, schema.TABLE_SCHEMA);

                    }
                    //AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema.TABLE_SCHEMA}/Controllers/SecurityController.cs", schema.TABLE_SCHEMA, AppGenerator.CSharpEnviroment.buildApiSecurityController());
                    //AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema.TABLE_SCHEMA}/Security/AuthNetcore.cs", schema.TABLE_SCHEMA, AppGenerator.CSharpEnviroment.body);
                    //AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema.TABLE_SCHEMA}/Pages/LoginView.cshtml", schema.TABLE_SCHEMA, AppGenerator.CSharpEnviroment.loginString);
                    indexBuilder.Append(AppGenerator.CSharpEnviroment.transactionalMenu);
                    indexBuilder.Append(AppGenerator.CSharpEnviroment.catalogoMenu);
                    AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema.TABLE_SCHEMA}/Pages/Index.cshtml", schema.TABLE_SCHEMA, indexBuilder.ToString());
                }

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void createFile(string contain, string name, string type, string schema)
        {
            AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema}/Model/" + AppGenerator.Utility.capitalize(name) + (type == "VIEW" ? "ViewModel.cs" : ".cs"), schema, contain);

        }

        public static void createDataBaseJSModelFile(string contain, string name, string type, string schema)
        {
            AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema}/FrontModel/" + AppGenerator.Utility.capitalize(name) + (type == "VIEW" ? "ViewModel.js" : ".js"), schema, contain);

        }
        public static void createDataBaseJSModelCompFile(string contain, string name, string type, string schema)
        {
            AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema}/FrontModel/ModelComponent/" + AppGenerator.Utility.capitalize(name) + (type == "VIEW" ? "ViewModel.js" : "_ModelComponent.js"), schema, contain);

        }

        public static void createApiControllerFile(string contain, string name, string type, string schema)
        {
            AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema}/Controllers/Api" + (type == "VIEW" ? "View" : "Entity") + AppGenerator.Utility.capitalize(schema) + "Controller.cs", schema, contain);

        }

    }
}
