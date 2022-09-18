using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data
{
    public enum Ordering { Asc, Dsc }

    public class OrderElement
    {
        public OrderElement(Ordering order, string name)
        {
            Order = order;
            Name = name;
        }

        public string Name { get; set; }
        public Ordering Order { get; set; }
    }
}