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
	public enum Permissions
	{
		ADMIN_ACCESS,//PERMITE ACCESO TOTAL AL SISTEMA
		ADMINISTRAR_USUARIOS,//PERMITE ADMINISTRAR USUARIOS	
		PERFIL_MANAGER,//PERMITE ADMINISTRAR EL PERFIL DEL USUARIO
		PERFIL_ACCESS,//PERMITE ACCESO AL PERFIL		
		

		GESTION_CLIENTES,//PERMITE GESTIONAR CLIENTES  EDITARLOS Y CREARLOS
		GESTION_EMPEÑOS,//PERMITE HACER EMPEÑOS Y VALORACIONES
		GESTION_PRESTAMOS,//PERMITE HACER PRESTAMOS

		GESTION_PRESTAMOS_POR_PERSONAS_NATURALES,//PERMITE HACER PRESTAMOS DE PERSONAS NATURALES
		GESTION_SUCURSAL,//PERMITE EDITAR DATOS DE LA SUCURSAL		
		GESTION_MOVIMIENTOS,//PERMITE INGRESOS Y EGRESOS, Y MOVIMIENTOS DE CAJA

		GESTION_COMPRAS,//PERMITE HACER COMPRAS
		GESTION_VENTAS,//PERMITE HACER VENTAS
		GESTION_LOTES,//PERMITE GESTIONAR LOTES
		GESTION_RECIBOS,//PERMITE GESTIONAR RECIBOS

		//------> HELPDESK
		GENERADOR_SOLICITUDES,
		ADMINISTRAR_CASOS_DEPENDENCIA,
		TECNICO_CASOS_DEPENDENCIA,

		//Questionnaires
		QUESTIONNAIRES_MANAGER,
		QUESTIONNAIRES_GESTOR,
		QUESTIONNAIRES_USER,
		//CCA---->
		GESTION_ESTUDIANTES_PROPIOS,
        NOTIFICACIONES,
        GESTION_ESTUDIANTES,
        CAN_CHANGE_PASSWORD,
        CAN_CHANGE_OW_PASSWORD, 
		SEND_MESSAGE,
		NOTIFICACIONES_READER, 
		GESTION_CLASES,
		GESTION_CLASES_ASIGNADAS
    }
}
