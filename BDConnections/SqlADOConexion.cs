using System;
using System.Collections.Generic;
using System.Text;

namespace CAPA_DATOS;

public class SqlADOConexion
{
    private static string UserSQLConexion = "";
    public static GDatosAbstract? SQLM;
    public static string DataBaseName = "HELPDESK";
    public static bool Anonimo = true;
    static public bool IniciarConexion()
    {
        Anonimo = false;
        return IniciarConexionInLocal();
    }
    static public bool IniciarConexion(string SGBD_USER, string SWGBD_PASSWORD, string SQLServer, string BDNAME)
    {
        try
        {
            Anonimo = false;
            return createConexion(SQLServer, SGBD_USER, SWGBD_PASSWORD, BDNAME);
        }
        catch (Exception)
        {
            SQLM = null;
            Anonimo = true;
            return false;
            throw;
        }
    }
    static public bool IniciarConexionAnonima()
    {
        try
        {
            Anonimo = false;
            return IniciarConexion();
        }
        catch (Exception)
        {
            return false;
            throw;
        }
    }
    static public bool IniciarConexionInLocal()
    {
        try
        {
            Anonimo = false;
            return createConexion(".", "sa", "zaxscd", DataBaseName);
            return createConexion(".", "sa", "Helpdesk2023", DataBaseName);
            return createConexion(".\\SQLEXPRESS", "sa", "123", DataBaseName);
            return createConexion("tcp:empresa-sa.database.windows.net", "empresa", "Wmatus09%", DataBaseName);
        }
        catch (Exception)
        {
            SQLM = null;
            Anonimo = true;
            return false;
            throw;
        }
    }

    private static bool createConexion(string SQLServer, string SGBD_USER, string SWGBD_PASSWORD, string BDNAME)
    {
        UserSQLConexion = "Data Source=" + SQLServer +
           "; Initial Catalog=" + BDNAME + "; User ID="
           + SGBD_USER + ";Password=" + SWGBD_PASSWORD + ";MultipleActiveResultSets=true";
        SQLM = new SqlServerGDatos(UserSQLConexion);
        if (SQLM.TestConnection()) return true;
        else
        {
            SQLM = null;
            return false;
        }
    }
}


