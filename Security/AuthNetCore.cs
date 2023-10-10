using CAPA_DATOS;
using CAPA_DATOS.Services;
using CAPA_DATOS.Security;

namespace API.Controllers
{
    public class AuthNetCore
    {
        static public bool AuthAttribute = false;
        static public bool Authenticate(string idetify)
        {
            var security_User = SeasonServices.Get<Security_Users>("loginIn", idetify);
            if (SqlADOConexion.SQLM == null || SqlADOConexion.Anonimo || security_User == null)
            {
                //security_User = null;
                //SqlADOConexion.SQLM = null;
                return false;
            }
            return true;

        }
        static public bool AnonymousAuthenticate()
        {
            SqlADOConexion.IniciarConexionAnonima();
            return true;
        }
        static public object loginIN(string? mail, string? password, string idetify)
        {
            if (mail == null || mail.Equals("") || password == null || password.Equals(""))
                return new UserModel()
                {
                    success = false,
                    message = "Usuario y contraseña son requeridos.",
                    status = 500
                };
            try
            {
                SqlADOConexion.IniciarConexion();
                var security_User = new Security_Users()
                {
                    Mail = mail,
                    Password = EncrypterServices.Encrypt(password)
                }.GetUserData();
                if (security_User == null) ClearSeason();
                SeasonServices.Set("loginIn", security_User, idetify);
                return User(idetify);
            }
            catch (Exception ex)
            {
                Console.WriteLine("-- > :" + ex);
                return new UserModel()
                {
                    success = false,
                    message = "Error al intentar iniciar sesión, favor intentarlo mas tarde, o contactese con nosotros.",
                    status = 500
                };
            }
        }
        static public bool ClearSeason()
        {
            //SqlADOConexion.SQLM = null;
            //security_User = null;
            //SeasonServices.ClearSeason(idetify);
            return true;

        }
        public static UserModel User(string identfy)
        {
            var security_User = SeasonServices
                .Get<Security_Users>("loginIn", identfy);
            if (security_User != null)
            {
                return new UserModel()
                {
                    UserId = security_User.Id_User,
                    mail = security_User.Mail,
                    UserData = security_User,
                    password = "PROTECTED",
                    status = 200,
                    success = true,
                    isAdmin = security_User.IsAdmin(),
                    message = "Inicio de sesión exitoso."
                };
            }
            else
            {
                return new UserModel()
                {
                    UserId = 0,
                    mail = "FAILD",
                    password = "FAILD",
                    status = 500,
                    success = false,
                    message = "Usuario o contraseña incorrectos."
                };
            }
        }
        public static UserModel User()
        {
            var security_User = SeasonServices
                .Get<Security_Users>("loginIn", "identfy");
            if (security_User != null)
            {
                return new UserModel()
                {
                    UserId = security_User.Id_User,
                    mail = security_User.Mail,
                    UserData = security_User,
                    password = "PROTECTED",
                    status = 200,
                    success = true,
                    message = "Inicio de sesión exitoso."
                };
            }
            else
            {
                return new UserModel()
                {
                    UserId = 0,
                    mail = "FAILD",
                    password = "FAILD",
                    status = 500,
                    success = false,
                    message = "Usuario o contraseña incorrectos."
                };
            }
        }
        public static bool HaveRole(string role, string identfy)
        {
            var security_User = User(identfy).UserData;
            if (Authenticate(identfy))
            {
                var AdminRole = security_User?.Security_Users_Roles?.Where(r => r?.Security_Role?.Descripcion == role).ToList();
                if (AdminRole?.Count != 0) return true;
                return false;
            }
            else
            {
                return false;
            }
        }
        public static bool HavePermission(string permission, string identfy)
        {
            var security_User = User(identfy).UserData;
            var isAdmin = security_User?.Security_Users_Roles?.Where(r => RoleHavePermission(PermissionsEnum.ADMIN_ACCESS.ToString(), r)?.Count != 0).ToList();
            if (isAdmin?.Count != 0) return true;
            if (Authenticate(identfy))
            {
                var roleHavePermision = security_User?.Security_Users_Roles?.Where(r => RoleHavePermission(permission, r)?.Count != 0).ToList();
                if (roleHavePermision?.Count != 0) return true;
                return false;
            }
            else
            {
                return false;
            }
        }
        private static List<Security_Permissions_Roles>? RoleHavePermission(string permission, Security_Users_Roles? r)
        {
            return r?.Security_Role?.Security_Permissions_Roles?.Where(p => p.Security_Permissions?.Descripcion == permission).ToList();
        }

        public static UserModel RecoveryPassword(string? mail)
        {
            if (mail == null || mail.Equals(""))
            {
                return new UserModel()
                {
                    success = false,
                    message = "Usuario y contraseña son requeridos.",
                    status = 500
                };
            }
            try
            {
                SqlADOConexion.IniciarConexion();
                var security_User = new Security_Users()
                {
                    Mail = mail
                }.RecoveryPassword();
                if (security_User != null)
                {
                    return new UserModel()
                    {
                        success = true,
                        message = "Contraseña enviada por correo",
                        status = 200
                    };
                }
                else
                {
                    return new UserModel()
                    {
                        success = false,
                        message = "El usuario no existe",
                        status = 500
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("-- > :" + ex);
                return new UserModel()
                {
                    success = false,
                    message = "Error al intentar recuperar la contraseña, favor intentarlo mas tarde, o contactese con nosotros.",
                    status = 500
                };
            }
        }
    }
    public class UserModel
    {
        public int? UserId { get; set; }
        public int? status { get; set; }
        public string? mail { get; set; }
        public string? password { get; set; }
        public string? message { get; set; }
        public bool? success { get; set; }
        public bool isAdmin { get; set; }
        public Security_Users? UserData { get; set; }
    }
}
