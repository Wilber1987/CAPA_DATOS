using System;
using System.Collections.Generic;
using System.Text;
using CAPA_DATOS.BDCore;
using CAPA_DATOS.BDCore.Abstracts;
using CAPA_DATOS.BDCore.Implementations;

namespace CAPA_DATOS;

public class SqlADOConexion
{
    public static WDataMapper? SQLM;
    static public bool IniciarConexion(string SGBD_USER, string SWGBD_PASSWORD, string SQLServer, string BDNAME)
    {
        try
        {
            return createConexion(SQLServer, SGBD_USER, SWGBD_PASSWORD, BDNAME);
        }
        catch (Exception)
        {
            SQLM = null;
            return false;
            throw;
        }
    }   
    private static bool createConexion(string SQLServer, string SGBD_USER, string SWGBD_PASSWORD, string BDNAME)
    {
        string userSQLConexion = $"Data Source={SQLServer}; Initial Catalog={BDNAME}; User ID={SGBD_USER};Password={SWGBD_PASSWORD};MultipleActiveResultSets=true";
        SQLM = new WDataMapper(new SqlServerGDatos(userSQLConexion), new SQLServerQueryBuilder());
        if (SQLM.GDatos.TestConnection()) {
            Connections.Default = SQLM;
            return true;
        } 
        else
        {
            SQLM = null;
            return false;
        }
    }
}


