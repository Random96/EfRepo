using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data
{
    internal interface IDeletable
    { 
        bool IsDeleted { get; set; }
    }
}
