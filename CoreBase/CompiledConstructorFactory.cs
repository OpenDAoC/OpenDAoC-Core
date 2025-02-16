using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DOL
{
    public static class CompiledConstructorFactory
    {
        public static Delegate CompileConstructor(Type type, Type[] paramTypes)
        {
            ConstructorInfo constructor = type.GetConstructor(paramTypes);

            if (constructor == null)
                throw new InvalidOperationException($"Type {type.FullName} does not have a matching constructor.");

            ParameterExpression[] parameters = paramTypes.Select(Expression.Parameter).ToArray();
            NewExpression newExpression = Expression.New(constructor, parameters);
            Type delegateType = Expression.GetFuncType(paramTypes.Concat([type]).ToArray());
            return Expression.Lambda(delegateType, newExpression, parameters).Compile();
        }
    }
}
