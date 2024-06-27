delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Comments_Tasks 
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Comments
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Mails  
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Calendario 
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Participantes  
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Tareas 
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Profile_CasosAsignados  
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Case


delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda 
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Servicios
delete from PROYECT_MANAGER_BD.helpdesk.Tbl_Dependencias_Usuarios  
delete from PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias 

DBCC CHECKIDENT ('PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias', RESEED, 0);


--DATOS BASICOS DE PRUEBA
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias
(Descripcion, Id_Dependencia_Padre, Username, Password, Host, AutenticationType, TENAT, CLIENT, OBJECTID, CLIENT_SECRET, HostService, SMTPHOST, [default])
VALUES(N'DEVEXP', NULL, N'wdevexp@outlook.com', N'%WtestDev2023%1', N'outlook.office365.com', N'BASIC', NULL, NULL, NULL, NULL, N'OUTLOOK', N'smtp-mail.outlook.com', NULL);

INSERT INTO PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias
(Descripcion, Id_Dependencia_Padre, Username, Password, Host, AutenticationType, TENAT, CLIENT, OBJECTID, CLIENT_SECRET, HostService, SMTPHOST, [default])
VALUES(N'ON_DEVEXP', NULL, N'wilbermatusgonzalez@wexpdev.onmicrosoft.com', N'-', N'outlook.office365.com', N'AUTH2', N'8097a003-1162-40cb-ba74-f198eda4d6e9', N'b3161d3c-f437-47b7-aa3b-6a0ed3532f5b', N'1c56f54e-0255-44fd-a5ea-8be46debd21c', N'RrH8Q~O6hHqDetZWbNOYLQrdRgn.WupFPlSpBatO', N'OUTLOOK', N'smtp-mail.outlook.com', 1);
--servicios de prueba

INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Servicios
(Nombre_Servicio, Descripcion_Servicio, Id_Tipo_Servicio, Visibilidad, Estado, Fecha_Inicio, Fecha_Finalizacion, Id_Dependencia)
VALUES(N'Servicio Atención', N'Atención', 1, N'publica', N'Activo', '2021-01-12', '2021-12-12', 1);
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Servicios
(Nombre_Servicio, Descripcion_Servicio, Id_Tipo_Servicio, Visibilidad, Estado, Fecha_Inicio, Fecha_Finalizacion, Id_Dependencia)
VALUES(N'Servicio Atención', N'Atención', 3, N'publica', N'Activo', NULL, NULL, 2);

--Agenda dependencia


INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 1, N'Lunes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 1, N'Martes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 1, N'Miércoles', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 1, N'Jueves', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 1, N'Viernes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');

INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 2, N'Lunes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 2, N'Martes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 2, N'Miércoles', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 2, N'Jueves', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
(Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
VALUES(NULL, 2, N'Viernes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');


--seguridad

DELETE FROM PROYECT_MANAGER_BD.[security].Security_Users_Roles;
DELETE FROM PROYECT_MANAGER_BD.[security].Security_Permissions_Roles;
DELETE FROM PROYECT_MANAGER_BD.[security].Tbl_Profile;
DELETE FROM PROYECT_MANAGER_BD.[security].Security_Users;
DELETE FROM PROYECT_MANAGER_BD.[security].Security_Permissions;
DELETE FROM PROYECT_MANAGER_BD.[security].Security_Roles;

DBCC CHECKIDENT ('PROYECT_MANAGER_BD.[security].Security_Users', RESEED, 0);
DBCC CHECKIDENT ('PROYECT_MANAGER_BD.[security].Security_Permissions', RESEED, 0);
DBCC CHECKIDENT ('PROYECT_MANAGER_BD.[security].Security_Roles', RESEED, 0);


ALTER TABLE PROYECT_MANAGER_BD.[security].Security_Permissions ADD Detalles nvarchar(500) NULL;


INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'ADMIN_ACCESS', N'Activo', N'ACCESO TOTAL DEL SISTEMA');
INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'ADMINISTRAR_CASOS_DEPENDENCIA', N'Activo', N'PUEDE VER LOS CASOS DE TODA LA DEPENDENCIA A LA QUE PERTENECE, APROBARLOS, RECHAZARLOS, CERRARLOS');
INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'TECNICO_CASOS_DEPENDENCIA', N'Activo', N'PUEDE VER LOS CASOS ASIGNADOS Y CERRARLOS');

INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'GENERADOR_SOLICITUDES', N'Activo', N'PERMITE REALIZAR SOLICITUDES DE CASOS DENTRO DE LA PLATAFORMA');

INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'PERFIL_ACCESS', N'Activo', N'PERMITE ACCEDER AL PERFIL DEL USUARIO');

INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'PERFIL_MANAGER', N'Activo', N'PERMITE MANIPULAR PERFILES DE LOS USUARIO');


INSERT INTO PROYECT_MANAGER_BD.[security].Security_Roles
(Descripcion, Estado)
VALUES(N'ADMIN', N'ACTIVO');

INSERT INTO PROYECT_MANAGER_BD.[security].Security_Permissions_Roles 
VALUES(1, 1, N'ACTIVO');

INSERT INTO PROYECT_MANAGER_BD.[security].Security_Users
(Nombres, Estado, Descripcion, Password, Mail, Token, Token_Date, Token_Expiration_Date)
VALUES(N'ADMIN', N'ACTIVO', N'ADMIN', N'PxI/Pz8/Pz8/PwdSP2E/Pw==', N'admin@admin.net', NULL, NULL, NULL);

INSERT INTO PROYECT_MANAGER_BD.[security].Tbl_Profile
(Nombres, Apellidos, FechaNac, IdUser, Sexo, Foto, DNI, Correo_institucional, Id_Pais_Origen, Estado)
VALUES(N'ADMIN', N'ADMIN', '1900-01-01', 1, N'Masculino', N'\Media\profiles\avatar.png', N'1112dfg', N'admin@admin.net', NULL, N'ACTIVO');

INSERT INTO PROYECT_MANAGER_BD.[security].Security_Users_Roles
(Id_Role, Id_User, Estado)
VALUES(1, 1, N'ACTIVO');




