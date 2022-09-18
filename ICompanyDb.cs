using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ru.emlsoft.data.Access;

namespace ru.emlsoft.data
{
    public interface ICompanyDb
    {
        DbSet<IUser> Users { get; set; }
    }
}
