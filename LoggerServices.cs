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
            new Log
            {
                Fecha = DateTime.Now,               
                message = message,
                LogType = LogType.INFO.ToString()
            }.Save();
        }
        public static void AddMessageError(string message, Exception ex)
        {
            Console.WriteLine("-- >");
            new Log
            {
                Fecha = DateTime.Now,
                body = $"Tipo: {ex.GetType().Name},/n Mensaje: {ex.Message},/n Pila de llamadas:/n {ex.StackTrace}",
                message = message,
                LogType = LogType.ERROR.ToString()
            }.Save();
        }

    }

    public class Log : EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_Log { get; set; }
        public DateTime? Fecha { get; set; }
        public string? message { get; set; }
        public string? LogType { get; set; }
        [JsonProp]
        public Object? body { get; set; }
    }

    public enum LogType
    {
        ERROR, INFO
    }
}
