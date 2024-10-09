using System.Data;

namespace CAPA_DATOS
{
	public abstract class GDatosAbstract
	{
		public string? Database { get; set; }
		/**
		Esta propiedad abstracta define una conexión a la base de datos. La clase derivada debe implementar esta propiedad para proporcionar una instancia de IDbConnection (por ejemplo, SqlConnection para SQL Server o MySqlConnection para MySQL).
		*/
		/**
		 * Propiedad que devuelve la conexión a la base de datos.
		 * Si existe una conexión previa (MTConnection), la devuelve; de lo contrario, crea una nueva conexión.
		 */
		protected IDbConnection SQLMCon
		{
			get
			{
				if (this.MTConnection != null && this.MTConnection.ConnectionString?.Length > 0)
				{
					return this.MTConnection;
				}
				this.MTConnection = CrearConexion(ConexionString);
				return this.MTConnection;
			}
		}
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
				if (this.MTransaccion?.Connection?.State == ConnectionState.Open)
				{
					this.MTransaccion?.Commit();
				}
				ReStartData();
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
				if (this.MTransaccion.Connection.State == ConnectionState.Open)
				{
					this.MTransaccion.Rollback();
				}
				ReStartData();
			}

		}
		public void BeginGlobalTransaction()
		{
			if (this.globalTransaction) { return; }//TODO ELIMINAR DESPUES
			this.globalTransaction = true;
			this.MTConnection = null;
			this.MTConnection = SQLMCon;
			this.SQLMCon.Open();
			this.MTransaccion = this.MTConnection.BeginTransaction();
		}
		/*public void CommitGlobalTransaction()
		{
			if (this.MTransaccion?.Connection?.State == System.Data.ConnectionState.Open)
			{
				this.globalTransaction = false;
				this.MTransaccion?.Commit();
				ReStartData();
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
			}
			ReStartData();
		}*/

		public void CommitGlobalTransaction()
		{
			if (this.MTransaccion != null)
			{
				try
				{
					this.MTransaccion.Commit();
				}
				catch (Exception ex)
				{
					// Manejar el caso donde la transacción ya no sea válida o haya fallado
					LoggerServices.AddMessageError("Error committing transaction", ex);
					throw;
				}
				finally
				{
					this.MTransaccion = null; // Limpiar después del commit
					this.globalTransaction = false;
					ReStartData();
				}
			}
			else
			{
				// Log o manejar el caso donde la transacción es nula
				//LoggerServices.AddMessageError("Attempted to commit a null transaction.");
			}
		}

		public void RollBackGlobalTransaction()
		{
			if (this.MTransaccion != null)
			{
				try
				{
					this.MTransaccion.Rollback();
				}
				catch (Exception ex)
				{
					// Manejar el caso donde la transacción ya no sea válida o haya fallado
					LoggerServices.AddMessageError("Error rolling back transaction", ex);
					throw;
				}
				finally
				{
					this.MTransaccion = null; // Limpiar después del rollback
					this.globalTransaction = false;
					ReStartData();
				}
			}
			else
			{
				// Log o manejar el caso donde la transacción es nula
				//LoggerServices.AddMessageError("Attempted to rollback a null transaction.");
			}
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
				var command = ComandoSql(strQuery, SQLMCon);
				command.Transaction = this.MTransaccion;
				SetParametersInCommand(parameters, command);
				var scalar = command.ExecuteScalar();
				if (scalar == DBNull.Value)
				{
					return true;
				}
				else
				{
					return Convert.ToInt32(scalar);
				}
			});
		}

		public object? ExcuteSqlQueryWithOutScalar(string? strQuery, List<IDbDataParameter>? parameters = null)
		{			
			try
			{
				SQLMCon.Open();
				var command = ComandoSql(strQuery, SQLMCon);
				command.Transaction = this.MTransaccion;
				SetParametersInCommand(parameters, command);
				var scalar = command.ExecuteNonQuery();
				SQLMCon.Close();
				return true;			
			}
			catch (System.Exception)
			{		
				ReStartData();		
				return false;
			}				
		}

		private void SetParametersInCommand(List<IDbDataParameter>? parameters, IDbCommand command)
		{
			if (parameters != null)
			{
				foreach (var param in parameters)
				{
					command.Parameters.Add(CloneParameter(param));
				}
			}
		}

		private IDbDataParameter? CloneParameter(IDbDataParameter originalParam)
		{
			IDbDataParameter? newParam = (IDbDataParameter?)Activator.CreateInstance(originalParam.GetType());
			foreach (var prop in originalParam.GetType().GetProperties())
			{
				if (prop.CanWrite)
				{
					prop.SetValue(newParam, prop.GetValue(originalParam));
				}
			}
			return newParam;
		}
		// Otros métodos y propiedades existentes

		protected object ExecuteWithRetry(Func<object> operation, int maxRetries = 0)
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
					if (retries >= maxRetries)
					{
						// Log the error and rethrow the exception
						LoggerServices.AddMessageError("ERROR: Max retries reached. Operation failed.", ex);
						this.ReStartData(ex);
						throw;
					}
					// Log the retry attempt
					retries++;
					Console.WriteLine($"read retry query => {retries}");
					// Optionally, add a delay before retrying
					Task.Delay(100).Wait();
					//this.ReStartData(ex);
				}
			}
		}

		/**
		 * Reinicia los datos de conexión y transacción en caso de excepción.
		 * @param ex Excepción que provocó la reinicialización.
		 */
		public void ReStartData(Exception ex)
		{
			ReStartData();
			LoggerServices.AddMessageError("Transaction failed and connection restarted.", ex);
		}
		public void ReStartData()
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
				//VerifyConec();						
				//SQLMCon.Open();
				IDbConnection connection = this.MTransaccion != null ? SQLMCon : CrearConexion(ConexionString);
				var command = ComandoSql(queryString, connection);
				command.Transaction = this.MTransaccion;
				SetParametersInCommand(parameters, command);
				IDataAdapter dataAdapter = CrearDataAdapterSql(command);
				dataAdapter.Fill(ObjDS);
				return ObjDS.Tables.Count > 0 ? ObjDS.Tables[0].Copy() : new DataTable();
			});
		}

		private void VerifyConec()
		{
			// Verifica si la conexión está abierta
			switch (SQLMCon.State)
			{
				case ConnectionState.Closed:
					this.ReStartData(new Exception("reestablecimiento"));
					SQLMCon.Open();
					break;
				case ConnectionState.Open:
					// La conexión ya está abierta, no hay necesidad de hacer nada
					break;
				case ConnectionState.Connecting:
				case ConnectionState.Executing:
				case ConnectionState.Fetching:
					// Esperar a que la conexión se establezca
					while (SQLMCon.State == ConnectionState.Connecting
					|| SQLMCon.State == ConnectionState.Fetching
					|| SQLMCon.State == ConnectionState.Executing)
					{
						Thread.Sleep(100); // Espera breve antes de volver a comprobar
					}
					if (SQLMCon.State == ConnectionState.Closed || SQLMCon.State == ConnectionState.Broken)
					{
						//SQLMCon.Open();
					}
					break;
					// Esperar a que la operación actual se complete
					throw new InvalidOperationException("La conexión está ejecutando una operación. Intente nuevamente más tarde.");
				case ConnectionState.Broken:
					// Cerrar la conexión rota y reabrirla
					SQLMCon.Close();
					SQLMCon.Open();
					break;
				default:
					throw new InvalidOperationException("Estado de conexión desconocido.");
			}
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
