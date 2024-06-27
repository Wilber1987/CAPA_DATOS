
DELETE FROM [security].Security_Users_Roles;
DELETE FROM [security].Security_Permissions_Roles;
DELETE FROM [security].Security_Users;
DELETE FROM [security].Security_Permissions;
DELETE FROM [security].Security_Roles;

DBCC CHECKIDENT ('[security].Security_Users', RESEED, 0);
DBCC CHECKIDENT ('[security].Security_Permissions', RESEED, 0);
DBCC CHECKIDENT ('[security].Security_Roles', RESEED, 0);


INSERT INTO [security].Security_Permissions (Descripcion, Detalles, Estado)
VALUES (
        'ADMIN_ACCESS',
        'PERMITE ACCESO TOTAL AL SISTEMA',
        'ACTIVO'
    ),
    (
        'ADMINISTRAR_USUARIOS',
        'PERMITE ADMINISTRAR USUARIOS',
        'ACTIVO'
    ),
    (
        'PERFIL_MANAGER',
        'PERMITE ADMINISTRAR EL PERFIL DEL USUARIO',
        'ACTIVO'
    ),
    (
        'PERFIL_ACCESS',
        'PERMITE ACCESO AL PERFIL',
        'ACTIVO'
    ),
    (
        'GESTION_CLIENTES',
        'PERMITE GESTIONAR CLIENTES EDITARLOS Y CREARLOS',
        'ACTIVO'
    ),
    (
        'GESTION_EMPEÑOS',
        'PERMITE HACER EMPEÑOS Y VALORACIONES',
        'ACTIVO'
    ),
    (
        'GESTION_PRESTAMOS',
        'PERMITE HACER PRESTAMOS',
        'ACTIVO'
    ),
    (
        'GESTION_PRESTAMOS_POR_PERSONAS_NATURALES',
        'PERMITE HACER PRESTAMOS DE PERSONAS NATURALES',
        'ACTIVO'
    ),
    (
        'GESTION_SUCURSAL',
        'PERMITE EDITAR DATOS DE LA SUCURSAL',
        'ACTIVO'
    ),
    (
        'GESTION_MOVIMIENTOS',
        'PERMITE INGRESOS Y EGRESOS, Y MOVIMIENTOS DE CAJA',
        'ACTIVO'
    ),
    (
        'GESTION_COMPRAS',
        'PERMITE HACER COMPRAS',
        'ACTIVO'
    ),
    (
        'GESTION_VENTAS',
        'PERMITE HACER VENTAS',
        'ACTIVO'
    ),
    (
        'GESTION_LOTES',
        'PERMITE GESTIONAR LOTES',
        'ACTIVO'
    );
INSERT INTO [security].Security_Permissions
(Descripcion, Estado, Detalles)
VALUES(N'GESTION_RECIBOS', N'ACTIVO', N'PERMITE GESTIONAR RECIBOS');


    
INSERT INTO [security].Security_Roles
(Descripcion, Estado)
VALUES(N'ADMIN', N'ACTIVO');

INSERT INTO [security].Security_Permissions_Roles
(Id_Role, Id_Permission, Estado)
VALUES(1, 1, N'ACTIVO');

INSERT INTO [security].Security_Users (
        Nombres,
        Estado,
        Descripcion,
        Password,
        Mail,
        Token,
        Token_Date,
        Token_Expiration_Date
    )
VALUES(
        N'ADMIN',
        N'ACTIVO',
        N'ADMIN',
        N'PxI/Pz8/Pz8/PwdSP2E/Pw==',
        N'admin@admin.net',
        N'',
        '1900-01-01 00:00:00.000',
        '2025-01-01 00:00:00.000'
    );

INSERT INTO [security].Security_Users_Roles
(Id_Role, Id_User, Estado)
VALUES(1, 1, N'ACTIVO');

INSERT INTO [security].Tbl_Profile
(Nombres, Apellidos, FechaNac, IdUser, Sexo, Foto, DNI, Correo_institucional, Id_Pais_Origen, Estado)
VALUES(N'Admin', N'Admin', NULL, 1, NULL, NULL, NULL, NULL, NULL, N'ACTIVO');