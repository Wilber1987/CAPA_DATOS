﻿using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAPA_DATOS
{
	public abstract class GDatosAbstract
	{
		/**
		Esta propiedad abstracta define una conexión a la base de datos. La clase derivada debe implementar esta propiedad para proporcionar una instancia de IDbConnection (por ejemplo, SqlConnection para SQL Server o MySqlConnection para MySQL).
		*/
		protected abstract IDbConnection SQLMCon { get; }
		/**
		Esta variable almacena la cadena de conexión a la base de datos. Debe ser inicializada por la clase derivada antes de usarla para establecer la conexión.
		*/
		protected String? ConexionString;
		/**
		Esta variable representa una transacción de base de datos. Puede ser nula si no se está utilizando una transacción.
		*/
		protected IDbTransaction? MTransaccion;
		/**
		Esta variable indica si se está utilizando una transacción global (que abarca múltiples operaciones) o no.
		*/
		protected bool globalTransaction;
		/**
		Esta variable representa otra conexión de base de datos. Al igual que MTransaccion, puede ser nula si no se está utilizando.
		*/
		protected IDbConnection? MTConnection;
		/**
		Este método abstracto debe ser implementado por las clases derivadas para crear y devolver una instancia de IDbConnection utilizando la cadena de conexión proporcionada.
		*/
		protected abstract IDbConnection CrearConexion(string cadena);
		/**
		Este método abstracto crea un objeto IDbCommand (por ejemplo, SqlCommand o MySqlCommand) para ejecutar una consulta SQL en la base de datos.
		*/
		protected abstract IDbCommand ComandoSql(string comandoSql, IDbConnection connection);
		/*
		Este método abstracto crea un objeto IDataAdapter (por ejemplo, SqlDataAdapter o MySqlDataAdapter) para llenar un DataSet con los resultados de una consulta SQL.		
		*/
		protected abstract IDataAdapter CrearDataAdapterSql(string comandoSql, IDbConnection connection);
		/*
		Similar al método anterior, pero crea un IDataAdapter a partir de un objeto IDbCommand.
		*/
		protected abstract IDataAdapter CrearDataAdapterSql(IDbCommand comandoSql);
		/*
		Este método abstracto ejecuta un procedimiento almacenado o función en la base de datos y devuelve un objeto como resultado.
		*/
		public abstract object ExecuteProcedure(object Inst, List<object> Params);
		/*
		Similar al método anterior, pero devuelve un DataTable con los resultados.
		*/
		public abstract DataTable ExecuteProcedureWithSQL(object Inst, List<object> Params);

		#region ADO.NET METHODS
		/**
		* Método para probar la conexión a la base de datos.
		* Devuelve verdadero si la conexión es exitosa, de lo contrario, lanza una excepción.
		*/
		public bool TestConnection()
		{
			try
			{
				SQLMCon.Open();
				SQLMCon.Close();
				return true;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public void BeginTransaction()
		{
			if (this.globalTransaction)
			{
				return;
			}
			//this.MTConnection = null;
			LoggerServices.AddMessageInfo("-- > BEGIN TRANSACTION <=================");
			//MTConnection = SQLMCon;
			if (SQLMCon.State == ConnectionState.Closed)
			{
				SQLMCon.Open();
			}
			this.MTransaccion = SQLMCon.BeginTransaction();
		}
		public void CommitTransaction()
		{
			if (this.globalTransaction)
			{
				return;
			}
			else if (this.MTransaccion != null && this.MTransaccion.Connection != null)
			{
				LoggerServices.AddMessageInfo("-- > COMMIT TRANSACTION <=================");
				if (this.MTransaccion?.Connection?.State == ConnectionState.Open)
				{
					this.MTransaccion?.Commit();
				}
				SQLMCon.Close();
				this.MTransaccion = null;
				//MTConnection = null;
			}
		}
		public void RollBackTransaction()
		{
			if (this.globalTransaction)
			{
				return;
			}
			if (this.MTransaccion != null && this.MTransaccion.Connection != null)
			{
				LoggerServices.AddMessageInfo("-- > ROLLBACK TRANSACTION <=================");
				if (this.MTransaccion.Connection.State == ConnectionState.Open)
				{
					this.MTransaccion.Rollback();
				}
				SQLMCon.Close();
				this.MTransaccion = null;
			}

		}
		public void BeginGlobalTransaction()
		{
			this.globalTransaction = true;
			this.MTConnection = null;
			this.MTConnection = SQLMCon;
			this.SQLMCon.Open();
			this.MTransaccion = this.MTConnection.BeginTransaction();
			LoggerServices.AddMessageInfo("-- > BEGIN GLOBAL TRANSACTION <=================");
		}
		public void CommitGlobalTransaction()
		{
			if (this.MTransaccion?.Connection?.State == System.Data.ConnectionState.Open)
			{
				this.globalTransaction = false;
				this.MTransaccion?.Commit();
				this.SQLMCon?.Close();
				MTConnection = null;
				LoggerServices.AddMessageInfo("-- > COMMIT GLOBAL TRANSACTION <=================");
			}
		}
		public void RollBackGlobalTransaction()
		{
			if (this.MTransaccion != null)
			{
				if (this.MTransaccion?.Connection?.State == System.Data.ConnectionState.Open)
				{
					this.MTransaccion.Rollback();
					this.SQLMCon.Close();
				}
				LoggerServices.AddMessageInfo("-- > ROLLBACK GLOBAL TRANSACTION <=================");
			}
			this.MTConnection = null;
		}

		/**
		* Método para ejecutar una consulta SQL en la base de datos.
		* Devuelve el resultado de la consulta o lanza una excepción en caso de error.
		* @param strQuery Consulta SQL a ejecutar.
		* @param parameters Lista de parámetros (opcional) para la consulta.
		* @return El resultado de la consulta o verdadero si no hay resultados.
		*/
		public object? ExcuteSqlQuery(string? strQuery, List<IDbDataParameter>? parameters = null)
		{
			/*try
			{*/
			return ExecuteWithRetry(() =>
			{
				using (var command = ComandoSql(strQuery, SQLMCon))
				{
					command.Transaction = this.MTransaccion;

					if (parameters != null)
					{
						foreach (var param in parameters)
						{
							command.Parameters.Add(param);
						}
					}
					var scalar = command.ExecuteScalar();
					if (scalar == DBNull.Value)
					{
						return true;
					}
					else
					{
						return Convert.ToInt32(scalar);
					}
				}
			});
		}
		// Otros métodos y propiedades existentes

		protected object ExecuteWithRetry(Func<object> operation, int maxRetries = 10)
		{
			int retries = 0;
			while (true)
			{
				try
				{
					return operation();
				}
				catch (Exception ex)
				{
					this.ReStartData(ex);
					if (retries >= maxRetries)
					{
						// Log the error and rethrow the exception
						LoggerServices.AddMessageError("ERROR: Max retries reached. Operation failed.", ex);
						throw;
					}
					// Log the retry attempt
					LoggerServices.AddMessageInfo($"Retry attempt {retries + 1} after error: {ex.Message}");
					retries++;
					// Optionally, add a delay before retrying
					Task.Delay(1000).Wait();
				}
			}
		}

		/**
		 * Reinicia los datos de conexión y transacción en caso de excepción.
		 * @param ex Excepción que provocó la reinicialización.
		 */
		public void ReStartData(Exception ex)
		{
			if (this.MTransaccion?.Connection?.State == System.Data.ConnectionState.Open)
			{
				this.MTransaccion.Rollback();
			}
			if (this.SQLMCon?.State == System.Data.ConnectionState.Open)
			{
				this.SQLMCon.Close();
			}
			globalTransaction = false;
			this.MTConnection = null;
			this.MTransaccion = null;
			LoggerServices.AddMessageError("Transaction failed and connection restarted.", ex);
		}

		/**
		 * Ejecuta una consulta SQL y devuelve los resultados en un DataTable.
		 * @param queryString Consulta SQL a ejecutar.
		 * @return DataTable con los resultados de la consulta.
		 */
		public DataTable TraerDatosSQL(string queryString, List<IDbDataParameter>? parameters = null)
		{
			return (DataTable)ExecuteWithRetry(() =>
			{
				DataSet ObjDS = new DataSet();
				using (var command = ComandoSql(queryString, SQLMCon))
				{
					command.Transaction = this.MTransaccion;

					if (parameters != null)
					{
						foreach (var param in parameters)
						{
							command.Parameters.Add(param);
						}
					}
					CrearDataAdapterSql(command).Fill(ObjDS);
					return ObjDS.Tables.Count > 0 ? ObjDS.Tables[0].Copy() : new DataTable();
				}
			});
		}
		/**
		 * Ejecuta una consulta SQL y devuelve los resultados en un DataTable.
		 * @param Command Comando SQL a ejecutar.
		 * @return DataTable con los resultados de la consulta.
		 */
		public DataTable TraerDatosSQL(IDbCommand Command)
		{
			return (DataTable)ExecuteWithRetry(() =>
			{
				DataSet ObjDS = new DataSet();
				Command.Transaction = this.MTransaccion;
				CrearDataAdapterSql(Command).Fill(ObjDS);
				return ObjDS.Tables[0].Copy();
			});
		}
		#endregion

		/*
		* Utlizado para la lectura de los datos (Esta variante sirve para cuando ya viene el string SQL)
		Este método BuildTablePaginated se encarga de construir una tabla de datos paginada basada en las consultas SQL directas proporcionadas 
		(queryString para los datos paginados y queryCount para contar el número total de registros).
		Este método es utilizado internamente para obtener tanto los datos paginados como el número total de registros que coinciden con una consulta. 
		Registra la consulta SQL de los datos paginados como un mensaje de información antes de ejecutarla. Luego, ejecuta las consultas SQL para obtener 
		los datos paginados y el número total de registros. Si ocurre algún error durante el proceso, registra el error y relanza la excepción
		para ser manejada en niveles superiores.
		*/
		public (DataTable, int) BuildTablePaginated(string queryString, string queryCount, List<IDbDataParameter>? parameters)
		{
			try
			{
				// Registra un mensaje de información con la consulta SQL de los datos paginados
				LoggerServices.AddMessageInfo(queryString);

				// Ejecuta la consulta SQL para obtener los datos paginados
				DataTable Table = TraerDatosSQL(queryString, parameters);

				// Ejecuta la consulta SQL para contar el número total de registros
				int totalRecords = TraerDatosSQL(queryCount, parameters).Rows.Count;

				// Retorna una tupla con la tabla de datos paginados y el número total de registros
				return (Table, totalRecords);
			}
			catch (Exception e)
			{
				// Si ocurre un error durante el proceso, registra el error y relanza la excepción
				this.ReStartData(e);
				LoggerServices.AddMessageError($"ERROR: BuildTablePaginated {queryString}", e);
				throw;
			}
		}
	}

}
