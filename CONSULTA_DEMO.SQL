CREATE PROCEDURE CrearUsuariosDesdeJSON
    @JSON NVARCHAR(MAX)
AS
BEGIN
    -- Variable para almacenar los datos del JSON
    DECLARE @TablaUsuarios TABLE (
        Departamento NVARCHAR(100),
        Cargo NVARCHAR(100),
        NombresYApellidos NVARCHAR(200),
        CorreoElectronico NVARCHAR(200),
        Telefono NVARCHAR(50),
        Extension NVARCHAR(50),
        Observaciones NVARCHAR(MAX)
    )

    -- Insertar los datos del JSON en la tabla temporal
    INSERT INTO @TablaUsuarios (Departamento, Cargo, NombresYApellidos, CorreoElectronico, Telefono, Extension, Observaciones)
    SELECT Departamento, Cargo, NombresYApellidos, CorreoElectronico, Telefono, Extension, Observaciones
    FROM OPENJSON(@JSON)
    WITH (
        Departamento NVARCHAR(100),
        Cargo NVARCHAR(100),
        NombresYApellidos NVARCHAR(200),
        CorreoElectronico NVARCHAR(200),
        Telefono NVARCHAR(50),
        Extension NVARCHAR(50),
        Observaciones NVARCHAR(MAX)
    )
    SELECT * FROM @TablaUsuarios

    -- Recorrer la tabla temporal para insertar los registros en la base de datos
    DECLARE @DepartamentoId INT, 
	    @CargoId INT, 
	    @IPerfil INT,
	    @UsuarioId INT, 
	    @NombresYApellidos NVARCHAR(MAX), 
	    @CorreoElectronico NVARCHAR(MAX),
	    @Telefono NVARCHAR(MAX), 
	    @Extension NVARCHAR(MAX),
	    @Observaciones NVARCHAR(MAX),
	    @Departamento NVARCHAR(MAX), 
	    @Cargo NVARCHAR(MAX)

    DECLARE usuarioCursor CURSOR FOR
    SELECT Departamento, Cargo, NombresYApellidos, CorreoElectronico, Telefono, Extension, Observaciones
    FROM @TablaUsuarios

    OPEN usuarioCursor
    FETCH NEXT FROM usuarioCursor INTO @Departamento, @Cargo, @NombresYApellidos, @CorreoElectronico, @Telefono, @Extension, @Observaciones

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Verificar si el departamento existe, sino crearlo
        IF NOT EXISTS (SELECT 1 FROM PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias WHERE Descripcion = @Departamento)
        BEGIN
            -- Insertar nuevo departamento
            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias (Descripcion)
            VALUES (@Departamento)
            SET @DepartamentoId = SCOPE_IDENTITY()

            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
            (Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
            VALUES(NULL,  @DepartamentoId, N'Lunes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
            (Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
            VALUES(NULL,  @DepartamentoId, N'Martes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
            (Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
            VALUES(NULL,  @DepartamentoId, N'Miércoles', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
            (Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
            VALUES(NULL,  @DepartamentoId, N'Jueves', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Agenda
            (Id_Perfil, Id_Dependencia, Dia, Hora_Inicial, Hora_Final, Fecha_Caducidad)
            VALUES(NULL,  @DepartamentoId, N'Viernes', N'08:00', N'17:00', '2024-06-07 00:00:00.000');
        END
        ELSE
        BEGIN
	       
            -- Obtener el ID del departamento existente
            SELECT @DepartamentoId = Id_Dependencia FROM PROYECT_MANAGER_BD.helpdesk.Cat_Dependencias WHERE Descripcion = @Departamento
        END

        -- Verificar si el cargo existe, sino crearlo
        IF NOT EXISTS (SELECT 1 FROM PROYECT_MANAGER_BD.helpdesk.Cat_Cargos_Dependencias WHERE Descripcion = @Cargo)
        BEGIN
            -- Insertar nuevo cargo
            INSERT INTO PROYECT_MANAGER_BD.helpdesk.Cat_Cargos_Dependencias (Descripcion)
            VALUES (@Cargo)
            SET @CargoId = SCOPE_IDENTITY()
        END
        ELSE
        BEGIN
            -- Obtener el ID del cargo existente
            SELECT @CargoId = Id_Cargo FROM PROYECT_MANAGER_BD.helpdesk.Cat_Cargos_Dependencias WHERE Descripcion = @Cargo
            
        END
         PRINT @DepartamentoId         
         PRINT @CargoId

        -- Insertar nuevo usuario
        INSERT INTO PROYECT_MANAGER_BD.[security].Security_Users (Nombres, Estado, Descripcion, Password, Mail, Token, Token_Date, Token_Expiration_Date)
        VALUES (@NombresYApellidos, N'ACTIVO', @NombresYApellidos, N'Mkk/P0lEcGM/az8/Pz98Pw==', @CorreoElectronico, NULL, NULL, NULL)
        SET @UsuarioId = SCOPE_IDENTITY()

        -- Insertar perfil de usuario
        INSERT INTO PROYECT_MANAGER_BD.[security].Tbl_Profile (Nombres, Apellidos, IdUser, Estado)
        VALUES (SUBSTRING(@NombresYApellidos, 1, CHARINDEX(' ', @NombresYApellidos) - 1), SUBSTRING(@NombresYApellidos, CHARINDEX(' ', @NombresYApellidos) + 1, LEN(@NombresYApellidos)), @UsuarioId, N'ACTIVO')
		SET @IPerfil = SCOPE_IDENTITY()
        -- Insertar relación usuario-departamento-cargo
        INSERT INTO PROYECT_MANAGER_BD.helpdesk.Tbl_Dependencias_Usuarios (Id_Perfil, Id_Dependencia, Id_Cargo)
        VALUES (@IPerfil, @DepartamentoId, @CargoId)

        FETCH NEXT FROM usuarioCursor INTO @Departamento, @Cargo, @NombresYApellidos, @CorreoElectronico, @Telefono, @Extension, @Observaciones
    END

    CLOSE usuarioCursor
    DEALLOCATE usuarioCursor
END

DECLARE @JSON2 NVARCHAR(MAX)
SET @JSON2 = '[
    {
      "Departamento": "ALTA VERAPAZ",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "BELEN GARCIA CORDOVA",
      "CorreoElectronico": "bgcordova@mineduc.gob.gt",
      "Telefono": "79510080 y 90",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "ALTA VERAPAZ",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "JULIO ARMANDO SAMAYOA SANTIAGO",
      "CorreoElectronico": "jasamayoa@mineduc.gob.gt",
      "Telefono": "50050606",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "BAJA VERAPAZ",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "EDGAR ROLANDO HERNANDEZ DE LOS SANTOS",
      "CorreoElectronico": "erhernandezs@mineduc.gob.gt",
      "Telefono": "55725877",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "BAJA VERAPAZ",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MAYNOR ROLANDO SANTIAGO RAMIREZ",
      "CorreoElectronico": "mrsantiago@mineduc.gob.gt",
      "Telefono": "30411005",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "CHIMALTENANGO",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "INGRID MARIANELA RUIZ MARROQUIN",
      "CorreoElectronico": "iruizm@mineduc.gob.gt",
      "Telefono": "42168947/77909200",
      "Extension": "308",
      "Observaciones": ""
    },
    {
      "Departamento": "CHIMALTENANGO",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MIRIAM REFUGIO MORALES LUNA",
      "CorreoElectronico": "mrmorales@mineduc.gob.gt",
      "Telefono": "33037171/ 77909200",
      "Extension": "207",
      "Observaciones": ""
    },
    {
      "Departamento": "CHIQUIMULA",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "OLGA ESPERANZA PERDOMO OLIVA",
      "CorreoElectronico": "operdomo@mineduc.gob.gt",
      "Telefono": "79423816",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "CHIQUIMULA",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "IBETTE MABILENA DIAZ MORALES DE BUEZO",
      "CorreoElectronico": "imdiaz@mineduc.gob.gt",
      "Telefono": "40408013",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "ESCUINTLA",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "NELY ZABRINA VELASQUEZ LARIOS",
      "CorreoElectronico": "educescu@mineduc.gob.gt",
      "Telefono": "42447960/79625252 ",
      "Extension": "104",
      "Observaciones": ""
    },
    {
      "Departamento": "ESCUINTLA",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "CLAUDIA LUCRECIA SAGASTUME ORTEGA",
      "CorreoElectronico": "csagastume@mineduc.gob.gt",
      "Telefono": "41497889",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA OCCIDENTE",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "ANA VICTORIA MENEGAZZO SALGUERO",
      "CorreoElectronico": "amenegazzo@mineduc.gob.gt",
      "Telefono": "23145900",
      "Extension": "200",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA OCCIDENTE",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "SÉLVYDO ARIS ACEITUNO GÓMEZ",
      "CorreoElectronico": "saceitunog@mineduc.gob.gt",
      "Telefono": "42744207",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA SUR",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "VILMA LETICIA BALCCARCEL MINCHEZ",
      "CorreoElectronico": "vbalcarcel@mineduc.gob.gt",
      "Telefono": "24628767",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA SUR",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "KENETH ANDERSON ARGUETA RAAEL",
      "CorreoElectronico": "karguetar@mineduc.gob.gt",
      "Telefono": "24628766",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA SUR",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "GUADALUPE ELIZABETH SAGASTUME MOSCOSO",
      "CorreoElectronico": "gsagastume@mineduc.gob.gt",
      "Telefono": "55107746/55311558",
      "Extension": "169",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA NORTE",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "YOHANNA DOLORES OVALLE DE LEÓN",
      "CorreoElectronico": "yovalle@mineduc.gob.gt",
      "Telefono": "22067426/ 30020184",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA NORTE",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "ROSSELLA MARILÚ ESCOBAR FLORES",
      "CorreoElectronico": "rmeflores@mineduc.gob.gt",
      "Telefono": "22067419/30806312",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA ORIENTE",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "LOURDES TAYES GONZALEZ",
      "CorreoElectronico": "itayes@mineduc.gob.gt",
      "Telefono": "22999696",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "GUATEMALA ORIENTE",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "SILVIA EUGENIA VALEZ",
      "CorreoElectronico": "svaldez@mineduc.gob.gt",
      "Telefono": "53894435",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "HUEHUETENANGO",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "ERENDIA GARCIA",
      "CorreoElectronico": "egarciap@mineduc.gob.gt",
      "Telefono": "77904444",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "HUEHUETENANGO",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MIRIAM BERNARDA LOPEZ",
      "CorreoElectronico": "mblopez@mineduc.gob.gt",
      "Telefono": "47960110",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "IZABAL",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "CLAUDIA MORALES/ WALTER MIRANDA",
      "CorreoElectronico": "martha.acosta@mineduc.gob.gt",
      "Telefono": "79488673",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "IZABAL",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MARTHA GLORIDALMA ACOSTA",
      "CorreoElectronico": "omorales@mineduc.gob.gt  wmiranda@mineduc.gob.gt",
      "Telefono": "56947448",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "JALAPA",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "JUAN FRANCISCO CABRERA CORTEZ",
      "CorreoElectronico": "jfcabrera@mineduc.gob.gt",
      "Telefono": "32911299",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "JALAPA",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "DORA MARÍA NAVAS LÓPEZ",
      "CorreoElectronico": "dnavas@mineduc.gog.gt",
      "Telefono": "32908293",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "JUTIAPA",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "ERICKA MARISOL MORALES SALGUERO",
      "CorreoElectronico": "emmorales@mineduc.gob.gt",
      "Telefono": "77908100/ 53041100",
      "Extension": "107",
      "Observaciones": ""
    },
    {
      "Departamento": "JUTIAPA",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MIRNA ELIAS OXLAJ",
      "CorreoElectronico": "mramirez@mineduc.gob.gt",
      "Telefono": "50513810",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "PETEN",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "MIRZA ROSALES",
      "CorreoElectronico": "mrosales@mineduc.gob.gt",
      "Telefono": "79638300/55589114",
      "Extension": "111",
      "Observaciones": ""
    },
    {
      "Departamento": "PETEN",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "ERVIN ARMANDO LÓPEZ",
      "CorreoElectronico": "ealopez@mineduc.gob.gt",
      "Telefono": "40476183",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "QUETZALTENANGO",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "DAFNE PEREZ/ JULIA VIVIANA RECANOJ",
      "CorreoElectronico": "dnperez@mineduc.gob.gt",
      "Telefono": "79621250",
      "Extension": "132",
      "Observaciones": ""
    },
    {
      "Departamento": "QUETZALTENANGO",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MIGUEL ELIAS OXLAJ",
      "CorreoElectronico": "moxlaj@mineduc.gob.gt",
      "Telefono": "59151069",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "QUICHE",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "CAROLINA MATILU LÓPEZ CARDONA/ELSA MARIA CANIL",
      "CorreoElectronico": "omlopezc@mineduc.gob.gt",
      "Telefono": "77906600",
      "Extension": "300",
      "Observaciones": ""
    },
    {
      "Departamento": "QUICHE",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "WILSON SOBERANIZ SOTO",
      "CorreoElectronico": "wsoberaniz@mineduc.gob.gt",
      "Telefono": "58878991/30184679",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "QUICHE NORTE",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "YENIFER MEDINA",
      "CorreoElectronico": "ymedina@mineduc.gob.gt",
      "Telefono": "48230464",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "QUICHE NORTE",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "DOUGLAS HOMERO GUTIERREZ",
      "CorreoElectronico": "dhgutierrez@mineduc.gob.gt",
      "Telefono": "30783148",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "RETALHULEU",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "NO HAY EN ESTE MOMENTO",
      "CorreoElectronico": "",
      "Telefono": "",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "RETALHULEU",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "LIZBETH SURAMA LOPEZ CALVILLO",
      "CorreoElectronico": "lscalvillo@mineduc.gob.gt",
      "Telefono": "56980135",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "SACATEPEQUEZ",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "LISETH MAYARI CASTELLANOS",
      "CorreoElectronico": "lcastellanos@mineduc.gob.gt",
      "Telefono": "79311313",
      "Extension": "213",
      "Observaciones": ""
    },
    {
      "Departamento": "SACATEPEQUEZ",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "EDNA AMARILIS GIRON GARCIA",
      "CorreoElectronico": "eagiron@mineduc.gob.gt",
      "Telefono": "79311313",
      "Extension": "213",
      "Observaciones": ""
    },
    {
      "Departamento": "SACATEPEQUEZ",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "MARIA DEL PILAR GOMEZ VERDUGO",
      "CorreoElectronico": "mgomez@mineduc.gob.gt",
      "Telefono": "79311313",
      "Extension": "201",
      "Observaciones": ""
    },
    {
      "Departamento": "SAN MARCOS",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "MARLEN LUCIA ALPIREZ MALDONADO",
      "CorreoElectronico": "malpirez@mineduc.gob.gt",
      "Telefono": "31295279/79638282",
      "Extension": "119",
      "Observaciones": ""
    },
    {
      "Departamento": "SAN MARCOS",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "VICTOR HUGO LÓPEZ LÓPEZ",
      "CorreoElectronico": "vhlopez@mineduc.gob.gt",
      "Telefono": "42238679",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "SOLOLA",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "ELBA LISBETH EZANON BARRIOS",
      "CorreoElectronico": "ezanon@mineduc.gob.gt",
      "Telefono": "77289903",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "SOLOLA",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "SALVADOR PETZEY CUA",
      "CorreoElectronico": "spetzey@mineduc.gob.gt",
      "Telefono": "59512498/55100690",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "SUCHITEPEQUEZ",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "ITA PERDOMO",
      "CorreoElectronico": "iperdomo@mineduc.gob.gt",
      "Telefono": "78720350",
      "Extension": "102",
      "Observaciones": ""
    },
    {
      "Departamento": "SUCHITEPEQUEZ",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "RUTH GORDILLO OROZCO",
      "CorreoElectronico": "rgordillo@mineduc.gob.gt",
      "Telefono": "56260709",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "TOTONICAPAN",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "JUANA PATRICIA CASTRO LACAN",
      "CorreoElectronico": "jcastrol@mineduc.gob.gt",
      "Telefono": "37644628",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "TOTONICAPAN",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "APOLINARIO LORENZO CHAMARRO IXCAQUIC",
      "CorreoElectronico": "achamorro@mineduc.gob.gt",
      "Telefono": "37643355",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "ZACAPA",
      "Cargo": "ASISTENTE",
      "NombresYApellidos": "ANA MARCELA APARICIO CHACÓN",
      "CorreoElectronico": "aaparicio@mineduc.gob.gt",
      "Telefono": "79578686",
      "Extension": "100",
      "Observaciones": ""
    },
    {
      "Departamento": "ZACAPA",
      "Cargo": "DIRECTOR DEPARTAMENTAL", 
      "NombresYApellidos": "CARLOS ROLANDO GUIROLA",
      "CorreoElectronico": "cguirola@mineduc.gob.gt",
      "Telefono": "37571501",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "DIPLAN",
      "NombresYApellidos": "ENLACE;MONICA CORINA MORALES MARROQUIN",
      "CorreoElectronico": "mcmoralesm@mineduc.gob.gt",
      "Telefono": "24119595",
      "Extension": "1209/3038",
      "Observaciones": ""
    },
    {
      "Departamento": "DIGEPSA",
      "NombresYApellidos": "ENLACE;LAURA ALEJANDRA ALDANA MONTERROSO",
      "CorreoElectronico": "laaldana@mineduc.gob.gt",
      "Telefono": "24119595",
      "Extension": "2030",
      "Observaciones": ""
    },
    {
      "Departamento": "DIGECADE",
      "NombresYApellidos": "ENLACE;LUBIA MAGALY FLORES",
      "CorreoElectronico": "lflores@mineduc.gob.gt",
      "Telefono": "24119595",
      "Extension": "4007",
      "Observaciones": ""
    },
    {
      "Departamento": "DIGEESP",
      "NombresYApellidos": "ENLACE;GRISELDA VIRGINIA MARTINEZ ORTIZ",
      "CorreoElectronico": "gmartinez@mineduc.gob.gt",
      "Telefono": "23623578-68",
      "Extension": "",
      "Observaciones": ""
    },
    {
      "Departamento": "DIGECOR",
      "NombresYApellidos": "ENLACE;ADA MILDRED ALEGRIA MENDEZ",
      "CorreoElectronico": "aalegria@mineduc.gob.gt",
      "Telefono": "24119595",
      "Extension": "2315",
      "Observaciones": ""
    },
    {
      "Departamento": "DIGEFOCE",
      "NombresYApellidos": "ENLACE;LOIDY JANNETH LÓPEZ PÉREZ",
      "CorreoElectronico": "ljlopez@mineduc.gob.gt",
      "Telefono": "24119595",
      "Extension": "4124",
      "Observaciones": ""
    }
  ]'

EXEC CrearUsuariosDesdeJSON @JSON2

DROP PROCEDURE CrearUsuariosDesdeJSON
