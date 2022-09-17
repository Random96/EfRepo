using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data.exception
{
    public class CompanyNotDefinedException : Exception
    {
        public override string Message => "Company not defind";
    }
}
