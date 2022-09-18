using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ru.emlsoft.data.Extensions
{
    public static class ReflectionExtension
    {
        public static MethodInfo? GetGenericMethod(Type type, string name, Type[] genericTypeArgs, Type[] paramTypes)
        {
            //выбираем у типа все методы
            var abstractGenericMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            //фильтруем по имени
            abstractGenericMethod = abstractGenericMethod.Where(x => x.Name == name).ToArray();

            //интересуют только generic-методы
            abstractGenericMethod = abstractGenericMethod.Where(x => x.IsGenericMethod).ToArray();

            //выбираем параметры, которые принимает метод
            //отбрасываем методы принимающие иное количество параметров
            abstractGenericMethod = abstractGenericMethod.Where(x => x.GetParameters().Length == paramTypes.Length).ToArray();

            //создаем конкретный метод, указывая типы шаблона
            var concreteGenericMethod = abstractGenericMethod.Select(x => x.MakeGenericMethod(genericTypeArgs)).ToArray();

            //у созданого метода проверяем типы параметров, чтобы имеющиеся у нас типы могли быть назначены параметрам метода
            concreteGenericMethod = concreteGenericMethod.Where(x => x.GetParameters().Select(p => p.ParameterType).SequenceEqual(paramTypes, new TestAssignable())).ToArray();

            //выбираем первый удовлетворяющий всем условиям метод. 
            return concreteGenericMethod.FirstOrDefault();

            /*            var methods =
                            //выбираем у типа все методы
                            from abstractGenericMethod in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                //фильтруем по имени
                            where abstractGenericMethod.Name == name
                            //интересуют только generic-методы
                            where abstractGenericMethod.IsGenericMethod
                            //выбираем параметры, которые принимает метод
                            let pa = abstractGenericMethod.GetParameters()
                            //отбрасываем методы принимающие иное количество параметров
                            where pa.Length == paramTypes.Length
                            //создаем конкретный метод, указывая типы шаблона
                            select abstractGenericMethod.MakeGenericMethod(genericTypeArgs) into concreteGenericMethod
                            //у созданого метода проверяем типы параметров, чтобы имеющиеся у нас типы могли быть назначены параметрам метода
                            where concreteGenericMethod.GetParameters().Select(p => p.ParameterType).SequenceEqual(paramTypes, new TestAssignable())
                            select concreteGenericMethod;
            */
            //выбираем первый удовлетворяющий всем условиям метод. 
            //return methods.FirstOrDefault();
        }
    }

    class TestAssignable : IEqualityComparer<Type>
    {
        public bool Equals(Type? x, Type? y)
        {
            if (x == null)
            {
                if (y == null)
                    return true;
                else
                    return false;
            }

            if (y == null)
                return false;

            //Если тип значение типа y может использовано в качестве значения типа x, то нас это устраивает
            return x.IsAssignableFrom(y);
        }

        public int GetHashCode(Type obj) => obj.GetHashCode();
    }

}
