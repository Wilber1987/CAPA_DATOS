using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CAPA_DATOS.BDCore
{
    public class Conexiones
    {
        public static GDatosAbstract? SQLM1;
        public static GDatosAbstract? SQLM2;
        public static GDatosAbstract? SQLM3;
        public static GDatosAbstract? SQLM4;
        public static GDatosAbstract? SQLM5;
        public static void InicializarConexion()
        {
            SQLM1 = new SqlServerGDatos("CADENA1");
            SQLM2 = new SqlServerGDatos("CADENA2");
            SQLM3 = new SqlServerGDatos("CADENA3");
            SQLM4 = new SqlServerGDatos("CADENA4");

        }
    }
}