using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CAPA_DATOS;

namespace CAPA_DATOS.Security
{
   public enum RoleEnum
   {
      ADMIN
   }
   public enum PermissionsEnum
   {

      ADMIN_ACCESS,
      PERFIL_MANAGER,
      GESTION_CLIENTES,//PERMITE GESTIONAR CLIENTES
                       //GESTION_INGRESOS,
                       //GESTION_EGRESOS,
      GESTION_PRESTAMOS_Y_EMPEÑOS,//PERMITE HACER VALORACIONES
      GESTION_PRESTAMOS_POR_PERSONAS_NATURALES,
      GESTION_CONTRATOS,
      GESTION_SUCURSALES,
      GESTION_MOVIMIENTOS,//PERMITE INGRESOS Y EGRESOS, Y MOVIMIENTOS DE CAJA
      ADMINISTRAR_USUARIOS,
   }
}
