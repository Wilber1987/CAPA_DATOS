using System.Collections;
using System.Data;
using System.Reflection;
using CAPA_DATOS.BDCore;
using CAPA_DATOS.BDCore.Abstracts;
using CAPA_DATOS.BDCore.MySqlImplementations;
using CAPA_DATOS.BDCore.PostgresImplementations;
using CAPA_DATOS.BDCore.SQLServerImplementations;

namespace CAPA_DATOS;
// Clase abstracta base para todas las entidades del sistema
public abstract class EntityClass : TransactionalClass
{
	// Lista de filtros de datos que pueden aplicarse a las consultas de la entidad
	public List<FilterData>? filterData { get; set; }
	public List<OrdeData>? orderData { get; set; }

	// Método para obtener una lista de entidades que cumplen cierta condición
	public List<T> Get<T>(string condition = "")
	{
		// Llama al método TakeList de MTConnection para obtener datos
		var Data = MTConnection?.TakeList<T>(this, true, condition);
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Data.ToList() ?? new List<T>();
	}

	// Método para obtener una lista de todas las entidades
	public List<T> GetAll<T>()
	{
		// Llama al método TakeList de MTConnection sin condiciones para obtener todos los datos
		var Data = MTConnection?.TakeList<T>(this, true);
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Data.ToList() ?? new List<T>();
	}

	// Método para filtrar una lista de entidades según una o más condiciones
	public List<T> Where<T>(params FilterData[] where_condition)
	{
		// Verifica si alguna condición de filtro tiene valores nulos o vacíos
		if (where_condition.Where(c => c.FilterType != "or" 
		&& c.FilterType != "and"
		&& c.FilterType != "Not Null"
		&& c.FilterType != "NotNull"
		&& c.FilterType != "IsNull"
		&& c.FilterType != "Is Null"
		&& (c.Values == null || c.Values?.Count == 0)).ToList().Count > 0)
		{
			// Retorna una lista vacía si alguna condición no está definida correctamente
			return new List<T>();
		}

		// Si no hay problemas con las condiciones, se agregan al filtro de datos de la entidad
		if (filterData == null)
			filterData = new List<FilterData>();

		filterData.AddRange(where_condition.ToList());

		// Se obtienen los datos utilizando el filtro actualizado
		var Data = MTConnection?.TakeList<T>(this, true);
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Data ?? new List<T>();
	}

	// Método para obtener una lista de entidades donde un campo esté dentro de un conjunto de valores
	public List<T> Get_WhereIN<T>(string Field, string?[]? conditions)
	{
		// Construye una condición IN a partir de los valores proporcionados
		string condition = BuildArrayIN(conditions);
		// Obtiene los datos utilizando la condición IN especificada
		var Data = MTConnection?.TakeList<T>(this, true, Field + " IN (" + condition + ")");
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Data ?? new List<T>();
	}

	// Método para obtener una lista de entidades donde un campo no esté dentro de un conjunto de valores
	public List<T> Get_WhereNotIN<T>(string Field, string[] conditions)
	{
		// Construye una condición NOT IN a partir de los valores proporcionados
		string condition = BuildArrayIN(conditions);
		// Obtiene los datos utilizando la condición NOT IN especificada
		var Data = MTConnection?.TakeList<T>(this, true, Field + " NOT IN (" + condition + ")");
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Data ?? new List<T>();
	}

	// Método para encontrar una entidad que cumpla ciertas condiciones
	public T? Find<T>(params FilterData[]? where_condition)
	{
		// Establece los filtros de datos de la entidad
		filterData = where_condition?.ToList();
		// Intenta obtener la entidad utilizando los filtros establecidos
		var Data = SqlADOConexion.SQLM != null ? SqlADOConexion.SQLM.TakeObject<T>(this) : default(T);
		// Retorna la entidad encontrada o null si no se encuentra
		return Data;
	}

	// Método para verificar si existen entidades que cumplan ciertas condiciones
	public Boolean Exists<T>()
	{
		// Obtiene los datos utilizando la entidad actual
		var Data = MTConnection?.TakeList<T>(this, true);
		// Retorna true si se encuentran datos, false si no se encuentran
		return Data?.Count > 0;
	}
	public Boolean Exists()
	{
		Type entityType = this.GetType();
		PropertyInfo[] lst = this.GetType().GetProperties();
		// Filtra las propiedades que son claves primarias y tienen valores no nulos
		var pkProperties = lst.Where(p => (PrimaryKey?)Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
		var values = pkProperties.Where(p => p.GetValue(this) != null).ToList();
		// Si el número de propiedades de clave primaria coincide con las que tienen valores, realiza la actualización
		if (pkProperties.Count == values.Count)
		{
			var newInstance = Activator.CreateInstance(entityType);
			// Establecer los valores de clave primaria en la nueva instancia
			foreach (var property in pkProperties)
			{
				var value = property.GetValue(this);
				property.SetValue(newInstance, value);
			}
			// Obtener el método TakeList y llamarlo con la nueva instancia
			var method = typeof(WDataMapper).GetMethod("TakeList").MakeGenericMethod(entityType);
			var data = method.Invoke(MTConnection, new object[] { newInstance, true }) as IList;
			// Retornar verdadero si se encuentran datos, falso si no se encuentran
			return data?.Count > 0;
		}
		return false;
		// Retorna true si se encuentran datos, false si no se encuentran
		//return Data?.Count > 0;
	}

	// Método para obtener una lista de entidades sin aplicar transacción
	public List<T> SimpleGet<T>()
	{
		// Obtiene los datos sin aplicar transacción
		var Data = MTConnection?.TakeList<T>(this, false);
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Data ?? new List<T>();
	}

	// Método estático que representa un método de punto final para operaciones genéricas
	public static List<T> EndpointMethod<T>()
	{
		// Crea una lista vacía y la retorna
		List<T> list = new List<T>();
		return list;
	}

	// Método privado para construir una condición IN a partir de un conjunto de valores
	private static string BuildArrayIN(string?[]? conditions)
	{
		// Construye la condición IN separando los valores por comas
		string condition = "";
		foreach (string? Value in conditions ?? new string?[0])
		{
			condition = condition + Value + ",";
		}
		condition = condition.TrimEnd(',');
		// Si no hay valores, retorna -1
		if (condition == "")
		{
			return "-1";
		}
		return condition;
	}

	// Método para guardar una entidad en la base de datos
	public object? Save()
	{
		try
		{
			// Inicia una transacción
			MTConnection?.GDatos.BeginTransaction();
			// Inserta la entidad en la base de datos y obtiene el resultado
			var result = MTConnection?.InsertObject(this);
			// Confirma la transacción
			MTConnection?.GDatos.CommitTransaction();
			// Retorna el resultado de la operación de guardado
			return result;
		}
		catch (Exception e)
		{
			// Revierte la transacción en caso de error y registra el error
			MTConnection?.GDatos.RollBackTransaction();
			LoggerServices.AddMessageError("ERROR: Save entity", e);
			throw;
		}
	}

	// Método para actualizar una entidad en la base de datos
	public object? Update()
	{
		try
		{
			// Obtiene todas las propiedades de la entidad
			PropertyInfo[] lst = this.GetType().GetProperties();
			// Filtra las propiedades que son claves primarias y tienen valores no nulos
			var pkPropiertys = lst.Where(p => (PrimaryKey?)Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
			var values = pkPropiertys.Where(p => p.GetValue(this) != null).ToList();
			// Si el número de propiedades de clave primaria coincide con las que tienen valores, realiza la actualización
			if (pkPropiertys.Count == values.Count)
			{
				// Llama al método Update sobrecarg ado con los nombres de las propiedades de clave primaria
				this.Update(pkPropiertys.Select(p => p.Name).ToArray());
				// Retorna un mensaje de éxito
				return new ResponseService()
				{
					status = 200,
					message = this.GetType().Name + " actualizado correctamente"
				};
			}
			// Si no se encuentran todas las propiedades de clave primaria con valores, retorna un mensaje de error
			else
				return new ResponseService()
				{
					status = 500,
					message = "Error al actualizar: no se encuentra el registro " + this.GetType().Name
				};
		}
		catch (Exception e)
		{
			// Registra cualquier error que ocurra durante la actualización
			LoggerServices.AddMessageError("ERROR: Update entity", e);
			return new ResponseService()
			{
				status = 500,
				message = "Error al actualizar: " + e.Message
			};
		}
	}

	// Método para actualizar una entidad en la base de datos utilizando el identificador proporcionado
	public bool Update(string Id)
	{
		try
		{
			// Inicia una transacción
			MTConnection?.GDatos.BeginTransaction();
			// Actualiza la entidad en la base de datos utilizando el identificador proporcionado
			MTConnection?.UpdateObject(this, Id);
			// Confirma la transacción
			MTConnection?.GDatos.CommitTransaction();
			// Retorna verdadero para indicar que la operación fue exitosa
			return true;
		}
		catch (Exception e)
		{
			// Revierte la transacción en caso de error y registra el error
			MTConnection?.GDatos.RollBackTransaction();
			LoggerServices.AddMessageError("ERROR: Update entity ID", e);
			throw;
		}
	}

	// Método para actualizar una entidad en la base de datos utilizando un arreglo de identificadores proporcionado
	public bool Update(string[] Id)
	{
		try
		{
			// Inicia una transacción
			MTConnection?.GDatos.BeginTransaction();
			// Actualiza la entidad en la base de datos utilizando el arreglo de identificadores proporcionado
			MTConnection?.UpdateObject(this, Id);
			// Confirma la transacción
			MTConnection?.GDatos.CommitTransaction();
			// Retorna verdadero para indicar que la operación fue exitosa
			return true;
		}
		catch (Exception e)
		{
			// Revierte la transacción en caso de error y registra el error
			MTConnection?.GDatos.RollBackTransaction();
			LoggerServices.AddMessageError("ERROR: Update entity []ID", e);
			throw;
		}
	}

	// Método para eliminar una entidad de la base de datos
	public bool Delete()
	{
		try
		{
			// Inicia una transacción
			MTConnection?.GDatos.BeginTransaction();
			// Elimina la entidad de la base de datos
			MTConnection?.Delete(this);
			// Confirma la transacción
			MTConnection?.GDatos.CommitTransaction();
			// Retorna verdadero para indicar que la operación fue exitosa
			return true;
		}
		catch (Exception e)
		{
			// Revierte la transacción en caso de error y registra el error
			MTConnection?.GDatos.RollBackTransaction();
			LoggerServices.AddMessageError("ERROR: Update entity Delete", e);
			throw;
		}
	}
	
public int Count(params FilterData[] where_condition)
	{
		// Verifica si alguna condición de filtro tiene valores nulos o vacíos
		if (where_condition.Where(c => c.FilterType != "or" 
		&& c.FilterType != "and"
		&& c.FilterType != "Not Null"
		&& c.FilterType != "NotNull"
		&& c.FilterType != "IsNull"
		&& c.FilterType != "Is Null"
		&& (c.Values == null || c.Values?.Count == 0)).ToList().Count > 0)
		{
			// Retorna una lista vacía si alguna condición no está definida correctamente
			return 0;
		}

		// Si no hay problemas con las condiciones, se agregan al filtro de datos de la entidad
		if (filterData == null)
			filterData = new List<FilterData>();

		filterData.AddRange(where_condition.ToList());

		// Se obtienen los datos utilizando el filtro actualizado
		var Count = MTConnection?.Count(this);
		// Retorna los datos obtenidos o una lista vacía si es nulo
		return Count ?? 0;
	}
	// Método para describir la estructura de la entidad utilizando un tipo de enumeración de SQL
	public List<EntityProps> DescribeEntity(SqlEnumType sqlEnumType)
	{
		// Determina la consulta de descripción de entidad según el tipo de SQL
		string? DescribeEntityQuery = sqlEnumType switch
		{
			SqlEnumType.SQL_SERVER => SQLServerEntityQuerys.DescribeEntityQuery,
			SqlEnumType.POSTGRES_SQL => PostgreEntityQuerys.DescribeEntityQuery,
			SqlEnumType.MYSQL => MySqlEntityQuerys.DescribeEntityQuery.Replace("entityDatabase", MTConnection?.GDatos.Database),
			_ => null
		};
		// Obtiene los datos de la descripción de la entidad utilizando la consulta determinada
		DataTable? Table = this.MTConnection?.GDatos.TraerDatosSQL(DescribeEntityQuery?.Replace("entityName", this.GetType().Name.ToLower()), null);
		// Convierte los datos de la descripción a una lista de propiedades de entidad
		List<EntityProps> entityProps = AdapterUtil.ConvertDataTable<EntityProps>(Table, new EntityProps());
		// Si no se encuentra ninguna descripción, lanza una excepción
		if (entityProps.Count == 0)
		{
			throw new Exception("La entidad buscada no existe: " + this.GetType().Name);
		}
		// Retorna la lista de propiedades de entidad obtenidas
		return entityProps;
	}
}



public abstract class StoreProcedureClass : TransactionalClass
{
	public List<Object>? Parameters { get; set; }
	public ResponseService Execute()
	{
		var DataProcedure = MTConnection?.GDatos.ExecuteProcedure(this, Parameters);
		return new ResponseService
		{
			message = "Procedimiento ejecutado correctamente"
		};
	}
	public List<T> Get<T>()
	{
		var DataProcedure = MTConnection?.TakeListWithProcedure<T>(this, Parameters);
		return DataProcedure.ToList() ?? new List<T>();
	}
}
public abstract class TransactionalClass
{
	private WDataMapper? Conection;
	protected WDataMapper? MTConnection
	{
		get
		{
			if (this.Conection != null)
				return this.Conection;
			else
				return Connections.Default;
		}
		set { Conection = value; }
	}
	internal WDataMapper? GetConnection()
	{
		return MTConnection;
	}
	public void SetConnection(WDataMapper? wDataMapper)
	{
		MTConnection = wDataMapper;
	}

	//TRANSACCIONES
	public void BeginGlobalTransaction()
	{
		MTConnection?.GDatos.BeginGlobalTransaction();
	}
	public void CommitGlobalTransaction()
	{
		MTConnection?.GDatos.CommitGlobalTransaction();
	}
	public void RollBackGlobalTransaction()
	{
		MTConnection?.GDatos.RollBackGlobalTransaction();
	}
}
