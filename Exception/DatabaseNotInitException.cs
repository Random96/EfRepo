using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data.exception
{
    public class DatabaseNotInitException : Exception
    {
        private readonly string? _message;
        public DatabaseNotInitException(string? message)
        {
            _message = message;
        }
        public override string Message => string.IsNullOrWhiteSpace(_message)
            ? Properties.Resources.ERROR_DB_NOT_INIT
            : Properties.Resources.ERROR_DB_NOT_INIT + "(" + _message + ")";
    }
}
