using System;
using System.Collections.Generic;
using System.Text;

namespace CAPA_DATOS
{
    public class LoggerServices    
    {
        public static void AddMessageInfo(string message){
            Console.WriteLine("-- >");
            Console.WriteLine(message);
        }
        public static void AddMessageError(string message, Exception ex)
        {
            Console.WriteLine("-- >");
            new LogError
            {
                Fecha = DateTime.Now,
                body = ex,
                message = message
            }.Save();
        }

    }

    public class LogError: EntityClass
    {
        [PrimaryKey(Identity = true)]
        public int? Id_Log { get; set; }
        public DateTime? Fecha { get; set; }
        public string? message { get; set; }
        [JsonProp]
        public Exception? body { get; set; }
    }
}
