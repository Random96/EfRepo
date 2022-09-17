using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data
{
    public interface IRepository<T> : IDisposable where T : class
    {
        T Add(T item);

        Task<T> AddAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет наличие записей в репозитории по заданному фильтру
        /// </summary>
        /// <param name="filters">Список фильтров</param>
        /// <returns></returns>
        public bool Any(IEnumerable<FilterObject> filters);
        public Task<bool> AnyAsync(IEnumerable<FilterObject> filters, CancellationToken cancellationToken = default);

        IEnumerable<T> GetList(IEnumerable<FilterObject> filters, IEnumerable<OrderElement>? orderByField, bool includeProperties = false);
        Task<IEnumerable<T>> GetListAsync(IEnumerable<FilterObject> filters, IEnumerable<OrderElement>? orderByField, CancellationToken cancellationToken, bool includeProperties = false);
        Task<T> UpdateAsync(T item, CancellationToken cancellationToken = default);
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<T>> GetPageAsync(int pageNum, int pageSize, IEnumerable<FilterObject> filters, IEnumerable<OrderElement>? orderByField, CancellationToken cancellationToken, bool includeProperties = false);

        /// <summary>
        /// Удаляет указанный элемент
        /// </summary>
        /// <param name="obj">Удаляемый элемент</param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        Task DeleteAsync(int id, CancellationToken cancellationToken);

        int ? UserId { get; set; }

        IDataModel DataProvider { get; }
    }
}

    