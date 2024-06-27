using System.Data;
using System.Reflection;
using Newtonsoft.Json;

namespace CAPA_DATOS;
public class AdapterUtil
{
    public static object GetValue(Object defaultValue, Type type)
    {
        string? literal = defaultValue.ToString();
        if (string.IsNullOrEmpty(literal))
        {
            return defaultValue;
        }

        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsEnum)
        {
            // Si el tipo es un enumerado, intenta convertir la cadena al enumerado.
            try
            {
                return Enum.Parse(underlyingType, literal, ignoreCase: true);
            }
            catch (ArgumentException)
            {
                // Puedes manejar la excepción de argumento aquí si es necesario.
                // En este caso, devolvemos el valor predeterminado.
                return defaultValue;
            }
        }

        IConvertible obj = literal;
        Type? u = Nullable.GetUnderlyingType(type);

        if (u != null)
        {
            return (obj == null) ? defaultValue : Convert.ChangeType(obj, u);
        }
        else
        {
            return Convert.ChangeType(obj, type);
        }
    }
    public static object? GetJsonValue(Object DefaultValue, Type type)
    {
        string? Literal = DefaultValue.ToString();
        if (Literal == null || Literal == "" || Literal == string.Empty) return null;
        var ListInstanceType = JsonConvert.DeserializeObject(Literal, type);
        return ListInstanceType;
    }
    //DEPRECATE        
    public static bool JsonCompare(object obj, object another)
    {
        if (ReferenceEquals(obj, another)) return true;
        if ((obj == null) || (another == null)) return false;
        if (obj.GetType() != another.GetType()) return false;

        var objJson = JsonConvert.SerializeObject(obj);
        var anotherJson = JsonConvert.SerializeObject(another);

        return objJson == anotherJson;
    }
    public static List<T> ConvertDataTable<T>(DataTable dt, object Inst)
    {
        return dt.AsEnumerable().Select(row => ConvertRow<T>(Inst, row)).ToList();
    }
    public static T ConvertRow<T>(object Inst, DataRow dr)
    {
        var obj = Activator.CreateInstance<T>();
        Type temp = Inst.GetType();
        foreach (DataColumn column in dr.Table.Columns)
        {
            if (!string.IsNullOrEmpty(dr[column.ColumnName].ToString()))
            {
                foreach (PropertyInfo oProperty in temp.GetProperties())
                {
                    if (oProperty.Name.ToLower() == column.ColumnName.ToLower())
                    {
                        var val = dr[column.ColumnName];
                        var jsonProp = (JsonProp?)Attribute.GetCustomAttribute(oProperty, typeof(JsonProp));
                        var oneToOne = (OneToOne?)Attribute.GetCustomAttribute(oProperty, typeof(OneToOne));
                        var manyToOne = (ManyToOne?)Attribute.GetCustomAttribute(oProperty, typeof(ManyToOne));
                        var oneToMany = (OneToMany?)Attribute.GetCustomAttribute(oProperty, typeof(OneToMany));
                        if (oneToOne != null || manyToOne != null || oneToMany != null || jsonProp != null)
                        {
                            var getVal = AdapterUtil.GetJsonValue(val, oProperty.PropertyType);
                            oProperty.SetValue(obj, getVal);
                        }
                        else
                        {
                            var getVal = AdapterUtil.GetValue(val, oProperty.PropertyType);
                            oProperty.SetValue(obj, getVal);
                        }

                    }
                    else continue;
                }
            }
            else continue;
        }
        return obj;
    }

}