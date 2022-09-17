using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data.Extensions
{
    internal static class DbQueryExtension
    {
        private static IEnumerable<OrderElement> PrepareOrder(IEnumerable<OrderElement> orderByField)
        {

            if (orderByField == null)
            {
                return new OrderElement[] { new OrderElement(Ordering.Asc, "Id") };
            }

            if (orderByField.Any(x => x.Name == "Id"))
                return orderByField;

            var ret = orderByField.ToList();

            ret.Add(new OrderElement(Ordering.Asc, "Id"));

            return ret;
        }

        public static IQueryable<T> SetOrder<T>(this IQueryable<T> dbSet, IEnumerable<OrderElement> orderByField)
            where T : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet));

            int i = 0;

            orderByField = PrepareOrder(orderByField);

            foreach (var order in orderByField)
            {
                string methodName = string.Empty;

                switch (order.Order)
                {
                    case Ordering.Asc:
                        {
                            if (i++ == 0)
                            {
                                methodName = "OrderBy";
                            }
                            else
                            {
                                methodName = "ThenBy";
                            }
                        }
                        break;
                    case Ordering.Dsc:
                        {
                            if (i++ == 0)
                            {
                                methodName = "OrderByDescending";
                            }
                            else
                            {
                                methodName = "ThenByDescending";
                            }
                        }
                        break;
                }

                var arg = Expression.Parameter(typeof(T), "x");

                Expression expr = arg;

                expr = Expression.Property(expr, order.Name);

                var lambda = Expression.Lambda(expr, arg);

                var method = ReflectionExtension.GetGenericMethod(typeof(Queryable), methodName,
                    new[] { typeof(T), expr.Type },
                    new[] { dbSet.GetType(), lambda.GetType() });

                if (method == null)
                    return dbSet;

                var ret = method.Invoke(null, new object[] { dbSet, lambda });

                if (ret is IOrderedQueryable<T> retQuerable)
                    dbSet = retQuerable;
            }

            return dbSet;
        }

        public static IQueryable<T> GetQueryable<T>(this IQueryable<T> source, IEnumerable<FilterObject>? filters, IEnumerable<OrderElement>? order, bool includePropertyes = false)
            where T : class
        {
            var actualFilters = filters?.ToArray() ?? Array.Empty<FilterObject>();

            var item = Expression.Parameter(typeof(T), "item");

            Expression<Func<T, bool>>? filterExp = null;
            Expression<Func<T, bool>>? lambda;

            foreach (var filter in actualFilters)
            {
                lambda = null;
                var property = Expression.Property(item, filter.PropertyName);

                switch (filter.Operation)
                {
                    case FilterOption.Equals:
                        {
                            if (filter.Comparison == StringComparison.CurrentCultureIgnoreCase && filter.Value is string strVal)
                            {
                                var value = Expression.Constant(strVal.ToUpper(), property.Type);

                                Type prop = typeof(string);
                                var methods = prop.GetMethods().Where(x => x.Name == "ToUpper").ToArray();
                                var method = methods.First(); //  prop? .GetMethod("Equals", new Type[] { prop, typeof(StringComparison) });
                                var upperProperty = Expression.Lambda(Expression.Call(property, method));
                                var expr = Expression.Equal(upperProperty.Body, value);
                                lambda = Expression.Lambda<Func<T, bool>>(expr, item);

                            }
                            else
                            {
                                var value = Expression.Constant(filter.Value, property.Type);
                                var expr = Expression.Equal(property, value);
                                lambda = Expression.Lambda<Func<T, bool>>(expr, item);
                            }
                        }
                        break;
                    case FilterOption.NotEquals:
                        {
                            var value = Expression.Constant(filter.Value, property.Type);
                            var expr = Expression.NotEqual(property, value);
                            lambda = Expression.Lambda<Func<T, bool>>(expr, item);
                        }
                        break;
                    case FilterOption.In:
                        {
                            var value = Expression.Constant(filter.Value);
                            var arrayType = GetArrayType(filter.Value);
                            if (arrayType != null)
                            {
                                var method = ReflectionExtension.GetGenericMethod(typeof(Enumerable), "Contains",
                                    new[] { arrayType.Item2 },
                                    new[] { arrayType.Item1, arrayType.Item2 });

                                if (method == null)
                                    throw new Exception("illegal method");

                                var expr = Expression.Call(method, value, property);
                                lambda = Expression.Lambda<Func<T, bool>>(expr, item);
                            }
                        }
                        break;
                    case FilterOption.NotIn:
                        {
                            var value = Expression.Constant(filter.Value);
                            var arrayType = GetArrayType(filter.Value);
                            if (arrayType != null)
                            {
                                var method = ReflectionExtension.GetGenericMethod(typeof(Enumerable), "Contains",
                                    new[] { arrayType.Item2 },
                                    new[] { arrayType.Item1, arrayType.Item2 });

                                if (method == null)
                                    throw new Exception("illegal method");

                                var expr = Expression.Not(Expression.Call(method, value, property));
                                lambda = Expression.Lambda<Func<T, bool>>(expr, item);
                            }
                        }
                        break;
                    case FilterOption.GreaterThanOrEqual:
                        {
                            var value = Expression.Constant(filter.Value, property.Type);
                            var expr = Expression.GreaterThanOrEqual(property, value);
                            lambda = Expression.Lambda<Func<T, bool>>(expr, item);
                        }
                        break;
                    case FilterOption.LessThanOrEqual:
                        {
                            var value = Expression.Constant(filter.Value, property.Type);
                            var expr = Expression.LessThanOrEqual(property, value);
                            lambda = Expression.Lambda<Func<T, bool>>(expr, item);
                        }
                        break;
                }

                if (lambda != null)
                {
                    if (filterExp == null)
                        filterExp = lambda;
                    else
                    {
                        var andExp = Expression.AndAlso(filterExp.Body, lambda.Body);
                        filterExp = Expression.Lambda<Func<T, bool>>(andExp, item);
                    }
                }
            }

            if (filterExp != null)
                source = source.Where(filterExp);

            if (includePropertyes)
            {
                // .Where(x => x.PropertyType.IsClass && x.PropertyType != typeof(string)
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                        continue;


                    if (prop.PropertyType.IsClass || typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                    {
                        source = source.Include(prop.Name);
                    }
                };
            }

            if (order != null)
                source = source.SetOrder(order);

            return source;
        }

        private static Tuple<Type, Type>? GetArrayType(object value)
        {
            TypeInfo type = (TypeInfo)value.GetType();
            foreach (var interfaces in type.ImplementedInterfaces)
            {
                if (interfaces.IsGenericType && interfaces.GenericTypeArguments.Any())
                {
                    return new Tuple<Type, Type>(interfaces, interfaces.GenericTypeArguments[0]);
                }
            }
            return null;
        }

    }
}
