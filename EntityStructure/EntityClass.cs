using System.Data;
using System.Reflection;
using CAPA_DATOS.BDCore.Abstracts;
using CAPA_DATOS.BDCore.SQLServerImplementations;

namespace CAPA_DATOS;
public abstract class EntityClass : TransactionalClass
{
    public List<FilterData>? filterData { get; set; }

    public List<T> Get<T>(string condition = "")
    {
        var Data = MTConnection?.TakeList<T>(this, true, condition);
        return Data.ToList() ?? new List<T>();
    }
    public List<T> GetAll<T>()
    {
        var Data = MTConnection?.TakeList<T>(this, true);
        return Data.ToList() ?? new List<T>();
    }
    public List<T> Where<T>(params FilterData[] where_condition)
    {
        if (where_condition.Where(c => c.Values == null || c.Values?.Count == 0).ToList().Count > 0)
        {
            return new List<T>();
        }
        if (filterData == null)
            filterData = new List<FilterData>();

        filterData.AddRange(where_condition.ToList());
        var Data = MTConnection?.TakeList<T>(this, true);
        return Data ?? new List<T>();
    }
    public List<T> Get_WhereIN<T>(string Field, string?[]? conditions)
    {
        string condition = BuildArrayIN(conditions);
        //string? condition =  SqlServerGDatos.BuildArrayIN(conditions.ToList());
        var Data = MTConnection?.TakeList<T>(this, true, Field + " IN (" + condition + ")");
        return Data ?? new List<T>();
    }
    public List<T> Get_WhereNotIN<T>(string Field, string[] conditions)
    {
        string condition = BuildArrayIN(conditions);
        var Data = MTConnection?.TakeList<T>(this, true, Field + " NOT IN (" + condition + ")");
        return Data ?? new List<T>();
    }
    public T? Find<T>(params FilterData[]? where_condition)
    {
        filterData = where_condition?.ToList();
        var Data = SqlADOConexion.SQLM != null ? SqlADOConexion.SQLM.TakeObject<T>(this) : default(T);
        return Data;
    }
    public Boolean Exists<T>()
    {
        var Data = MTConnection?.TakeList<T>(this, true);
        return Data?.Count > 0;
    }
    public List<T> SimpleGet<T>()
    {
        var Data = MTConnection?.TakeList<T>(this, false);
        return Data ?? new List<T>();
    }
    public static List<T> EndpointMethod<T>()
    {
        List<T> list = new List<T>();
        return list;
    }


    private static string BuildArrayIN(string?[]? conditions)
    {
        string condition = "";
        foreach (string? Value in conditions ?? new string?[0])
        {
            condition = condition + Value + ",";
        }
        condition = condition.TrimEnd(',');
        if (condition == "")
        {
            return "-1";
        }
        return condition;
    }

    public object? Save()
    {
        try
        {
            MTConnection?.GDatos.BeginTransaction();
            var result = MTConnection?.InsertObject(this);
            MTConnection?.GDatos.CommitTransaction();
            return result;
        }
        catch (Exception e)
        {
            MTConnection?.GDatos.RollBackTransaction();
            LoggerServices.AddMessageError("ERROR: Save entity", e);
            throw;
        }
    }
    public object? Update()
    {
        try
        {
            PropertyInfo[] lst = this.GetType().GetProperties();
            var pkPropiertys = lst.Where(p => (PrimaryKey?)Attribute.GetCustomAttribute(p, typeof(PrimaryKey)) != null).ToList();
            var values = pkPropiertys.Where(p => p.GetValue(this) != null).ToList();
            if (pkPropiertys.Count == values.Count)
            {
                this.Update(pkPropiertys.Select(p => p.Name).ToArray());
                return new ResponseService()
                {
                    status = 200,
                    message = this.GetType().Name + " actualizado correctamente"
                };
            }
            else return new ResponseService()
            {
                status = 500,
                message = "Error al actualizar: no se encuentra el registro " + this.GetType().Name
            };
        }
        catch (Exception e)
        {
            LoggerServices.AddMessageError("ERROR: Update entity", e);
            return new ResponseService()
            {
                status = 500,
                message = "Error al actualizar: " + e.Message
            };
        }
    }
    public bool Update(string Id)
    {
        try
        {
            MTConnection?.GDatos.BeginTransaction();
            MTConnection?.UpdateObject(this, Id);
            MTConnection?.GDatos.CommitTransaction();
            return true;
        }
        catch (Exception e)
        {
            MTConnection?.GDatos.RollBackTransaction();
            LoggerServices.AddMessageError("ERROR: Update entity ID", e);
            throw;
        }
    }
    public bool Update(string[] Id)
    {
        try
        {
            MTConnection?.GDatos.BeginTransaction();
            MTConnection?.UpdateObject(this, Id);
            MTConnection?.GDatos.CommitTransaction();
            return true;
        }
        catch (Exception e)
        {
            MTConnection?.GDatos.RollBackTransaction();
            LoggerServices.AddMessageError("ERROR: Update entity []ID", e);
            throw;
        }
    }
    public bool Delete()
    {
        try
        {
            MTConnection?.GDatos.BeginTransaction();
            MTConnection?.Delete(this);
            MTConnection?.GDatos.CommitTransaction();
            return true;
        }
        catch (Exception e)
        {
            MTConnection?.GDatos.RollBackTransaction();
            LoggerServices.AddMessageError("ERROR: Update entity Delete", e);
            throw;
        }
    }
    public List<EntityProps> DescribeEntity(SqlEnumType sqlEnumType)
    {
        string? DescribeEntityQuery = sqlEnumType switch
        {
            SqlEnumType.SQL_SERVER => SQLServerEntityQuerys.DescribeEntityQuery,
            _ => null
        };
        DataTable? Table = this.MTConnection?.GDatos.TraerDatosSQL(DescribeEntityQuery?.Replace("entityName", this.GetType().Name));
        List<EntityProps> entityProps = AdapterUtil.ConvertDataTable<EntityProps>(Table, new EntityProps());
        if (entityProps.Count == 0)
        {
            throw new Exception("La entidad buscada no existe: " + this.GetType().Name);
        }
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
                return SqlADOConexion.SQLM;
        }
        set { Conection = value; }
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
