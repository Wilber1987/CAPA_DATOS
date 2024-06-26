﻿using System.Text;
using AppGenerate;
using CAPA_DATOS;

namespace AppGenerator
{
    public class CSharpEnviroment
    {
        public static string body = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CAPA_DATOS;
using DataBaseModel;

namespace Security
{
    public class AuthNetCore
    {
        static private string SGBD_SERVER = server;
        static private string SWGBD_BD = bd;
        static private string SGBD_USER = dbuser;
        static private string SWGBD_PASSWORD = dbpassword;
        static public bool AuthAttribute = false;
        static private Security_Users security_User;
        static public bool Authenticate()
        {
            if (AppGeneratorProgram.SQLDatabaseDescriptor == null || SqlADOConexion.Anonimo || security_User == null)
            {
                security_User = null;
                AppGeneratorProgram.SQLDatabaseDescriptor = null;
                return false;
            }
            return true;

        }
        static public bool AnonymousAuthenticate()
        {
            SqlADOConexion.IniciarConexionAnonima();
            return true;
        }
        static public bool loginIN(string mail, string password)
        {
            if (mail == null || password == null) throw new Exception();
            try
            {
                SqlADOConexion.IniciarConexionSNIBD(SGBD_USER, SWGBD_PASSWORD);
                security_User = new Security_Users()
                {
                    Mail = mail,
                    Password = password
                }.Find<Security_Users>();
                if (security_User.Id_User == null)
                {
                    security_User = null;
                    AppGeneratorProgram.SQLDatabaseDescriptor = null;
                    throw new Exception();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static public bool ClearSeason()
        {
            AppGeneratorProgram.SQLDatabaseDescriptor = null;
            security_User = null;
            return true;

        }

        public static bool HavePermission(string permission)
        {
            if (Authenticate())
            {
                var roleHavePermision = security_User.Security_Users_Roles.Where(r => RoleHavePermission(permission, r).Count != 0).ToList();
                if (roleHavePermision.Count != 0) return true;
                return false;
            }
            else
            {
                return false;
            }
        }

        private static List<Security_Permissions_Roles> RoleHavePermission(string permission, Security_Users_Roles r)
        {
            return r.Security_Role.Security_Permissions_Roles.Where(p => p.Security_Permissions.Descripcion == permission).ToList();
        }
    }
}
";

        public static string loginString = @"@page
@{
     Layout = null;
}
<!DOCTYPE html>
<html>
    <head>
        <meta charset='utf-8' />
        <title> Login </title>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>  
        <script type='module'>
            import { WRender, WAjaxTools } from './WDevCore/WModules/WComponentsTools.js';
            import { WSecurity } from './WDevCore/WModules/WSecurity.js';
            import { WForm } from './WDevCore/WComponents/WForm.js';

            const OnLoad = async () => {                
                const LoginForm = new WForm({
                    ModelObject: { Mail: { type: 'text'}, Password: { type: 'password' } } ,
                    SaveFunction: async (UserData) => { 
                        WSecurity.Login(UserData, window.location.origin)
                    }
                });
                App.appendChild(LoginForm);
            }
            window.onload = OnLoad;

        </script>
    </head>
    <body id='App'>
    </body>
</html>";

        public static StringBuilder catalogoMenu = new StringBuilder();
        public static StringBuilder transactionalMenu = new StringBuilder();
        public static StringBuilder CSharpIndexBuilder()
        {
            StringBuilder indexBuilder = new StringBuilder();
            indexBuilder.AppendLine("@page");
            indexBuilder.AppendLine(@"@using API.Controllers
@{
    if (!SecurityController.Auth())
    {
        Response.Redirect(" + "\"../LoginView\");" + @"
        return;
    }   
}");
            return indexBuilder;
        }

        public static void CSharpIndexBuilder(StringBuilder indexBuilder, EntitySchema table)
        {
            if (table.TABLE_NAME.ToLower().StartsWith("relational") || table.TABLE_NAME.ToLower().StartsWith("detail"))
            {
                return;
            }
            if (table.TABLE_NAME.ToLower().StartsWith("transaction"))
            {
                transactionalMenu.AppendLine("<a class=\"nav-link text-dark\" asp-area=\"\" asp-page=\"/" +
                  (Utility.capitalize(table.TABLE_NAME).Contains("Catalogo") ? "PagesCatalogos" : "PagesViews") + "/"
                  + Utility.capitalize(table.TABLE_NAME) + "View\"> " + Utility.capitalize(table.TABLE_NAME) + "</a>");
            }
            if (table.TABLE_NAME.ToLower().StartsWith("catalogo"))
            {
                catalogoMenu.AppendLine("<a class=\"nav-link text-dark\" asp-area=\"\" asp-page=\"/" +
               (Utility.capitalize(table.TABLE_NAME).Contains("Catalogo") ? "PagesCatalogos" : "PagesViews") + "/"
               + Utility.capitalize(table.TABLE_NAME) + "View\"> " + Utility.capitalize(table.TABLE_NAME) + "</a>");
            }

        }

        public static string buildApiSecurityController()
        {
            StringBuilder controllerString = new StringBuilder();
            controllerString.AppendLine("using DataBaseModel;");
            controllerString.AppendLine("using Security;");
            controllerString.AppendLine("using Microsoft.AspNetCore.Http;");
            controllerString.AppendLine("using Microsoft.AspNetCore.Mvc;");


            controllerString.AppendLine("namespace API.Controllers {");
            controllerString.AppendLine("   [Route(\"api/[controller]/[action]\")]");
            controllerString.AppendLine("   [ApiController]");
            controllerString.AppendLine("   public class SecurityController : ControllerBase {");
            controllerString.AppendLine("       [HttpPost]");
            controllerString.AppendLine("       public object Login(Security_Users Inst) {");
            controllerString.AppendLine("           return AuthNetCore.loginIN(Inst.Mail, Inst.Password);");
            controllerString.AppendLine("       }");
            controllerString.AppendLine("       public  static bool Auth() {");
            controllerString.AppendLine("           return AuthNetCore.Authenticate();");
            controllerString.AppendLine("       }");
            controllerString.AppendLine("       public  static bool HavePermission(string permission) {");
            controllerString.AppendLine("           return AuthNetCore.HavePermission(permission);");
            controllerString.AppendLine("       }");
            controllerString.AppendLine("   }");

            controllerString.AppendLine("}");
            return controllerString.ToString();

        }

        public static void buildApiController(EntitySchema schemaType, StringBuilder controllerString, EntitySchema table)
        {
            controllerString.AppendLine("       //" + Utility.capitalize(table.TABLE_NAME));
            controllerString.AppendLine("       [HttpPost]");
            controllerString.AppendLine("       [AuthController]");
            controllerString.AppendLine("       public List<" + Utility.capitalize(table.TABLE_NAME) + "> get" + Utility.capitalize(table.TABLE_NAME) + "(" + Utility.capitalize(table.TABLE_NAME) + " Inst) {");
            controllerString.AppendLine("           return Inst.Get<" + Utility.capitalize(table.TABLE_NAME) + ">();");
            controllerString.AppendLine("       }");
            controllerString.AppendLine("       [HttpPost]");
            controllerString.AppendLine("       [AuthController]");
            controllerString.AppendLine("       public " + Utility.capitalize(table.TABLE_NAME) + " find" + Utility.capitalize(table.TABLE_NAME) + "(" + Utility.capitalize(table.TABLE_NAME) + " Inst) {");
            controllerString.AppendLine("           return Inst.Find<" + Utility.capitalize(table.TABLE_NAME) + ">();");
            controllerString.AppendLine("       }");
            if (schemaType.TABLE_TYPE == "BASE TABLE")
            {
                controllerString.AppendLine("       [HttpPost]");
                controllerString.AppendLine("       [AuthController]");
                controllerString.AppendLine("       public object save" + Utility.capitalize(table.TABLE_NAME) + "(" + Utility.capitalize(table.TABLE_NAME) + " inst) {");
                controllerString.AppendLine("           return inst.Save();");
                controllerString.AppendLine("       }");
                controllerString.AppendLine("       [HttpPost]");
                controllerString.AppendLine("       [AuthController]");
                controllerString.AppendLine("       public object update" + Utility.capitalize(table.TABLE_NAME) + "(" + Utility.capitalize(table.TABLE_NAME) + " inst) {");
                controllerString.AppendLine("           return inst.Update();");
                controllerString.AppendLine("       }");
            }
        }

        public static void mapCSharpEntity(StringBuilder entityString, EntitySchema table)
        {
            entityString.AppendLine("   public class " + Utility.capitalize(table.TABLE_NAME) + " : EntityClass {");
            foreach (var entity in AppGeneratorProgram.SQLDatabaseDescriptor.describeEntity(table.TABLE_NAME))
            {
                string type = "";
                switch (entity.DATA_TYPE)
                {
                    case "int": type = "int"; break;
                    case "smallint": type = "short"; break;
                    case "bigint": type = "long"; break;
                    case "decimal": type = "Double"; break;
                    case "money": type = "Double"; break;
                    case "float": type = "Double"; break;
                    case "char": type = "string"; break;
                    case "nchar": type = "string"; break;
                    case "varchar": type = "string"; break;
                    case "nvarchar": type = "string"; break;
                    case "uniqueidentifier": type = "string"; break;
                    case "datetime":case "datetime2": type = "DateTime"; break;
                    case "date": type = "DateTime"; break;
                    case "bit":case "binary(1)": type = "bool"; break;
                }

                if (AppGeneratorProgram.SQLDatabaseDescriptor.isPrimary(table.TABLE_NAME, entity.COLUMN_NAME))
                {
                    var columnProps = AppGeneratorProgram.SQLDatabaseDescriptor.describePrimaryKey(table.TABLE_NAME, entity.COLUMN_NAME);
                    entityString.AppendLine("       [PrimaryKey(Identity = " + (columnProps != null ? "true" : "false") + ")]");
                }
                entityString.AppendLine("       public " + type
                                     + "? " + Utility.capitalize(entity.COLUMN_NAME)
                                     + " { get; set; }");

            }

            var ManyToOneKeys = AppGeneratorProgram.SQLDatabaseDescriptor.ManyToOneKeys($"{table.TABLE_SCHEMA}.{table.TABLE_NAME}");
            foreach (var entity in ManyToOneKeys)
            {

                //var oneToMany = AppGeneratorProgram.SQLDatabaseDescriptor.oneToManyKeys(entity.REFERENCE_TABLE_NAME);
                //var find = oneToMany.Find(o => o.FKTABLE_NAME == table.TABLE_NAME);
                //if (find == null)
                //{
                string relationalName = "ManyToOne";
                int fkey = AppGeneratorProgram.SQLDatabaseDescriptor.evalKeyType(table.TABLE_NAME, entity.CONSTRAINT_COLUMN_NAME, "PRIMARY KEY");
                int nkeyTable = AppGeneratorProgram.SQLDatabaseDescriptor.keyInformation(table.TABLE_NAME, "PRIMARY KEY");
                int nkeyReferenceTable = AppGeneratorProgram.SQLDatabaseDescriptor.keyInformation(entity.REFERENCE_TABLE_NAME, "PRIMARY KEY");
                if (fkey == 1 && nkeyTable == 1 && nkeyReferenceTable == 1)
                {
                    relationalName = "OneToOne";
                }
                entityString.AppendLine("       [" + relationalName + "("
                                  + "TableName = \""
                                  + Utility.capitalize(entity.REFERENCE_TABLE_NAME) + "\", "
                                  + "KeyColumn = \""
                                  + Utility.capitalize(entity.REFERENCE_COLUMN_NAME)  + "\", "
                                  + "ForeignKeyColumn = \""
                                  + Utility.capitalize(entity.CONSTRAINT_COLUMN_NAME)  + "\")]");
                string propName = entity.REFERENCE_TABLE_NAME;
                if (ManyToOneKeys.Where(e => e.REFERENCE_TABLE_NAME == entity.REFERENCE_TABLE_NAME).ToList().Count > 1)
                {
                    propName = Utility.capitalize(entity.REFERENCE_TABLE_NAME) + "_" + Utility.capitalize(entity.CONSTRAINT_COLUMN_NAME);
                }
                entityString.AppendLine("       public " + Utility.capitalize(entity.REFERENCE_TABLE_NAME)
                    + "? " +  Utility.capitalize(propName)
                    + " { get; set; }");
                //}

            }
            var oneToManyKeys = AppGeneratorProgram.SQLDatabaseDescriptor.oneToManyKeys($"{table.TABLE_NAME}", $"{table.TABLE_SCHEMA}");
            foreach (var entity in oneToManyKeys)
            {
                string relationalName = "OneToMany";
                int fkey = AppGeneratorProgram.SQLDatabaseDescriptor.evalKeyType(entity.FKTABLE_NAME, entity.FKCOLUMN_NAME, "PRIMARY KEY");
                int nkeyTable = AppGeneratorProgram.SQLDatabaseDescriptor.keyInformation(table.TABLE_NAME, "PRIMARY KEY");
                int nkeyReferenceTable = AppGeneratorProgram.SQLDatabaseDescriptor.keyInformation(entity.FKTABLE_NAME, "PRIMARY KEY");
                if (fkey == 1 && nkeyTable == 1 && nkeyReferenceTable == 1)
                {
                    relationalName = "OneToOne";
                }
                entityString.AppendLine("       [" + relationalName + "("
              + "TableName = \""
              + Utility.capitalize(entity.FKTABLE_NAME) + "\", "
              + "KeyColumn = \""
              + Utility.capitalize(entity.PKCOLUMN_NAME) + "\", "
              + "ForeignKeyColumn = \""
              + Utility.capitalize(entity.FKCOLUMN_NAME) + "\")]");
                string propName = entity.FKTABLE_NAME;
                string type = " List<" + Utility.capitalize(entity.FKTABLE_NAME) + ">? ";
                if (oneToManyKeys.Where(e => e.FKTABLE_NAME == entity.FKTABLE_NAME).ToList().Count > 1)
                {
                    propName = entity.FKTABLE_NAME + "_" + entity.FKCOLUMN_NAME;
                }
                if (relationalName == "OneToOne")
                {
                    type = " " + Utility.capitalize(entity.FKTABLE_NAME) + "? ";
                }
                entityString.AppendLine("       public" + type + Utility.capitalize(propName)
                    + " { get; set; }");
            }
            entityString.AppendLine("   }");
        }

        public static void setCSharpHeaders(out StringBuilder entityString, string schema, string type)
        {
            entityString = new StringBuilder();
            entityString.AppendLine("using CAPA_DATOS;");
            entityString.AppendLine("using System;");
            entityString.AppendLine("using System.Collections.Generic;");
            entityString.AppendLine("using System.Linq;");
            entityString.AppendLine("using System.Text;");
            entityString.AppendLine("using System.Threading.Tasks;");

            entityString.AppendLine("namespace DataBaseModel {");
        }
        public static void setControllerCSharpHeaders(out StringBuilder controllerString, string schema, string type)
        {
            controllerString = new StringBuilder();
            controllerString.AppendLine("using DataBaseModel;");
            controllerString.AppendLine("using Security;");
            controllerString.AppendLine("using Microsoft.AspNetCore.Http;");
            controllerString.AppendLine("using Microsoft.AspNetCore.Mvc;");
            controllerString.AppendLine("using System.Collections.Generic;");


            controllerString.AppendLine("namespace API.Controllers {");
            controllerString.AppendLine("   [Route(\"api/[controller]/[action]\")]");
            controllerString.AppendLine("   [ApiController]");

            controllerString.AppendLine("   public class  Api" + (type == "VIEW" ? "View" : "Entity") + AppGenerator.Utility.capitalize(schema) + "Controller : ControllerBase {");
        }
       
        public static void createCSharpView(string name, string schema)
        {
            if (name.ToLower().StartsWith("relational") || name.ToLower().StartsWith("detail"))
            {
                return;
            }
            var pageString = new StringBuilder();
            pageString.AppendLine(@"@page
@using API.Controllers
@{
    if (!SecurityController.Auth())
    {
        Response.Redirect(" + "\"../LoginView\");" + @"
        return;
    }    
}
<script src='~/Views/" + name + @"View.js' type='module'></script>
<div id='MainBody'></div>");
            AppGenerator.Utility.createFile($"../AppGenerateFiles/{schema}/" + (name.Contains("Catalogo") ? "PagesCatalogos" : "PagesViews") + "\\" + name + "View.cshtml", schema, pageString.ToString());
        }

    }

}
