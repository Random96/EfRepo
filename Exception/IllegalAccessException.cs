using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data.exception
{
    public class IllegalAccessException : Exception
    {
        public override string Message => "Access to another company data";
    }
}
