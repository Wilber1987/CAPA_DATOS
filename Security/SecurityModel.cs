﻿using API.Controllers;
using CAPA_DATOS.Services;


namespace CAPA_DATOS.Security
{
    public class Security_Roles : EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_Role { get; set; }
        public string? Descripcion { get; set; }
        public string? Estado { get; set; }
        [OneToMany(TableName = "Security_Permissions_Roles", KeyColumn = "Id_Role", ForeignKeyColumn = "Id_Role")]
        public List<Security_Permissions_Roles>? Security_Permissions_Roles { get; set; }
        public object SaveRole()
        {
            if (this.Id_Role == null)
            {
                this.Save();
            }
            else
            {
                this.Update("Id_Role");
            }
            if (this.Security_Permissions_Roles != null)
            {
                Security_Permissions_Roles IdI = new Security_Permissions_Roles();
                IdI.Id_Role = this.Id_Role;
                IdI.Delete();
                foreach (Security_Permissions_Roles obj in this.Security_Permissions_Roles)
                {
                    obj.Id_Role = this.Id_Role;
                    obj.Save();
                }
            }
            return this;
        }
        public object GetRolData()
        {
            this.Security_Permissions_Roles = new Security_Permissions_Roles()
            {
                Id_Role = this.Id_Role
            }.Get<Security_Permissions_Roles>();
            return this.Security_Permissions_Roles;

        }
        public List<Security_Roles>? GetRoles()
        {
            var roles = this.Get<Security_Roles>();
            foreach (Security_Roles role in roles)
            {
                role.GetRolData();
            }
            return roles;
        }
    }
    public class Security_Users : EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_User { get; set; }
        public string? Nombres { get; set; }
        public string? Estado { get; set; }
        public string? Descripcion { get; set; }
        public string? Password { get; set; }
        public string? Mail { get; set; }
        public string? Token { get; set; }
        public DateTime? Token_Date { get; set; }
        public DateTime? Token_Expiration_Date { get; set; }
        [OneToMany(TableName = "Security_Users_Roles", KeyColumn = "Id_User", ForeignKeyColumn = "Id_User")]
        public List<Security_Users_Roles>? Security_Users_Roles { get; set; }

        private int myVar;

        public int MyProperty
        {
            get { return myVar; }
            set { myVar = value; }
        }
        public Tbl_Profile? Tbl_Profile { get; set; }

        public Tbl_Profile? Get_Profile()
        {
            if (Tbl_Profile == null)
            {
                Tbl_Profile = new Tbl_Profile { IdUser = Id_User }.Find<Tbl_Profile>();
            }
            return Tbl_Profile;
        }
        public Security_Users? GetUserData()
        {
            Security_Users? user = this.Find<Security_Users>();
            if (user != null && user.Estado == "ACTIVO")
            {
                user.Security_Users_Roles = new Security_Users_Roles()
                {
                    Id_User = user.Id_User
                }.Get<Security_Users_Roles>();
                foreach (Security_Users_Roles role in user.Security_Users_Roles ?? new List<Security_Users_Roles>())
                {
                    role.Security_Role?.GetRolData();
                }
                return user;
            }
            if (user?.Estado == "INACTIVO")
            {
                throw new Exception("usuario inactivo");
            }
            return null;
        }
        public object SaveUser(string identity)
        {
            if (!AuthNetCore.HavePermission(Permissions.ADMINISTRAR_USUARIOS.ToString(), identity))
            {
                throw new Exception("no tiene permisos");
            }
            return Save_User(new Tbl_Profile()
            {
                Nombres = this.Nombres,
                Estado = this.Estado,
                Correo_institucional = this.Mail,
                Foto = "\\Media\\profiles\\avatar.png"
            });

        }

        public object Save_User(Tbl_Profile? tbl_Profile)
        {
            try
            {
                this.BeginGlobalTransaction();
                if (this.Password != null)
                {
                    this.Password = EncrypterServices.Encrypt(this.Password);
                }
                if (this.Id_User == null)
                {
                    if (new Security_Users() { Mail = this.Mail }.Exists<Security_Users>())
                    {
                        throw new Exception("Correo en uso");
                    }
                    var user = Save();
                    if (tbl_Profile != null)
                    {
                        tbl_Profile.IdUser = ((Security_Users?)user)?.Id_User;
                        tbl_Profile.Save();
                    }

                }
                else
                {
                    if (this.Estado == null)
                    {
                        this.Estado = "ACTIVO";
                    }
                    this.Update("Id_User");
                }
                if (this.Security_Users_Roles != null)
                {
                    Security_Users_Roles IdI = new Security_Users_Roles();
                    IdI.Id_User = this.Id_User;
                    IdI.Delete();
                    foreach (Security_Users_Roles obj in this.Security_Users_Roles)
                    {
                        obj.Id_User = this.Id_User;
                        obj.Save();
                    }
                }
                this.CommitGlobalTransaction();
                return this;
            }
            catch (System.Exception)
            {
                this.RollBackGlobalTransaction();
                throw;
            }
        }

        public object GetUsers()
        {
            var Security_Users_List = this.Where<Security_Users>(FilterData.Limit(30));
            foreach (Security_Users User in Security_Users_List)
            {
                User.Security_Users_Roles =
                    new Security_Users_Roles().Get_WhereIN<Security_Users_Roles>(
                         "Id_User", [User.Id_User.ToString()]);
            }
            return Security_Users_List;
        }
        internal bool IsAdmin()
        {
            return Security_Users_Roles?.Find(r => r.Security_Role?.Security_Permissions_Roles?.Find(p =>
             p.Security_Permissions.Descripcion.Equals(Permissions.ADMIN_ACCESS.ToString())
            ) != null) != null;
        }
        internal object RecoveryPassword()
        {
            Security_Users? user = this.Find<Security_Users>();
            if (user != null && user.Estado == "ACTIVO")
            {
                string password = Guid.NewGuid().ToString();
                user.Password = EncrypterServices.Encrypt(password);
                user.Update();
                _ = SMTPMailServices.SendMail("support@password.recovery",
                 [user.Mail],
                 "Recuperación de contraseña",
                 $"nueva contraseña: {password}", null, null, null);
                return user;
            }
            if (user?.Estado == "INACTIVO")
            {
                throw new Exception("usuario inactivo");
            }
            return null;
        }

        public object? changePassword(string? identfy)
        {
            var security_User = AuthNetCore.User(identfy).UserData;
            Password = EncrypterServices.Encrypt(Password);
            Id_User = security_User?.Id_User;
            return Update();
        }
    }

    public class Tbl_Profile : EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_Perfil { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public DateTime? FechaNac { get; set; }
        public int? IdUser { get; set; }
        public string? Sexo { get; set; }
        public string? Foto { get; set; }
        public string? DNI { get; set; }
        public string? Correo_institucional { get; set; }
        public string? Estado { get; set; }

        public string GetNombreCompleto()
        {
            return $"{Nombres} {Apellidos}";
        }
    }
    public class Security_Permissions : EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_Permission { get; set; }
        public string? Descripcion { get; set; }
        public string? Detalles { get; set; }
        public string? Estado { get; set; }
    }
    public class Security_Permissions_Roles : EntityClass
    {
        [PrimaryKey(Identity = false)]
        public int? Id_Role { get; set; }
        [PrimaryKey(Identity = false)]
        public int? Id_Permission { get; set; }
        public string? Estado { get; set; }
        [ManyToOne(TableName = "Security_Permissions", KeyColumn = "Id_Permission", ForeignKeyColumn = "Id_Permission")]
        public Security_Permissions? Security_Permissions { get; set; }
    }
    public class Security_Users_Roles : EntityClass
    {
        [PrimaryKey(Identity = false)]
        public int? Id_Role { get; set; }
        [PrimaryKey(Identity = false)]
        public int? Id_User { get; set; }
        public string? Estado { get; set; }
        [ManyToOne(TableName = "Security_Role", KeyColumn = "Id_Role", ForeignKeyColumn = "Id_Role")]
        public Security_Roles? Security_Role { get; set; }


    }
}
