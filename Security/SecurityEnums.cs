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
      ADMINISTRAR_CASOS_DEPENDENCIA,
      TECNICO_CASOS_DEPENDENCIA,
      ADMINISTRAR_USUARIOS,
      ADMINISTRAR_CATALOGOS,
      GENERADOR_SOLICITUDES,
      PERFIL_MANAGER,
      PERFIL_ACCESS,

      //Questionnaires
      QUESTIONNAIRES_MANAGER,
      QUESTIONNAIRES_GESTOR
   }
}
