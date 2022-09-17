using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data
{

    public enum FilterOption
    {
        Equals,
        NotEquals,
        IsNull,
        IsNotNull,
        In,
        NotIn,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    public class FilterObject
    {
        public FilterObject(string propertyName, FilterOption operation, object value)
        {
            PropertyName = propertyName;
            Operation = operation;
            Value = value;
        }

        public string PropertyName { get; set; }
        public FilterOption Operation { get; set; }
        public object Value { get; set; }
        public StringComparison Comparison { get; internal set; }
    }
}