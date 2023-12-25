using System;
using System.Collections.Generic;
using System.Text;

namespace CAPA_DATOS
{
    public class LoggerServices
    {
        public static void AddMessageInfo(string message)
        {            
            Console.WriteLine(message);            
        }
        public static void AddMessageInfoLog(string message)
        {            
            Console.WriteLine(message);
            new LogError
            {
                Fecha = DateTime.Now,               
                message = message,
                ErrorType = ErrorType.INFO.ToString()
            }.Save();
        }
        public static void AddMessageError(string message, Exception ex)
        {
            Console.WriteLine("-- >");
            new LogError
            {
                Fecha = DateTime.Now,
                body = ex,
                message = message,
                ErrorType = ErrorType.ERROR.ToString()
            }.Save();
        }

    }

    public class LogError : EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_Log { get; set; }
        public DateTime? Fecha { get; set; }
        public string? message { get; set; }
        public string? ErrorType { get; set; }
        [JsonProp]
        public Exception? body { get; set; }
    }

    public enum ErrorType
    {
        ERROR, INFO
    }
}
