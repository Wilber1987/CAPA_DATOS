using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
			};
		}
		public static void AddMessageError(string message, Exception ex)
		{
			Console.WriteLine(message, ex);
			try
			{
				new Log
				{
					Fecha = DateTime.Now,
					body = RemoveSpecialCharactersForSql($"Tipo: {ex.GetType().Name},/n/n Mensaje: {ex.Message},/n/n Pila de llamadas:/n/n {ex.StackTrace}"),
					message = RemoveSpecialCharactersForSql(message),
					LogType = LogType.ERROR.ToString()
				};
			}
			catch (System.Exception)
			{}			
		}
		static string RemoveSpecialCharactersForSql(string input)
		{
			// Utilizar una expresión regular para eliminar caracteres especiales para SQL
			return Regex.Replace(input, "[^a-zA-Z0-9]", " ");
		}

		static string RemoveSpecialCharactersForJson(string input)
		{
			// Utilizar una expresión regular para eliminar caracteres especiales para JSON
			return Regex.Replace(input, "[^a-zA-Z0-9,.{}\":]", "");
		}

		static string RemoveQuotes(string input)
		{
			// Eliminar comillas simples
			string withoutSingleQuotes = input.Replace("'", string.Empty);

			// Eliminar comillas dobles
			string withoutDoubleQuotes = withoutSingleQuotes.Replace("\"", string.Empty);

			return withoutDoubleQuotes;
		}

	}

	public class Log : EntityClass
	{
		[PrimaryKey(Identity = true)]
		public int? Id_Log { get; set; }
		public DateTime? Fecha { get; set; }
		public string? message { get; set; }
		public string? LogType { get; set; }
		public  string? body { get; set; }
	}

	public enum LogType
	{
		ERROR, INFO
	}
}
