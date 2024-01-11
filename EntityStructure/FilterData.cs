namespace CAPA_DATOS
{
    public class FilterData
    {
        public string? PropName { get; set; }
        public string? FilterType { get; set; }
        public List<String?>? Values { get; set; }
        public static FilterData In(string? propName, params String?[] values)
        {
            return new FilterData { PropName = propName, FilterType = "in", Values = values.ToList() };
        }
        public static FilterData NotIn(string? propName,  params String?[] values)
        {
            return new FilterData { PropName = propName, FilterType = "not in", Values = values.ToList() };
        }
        public static FilterData Equal(string? propName, String? value)
        {
            return new FilterData { PropName = propName, FilterType = "=", Values = new List<string?> { value } };
        }
        public static FilterData Like(string? propName, String? value)
        {
            return new FilterData { PropName = propName, FilterType = "like", Values = new List<string?> { value } };
        }
        public static FilterData Distinc(string? propName, String? value)
        {
            return new FilterData { PropName = propName, FilterType = "!=", Values = new List<string?> { value } };
        }
        public static FilterData Between(string? propName, String? value, String? value2)
        {
            return new FilterData { PropName = propName, FilterType = "BETWEEN", Values = new List<string?> { value , value2} };
        }
    }
}