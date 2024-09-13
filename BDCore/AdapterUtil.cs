using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace CAPA_DATOS
{
    public class AdapterUtil
    {
        public static object? GetValue(object defaultValue, Type type)
        {
            if (defaultValue == null) return null;
            if (type.IsInstanceOfType(defaultValue)) return defaultValue;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (underlyingType.IsEnum) return Enum.Parse(underlyingType, defaultValue.ToString()!, true);

            try { return Convert.ChangeType(defaultValue, underlyingType); }
            catch { return defaultValue; } // Maneja conversiones inválidas devolviendo el valor por defecto
        }


        public static object? GetJsonValue(object defaultValue, Type type)
        {
            if (defaultValue is string literal && !string.IsNullOrEmpty(literal))
            {
                try { return JsonConvert.DeserializeObject(literal, type); }
                catch { }
            }
            return null;
        }


        public static bool JsonCompare(object obj, object another)
        {
            if (ReferenceEquals(obj, another)) return true;
            if (obj == null || another == null) return false;

            var objJson = JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            });

            var anotherJson = JsonConvert.SerializeObject(another, Formatting.None, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            });

            return objJson == anotherJson;
        }

        public static List<T> ConvertDataTable<T>(DataTable dt, object Inst)
        {
            return dt.AsEnumerable()
                     .Select(row => ConvertRow<T>(Inst, row))
                     .ToList();
        }

        public static T ConvertRow<T>(object Inst, DataRow dr)
        {
            var obj = Activator.CreateInstance<T>();
            var instanceType = Inst.GetType();
            var properties = instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var column in dr.Table.Columns.Cast<DataColumn>())
            {
                string columnName = column.ColumnName.ToLower();
                object? columnValue = dr[column.ColumnName];

                if (string.IsNullOrEmpty(columnValue?.ToString()))
                    continue;

                var property = properties.FirstOrDefault(p => p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                if (property == null)
                    continue;

                var jsonProp = property.GetCustomAttribute<JsonProp>();
                var oneToOne = property.GetCustomAttribute<OneToOne>();
                var manyToOne = property.GetCustomAttribute<ManyToOne>();
                var oneToMany = property.GetCustomAttribute<OneToMany>();

                object? value = (jsonProp != null || oneToOne != null || manyToOne != null || oneToMany != null)
                    ? GetJsonValue(columnValue, property.PropertyType)
                    : GetValue(columnValue, property.PropertyType);

                property.SetValue(obj, value);
            }

            return obj;
        }
    }
}
