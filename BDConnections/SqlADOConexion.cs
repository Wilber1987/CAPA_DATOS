using System;
using System.Collections.Generic;
using System.Text;
using CAPA_DATOS.BDCore.Abstracts;
using CAPA_DATOS.BDCore.Implementations;

namespace CAPA_DATOS;

public class SqlADOConexion
{
    private static string UserSQLConexion = "";
    public static WDataMapper? SQLM;
    public static string DataBaseName = "HELPDESK";
    public static bool Anonimo = true;    
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
    private static bool createConexion(string SQLServer, string SGBD_USER, string SWGBD_PASSWORD, string BDNAME)
    {
        UserSQLConexion = $"Data Source={SQLServer}; Initial Catalog={BDNAME}; User ID={SGBD_USER};Password={SWGBD_PASSWORD};MultipleActiveResultSets=true";
        SQLM = new WDataMapper(new SqlServerGDatos(UserSQLConexion), new SQLServerQueryBuilder());
        if (SQLM.GDatos.TestConnection()) return true;
        else
        {
            SQLM = null;
            return false;
        }
    }
}


