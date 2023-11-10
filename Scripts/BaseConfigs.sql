-- HELPDESK.dbo.LogError definition

-- Drop table

-- DROP TABLE HELPDESK.dbo.LogError;

CREATE TABLE HELPDESK.dbo.LogError (
	Id_Log int IDENTITY(1,1) NOT NULL,
	Fecha datetime NULL,
	body nvarchar(MAX) COLLATE Modern_Spanish_CI_AS NULL,
	message varchar(MAX) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_CaseTable_Agendas PRIMARY KEY (Id_Log)
);

-- USERSSSSSSSS DATA
-- DROP SCHEMA [security];

CREATE SCHEMA [security];
-- HELPDESK.[security].Security_Permissions definition

-- Drop table

-- DROP TABLE HELPDESK.[security].Security_Permissions;

CREATE TABLE HELPDESK.[security].Security_Permissions (
	Id_Permission int IDENTITY(1,1) NOT NULL,
	Descripcion nvarchar(100) COLLATE Modern_Spanish_CI_AS NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_Security_Permissions PRIMARY KEY (Id_Permission)
);


-- HELPDESK.[security].Security_Roles definition

-- Drop table

-- DROP TABLE HELPDESK.[security].Security_Roles;

CREATE TABLE HELPDESK.[security].Security_Roles (
	Id_Role int IDENTITY(1,1) NOT NULL,
	Descripcion nvarchar(150) COLLATE Modern_Spanish_CI_AS NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_Security_Roles PRIMARY KEY (Id_Role)
);


-- HELPDESK.[security].Security_Users definition

-- Drop table

-- DROP TABLE HELPDESK.[security].Security_Users;

CREATE TABLE HELPDESK.[security].Security_Users (
	Id_User int IDENTITY(1,1) NOT NULL,
	Nombres nvarchar(150) COLLATE Modern_Spanish_CI_AS NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	Descripcion nvarchar(500) COLLATE Modern_Spanish_CI_AS NULL,
	Password nvarchar(500) COLLATE Modern_Spanish_CI_AS NULL,
	Mail nvarchar(150) COLLATE Modern_Spanish_CI_AS NULL,
	Token nvarchar(500) COLLATE Modern_Spanish_CI_AS NULL,
	Token_Date datetime NULL,
	Token_Expiration_Date datetime NULL,
	CONSTRAINT PK_Security_Users PRIMARY KEY (Id_User)
);


-- HELPDESK.[security].Security_Permissions_Roles definition

-- Drop table

-- DROP TABLE HELPDESK.[security].Security_Permissions_Roles;

CREATE TABLE HELPDESK.[security].Security_Permissions_Roles (
	Id_Role int NOT NULL,
	Id_Permission int NOT NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_Security_Permissions_Roles PRIMARY KEY (Id_Role,Id_Permission),
	CONSTRAINT FK_Security_Permissions_Roles_Security_Permissions FOREIGN KEY (Id_Permission) REFERENCES HELPDESK.[security].Security_Permissions(Id_Permission),
	CONSTRAINT FK_Security_Permissions_Roles_Security_Roles FOREIGN KEY (Id_Role) REFERENCES HELPDESK.[security].Security_Roles(Id_Role)
);


-- HELPDESK.[security].Security_Users_Roles definition

-- Drop table

-- DROP TABLE HELPDESK.[security].Security_Users_Roles;

CREATE TABLE HELPDESK.[security].Security_Users_Roles (
	Id_Role int NOT NULL,
	Id_User int NOT NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_Security_Users_Roles PRIMARY KEY (Id_Role,Id_User),
	CONSTRAINT FK_Security_Users_Roles_Security_Roles FOREIGN KEY (Id_Role) REFERENCES HELPDESK.[security].Security_Roles(Id_Role),
	CONSTRAINT FK_Security_Users_Roles_Security_Users FOREIGN KEY (Id_User) REFERENCES HELPDESK.[security].Security_Users(Id_User)
);

-- HELPDESK.helpdesk.Cat_Paises definition

-- Drop table

-- DROP TABLE HELPDESK.helpdesk.Cat_Paises;

CREATE TABLE HELPDESK.helpdesk.Cat_Paises (
	Id_Pais int IDENTITY(1,1) NOT NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	Descripcion nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_Cat_Nacionalidad PRIMARY KEY (Id_Pais)
);
-- HELPDESK.helpdesk.Tbl_Profile definition

-- Drop table

-- DROP TABLE HELPDESK.helpdesk.Tbl_Profile;

CREATE TABLE HELPDESK.helpdesk.Tbl_Profile (
	Id_Perfil int IDENTITY(1,1) NOT NULL,
	Nombres nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	Apellidos nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	FechaNac date NULL,
	IdUser int NULL,
	Sexo nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	Foto nvarchar(MAX) COLLATE Modern_Spanish_CI_AS NULL,
	DNI nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	Correo_institucional nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	Id_Pais_Origen int NULL,
	Estado nvarchar(50) COLLATE Modern_Spanish_CI_AS NULL,
	CONSTRAINT PK_Tbl_InvestigatorProfile PRIMARY KEY (Id_Perfil)
);


-- HELPDESK.helpdesk.Tbl_Profile foreign keys

ALTER TABLE HELPDESK.helpdesk.Tbl_Profile ADD CONSTRAINT FK_Tbl_InvestigatorProfile_Cat_Nacionalidad FOREIGN KEY (Id_Pais_Origen) REFERENCES HELPDESK.helpdesk.Cat_Paises(Id_Pais);
ALTER TABLE HELPDESK.helpdesk.Tbl_Profile ADD CONSTRAINT FK_Tbl_InvestigatorProfile_Security_Users FOREIGN KEY (IdUser) REFERENCES HELPDESK.[security].Security_Users(Id_User);