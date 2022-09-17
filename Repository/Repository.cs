using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ru.emlsoft.data.exception;
using ru.emlsoft.data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace ru.emlsoft.data.Repository
{
    internal class Repository<T> : IRepository<T> where T : class, IKeyable, new()
    {
        private readonly IMapper _mapper;
        private IDataModel ? _db;
        private readonly ILogger _logger;
        private bool disposedValue;

        public Repository(ILogger logger, IDataModel db, IMapper mapper)
        {
            _logger = logger;

            _db = db ?? throw new ArgumentNullException(nameof(db));

            _mapper = mapper;
        }

        public IDataModel DataProvider => _db ?? throw new Exception();

        private int CompanyId
        {
            get
            {
                if (_db == null || disposedValue)
                    throw new Exception("Is disposed");

                if (_db is ICompanyDb dbCompany)
                {
                    if (UserId == 0)
                        throw new NoUserDefinedException();

                    var user = dbCompany.Users.Find(UserId);
                    if (user == null)
                        throw new UserNotFoundException();

                    if (user.CompanyId == null || user.CompanyId == 0)
                        throw new CompanyNotDefinedException();

                    return user.CompanyId.Value;
                }

                return -1;
            }
        }

        protected void SetLastUpdate(object item, DateTime lastUpdate, Stack<object>? stack = null)
        {
            if (UserId == null)
                throw new Exception();

            if (stack == null)
                stack = new Stack<object>();
            stack.Push(item);

            if (item is ICompany company)
                company.CompanyId = CompanyId;

            if (item is IEntity entity)
            {
                entity.UserId = UserId.Value;
                entity.LastUpdated = lastUpdate;
            }

            foreach (var prop in item.GetType().GetProperties())
            {
                var value = prop.GetValue(item, null);

                if (value == null || value is string || value is DateTime)
                    continue;

                if (value is IEnumerable<object> childObject)
                {
                    foreach (var child in childObject)
                        if (!stack.Contains(child))
                            SetLastUpdate(child, lastUpdate, stack);

                    continue;
                }

                if (!stack.Contains(value))
                    SetLastUpdate(value, lastUpdate, stack);
            }

            stack.Pop();
        }


        protected bool CheckVersion(object? newValue, object? oldValue, Stack<object>? stack = null)
        {
            if (oldValue == null && newValue == null)
                return true;

            if (oldValue == null && newValue != null || newValue == null)
                return true;

            if (stack == null)
                stack = new Stack<object>();

            stack.Push(newValue);

            if (newValue is IEntity newEntity)
            {
                if (oldValue is IEntity oldEntity)
                {
                    if (newEntity.LastUpdated != oldEntity.LastUpdated)
                        return false;
                }
                else
                {
                    throw new Exception("Bad structure");
                }
            }

            foreach (var prop in newValue.GetType().GetProperties())
            {
                var value = prop.GetValue(newValue, null);

                if (value == null || value is string || value is DateTime)
                    continue;

                if (value is IEnumerable<object> childObject)
                {
                    foreach (var child in childObject)
                        if (!stack.Contains(child))
                            if (!CheckVersion(child, prop.GetValue(oldValue, null), stack))
                                return false;

                    continue;
                }

                if (!stack.Contains(value))
                    CheckVersion(value, prop.GetValue(oldValue, null), stack);
            }

            stack.Pop();

            return true;
        }

        public T Add(T item)
        {
            _logger.LogTrace("Add async of item");
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            SetLastUpdate(item, DateTime.UtcNow);

            _db.Set<T>().Add(item);
            _logger.LogTrace("Item was added");

            _db.SaveChanges();
            _logger.LogTrace("Added was commited");

            return item;
        }

        public async Task<T> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("Add async of item");
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            SetLastUpdate(item, DateTime.UtcNow);

            await _db.Set<T>().AddAsync(item, cancellationToken);
            _logger.LogTrace("Item was added");

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogTrace("Added was commited");

            return item;
        }

        public bool Any(IEnumerable<FilterObject> filters)
        {
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            _logger.LogTrace("Method 'Any' called...");
            try
            {
                List<FilterObject>? newFilters = null;
                if (typeof(ICompany).IsAssignableFrom(typeof(T)))
                {
                    newFilters = filters.ToList();
                    newFilters.Add(new FilterObject(nameof(ICompany.CompanyId), FilterOption.Equals, CompanyId));
                }

                IQueryable<T> query = _db.Set<T>().AsNoTracking().GetQueryable(newFilters ?? filters, null, false);
                return query.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'Any' failed");
                return false;
            }
            finally
            {
                _logger.LogTrace("Method 'Any' finished");
            }
        }

        public async Task<bool> AnyAsync(IEnumerable<FilterObject> filters, CancellationToken cancellationToken = default)
        {
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            _logger.LogTrace("Method 'AnyAsync' called...");
            try
            {
                List<FilterObject>? newFilters = null;
                if (typeof(ICompany).IsAssignableFrom(typeof(T)))
                {
                    newFilters = filters.ToList();
                    newFilters.Add(new FilterObject(nameof(ICompany.CompanyId), FilterOption.Equals, CompanyId));
                }

                IQueryable<T> query = _db.Set<T>().AsNoTracking().GetQueryable(newFilters ?? filters, null, false);

                var ret = await query.AnyAsync(cancellationToken);

                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'AnyAsync' failed");
                return false;
            }
            finally
            {
                _logger.LogTrace("Method 'AnyAsync' finished");
            }
        }

        public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            _logger.LogTrace("Method 'GetByIdAsync' called...");
            try
            {
                var ret = await _db.Set<T>().FindAsync(new object?[] { id }, cancellationToken: cancellationToken);

                if (ret != null)
                {
                    if (ret is ICompany company)
                    {
                        if (company.CompanyId != CompanyId)
                            throw new IllegalAccessException();
                    }
                    return ret;
                }

                return new T();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'GetByIdAsync' failed");
            }
            finally
            {
                _logger.LogTrace("Method 'GetByIdAsync' finished");
            }

            return new T();
        }

        public IEnumerable<T> GetList(IEnumerable<FilterObject> filters, IEnumerable<OrderElement>? orderByField, bool includeProperties = false)
        {
            _logger.LogTrace("Get list of items");
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            try
            {
                List<FilterObject>? newFilters = null;
                if (typeof(ICompany).IsAssignableFrom(typeof(T)))
                {
                    newFilters = filters.ToList();
                    newFilters.Add(new FilterObject(nameof(ICompany.CompanyId), FilterOption.Equals, CompanyId));
                }

                IQueryable<T> query = _db.Set<T>().AsNoTracking().GetQueryable(newFilters ?? filters, orderByField, includeProperties);
                return query.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'GetList' failed");
                return Array.Empty<T>();
            }
            finally
            {
                _logger.LogTrace("Method 'GetList' finished");
            }
        }

        public async Task<IEnumerable<T>> GetListAsync(IEnumerable<FilterObject> filters, IEnumerable<OrderElement>? orderByField, CancellationToken cancellationToken = default, bool includeProperties = false)
        {
            _logger.LogTrace("Get list of items async");
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            try
            {
                List<FilterObject>? newFilters = null;
                if (typeof(ICompany).IsAssignableFrom(typeof(T)))
                {
                    newFilters = filters.ToList();
                    newFilters.Add(new FilterObject(nameof(ICompany.CompanyId), FilterOption.Equals, CompanyId));
                }

                IQueryable<T> query = _db.Set<T>().AsNoTracking().GetQueryable(newFilters ?? filters, orderByField, includeProperties);

                var ret = await query.ToArrayAsync(cancellationToken);

                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'GetListAsync' failed");
                return Array.Empty<T>();
            }
            finally
            {
                _logger.LogTrace("Method 'GetListAsync' finished");
            }
        }

        public async Task<ICollection<T>> GetPageAsync(int pageNum, int pageSize, IEnumerable<FilterObject> filters, IEnumerable<OrderElement>? orderByField, CancellationToken cancellationToken, bool includeProperties = false)
        {
            _logger.LogTrace("Get list of items async");
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            if (pageSize == 0)
                return new List<T>();

            try
            {
                List<FilterObject>? newFilters = null;
                if (typeof(ICompany).IsAssignableFrom(typeof(T)))
                {
                    newFilters = filters.ToList();
                    newFilters.Add(new FilterObject(nameof(ICompany.CompanyId), FilterOption.Equals, CompanyId));
                }

                IQueryable<T> query = _db.Set<T>().AsNoTracking().GetQueryable(newFilters ?? filters, orderByField, includeProperties);

                var ret = await query.ToArrayAsync(cancellationToken);

                return ret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'GetListAsync' failed");
                return Array.Empty<T>();
            }
            finally
            {
                _logger.LogTrace("Method 'GetListAsync' finished");
            }
        }

        public async Task<T> UpdateAsync(T item, CancellationToken cancellationToken = default)
        {
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            _logger.LogTrace("Method 'UpdateAsync' called...");
            try
            {
                if (item.Id == 0)
                {
                    SetLastUpdate(item, DateTime.UtcNow);

                    _db.Set<T>().Add(item);
                }
                else
                {
                    var oldItem = _db.Set<T>().Find(item.Id);

                    if (oldItem == null)
                        throw new Exception();


                    if (!CheckVersion(item, oldItem))
                        throw new Exception("Data was changed");

                    oldItem = _mapper.Map(item, oldItem);

                    item = oldItem;

                    SetLastUpdate(item, DateTime.UtcNow);
                }

                await _db.SaveChangesAsync(cancellationToken);

                return item;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'UpdateAsync' failed");
                throw;
            }
            finally
            {
                _logger.LogTrace("Method 'UpdateAsync' finished");
            }
        }

        public void Delete(int Id)
        {
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken)
        {
            if (_db == null || disposedValue)
                throw new Exception("Is disposed");

            _logger.LogTrace("Method 'DeleteAsync' called...");
            try
            {
                if (id == 0)
                {
                    _logger.LogTrace("Id is  0 - return");
                    return;
                }

                var item = await _db.Set<T>().FindAsync(new object[] { id }, cancellationToken);

                if (item == null)
                {
                    _logger.LogTrace("Item with {id} not found", id);
                    return;
                }

                if (item is ICompany company)
                {
                    if (company.CompanyId != CompanyId)
                    {
                        _logger.LogTrace("Item with {id} is not in {CompanyId} company", new object[id, CompanyId]);
                        return;
                    }
                }

                if (item is IEntity entity)
                {
                    entity.IsDeleted = true;
                    _logger.LogTrace("Flag 'Deleted' in item with Id={id} was set to true", id);
                }
                else
                {
                    _db.Set<T>().Remove(item);
                    _logger.LogTrace("Item with Id={id} was deleted", id);
                }

                await _db.SaveChangesAsync(cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Method 'DeleteAsync' failed");
                throw;
            }
            finally
            {
                _logger.LogTrace("Method 'DeleteAsync' finished");
            }
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                _db = null;

                disposedValue = true;
            }
        }

        // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        // ~Repository()
        // {
        //     // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public int ? UserId { get; set; }

        #endregion
    }
}