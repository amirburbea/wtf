#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Castle.Windsor;
using ExcelDna.Integration;
using ExcelDna.Registration;

namespace Beacon.Excel.Data
{
    /// <summary>
    /// ExcelDNA requires static functions for registration, but for use within a dependency injection container, the functions are
    /// declared as instance methods on <see cref="IDataFunctions"/>.
    /// <para />
    /// This class generates a type with static methods invoking those methods on the <see cref="IDataFunctions"/> instance returned by the container.
    /// </summary>
    public static class DataFunctionsRegistration
    {
        private static readonly ConstructorInfo _optionalAttributeConstructor = typeof(OptionalAttribute).GetConstructor(Type.EmptyTypes);
        private static readonly Type _type = DataFunctionsRegistration.CreateType();

        public static void RegisterDataFunctions()
        {
            ExcelRegistration.RegisterFunctions(
                Array.ConvertAll(
                    DataFunctionsRegistration._type.GetMethods(BindingFlags.Public | BindingFlags.Static),
                    method => new ExcelFunctionRegistration(method)
                )
            );
        }

        /// <summary>
        /// For each instance method on <see cref="IDataFunctions"/>, an equivalent <see langword="static"/> method is created.
        /// </summary>
        private static MethodBuilder CreateMethod(TypeBuilder typeBuilder, MethodInfo method, Expression functionsExpression)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type[] types = parameters.Length == 0 ? Type.EmptyTypes : Array.ConvertAll(parameters, parameter => parameter.ParameterType);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard | (method.CallingConvention & CallingConventions.VarArgs),
                method.ReturnType,
                types
            );
            ExcelFunctionAttribute? functionAttribute = method.GetCustomAttribute<ExcelFunctionAttribute>();
            if (functionAttribute != null)
            {
                methodBuilder.SetCustomAttribute(AttributeBuilderFactory.CreateCustomAttributeBuilder(functionAttribute));
            }
            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];
                ParameterBuilder parameterBuilder = methodBuilder.DefineParameter(index + 1, parameter.Attributes, parameter.Name);
                if (parameter.IsOptional)
                {
                    parameterBuilder.SetCustomAttribute(new CustomAttributeBuilder(DataFunctionsRegistration._optionalAttributeConstructor, Array.Empty<object>()));
                }
                ExcelArgumentAttribute? argumentAttribute = parameter.GetCustomAttribute<ExcelArgumentAttribute>();
                if (argumentAttribute != null)
                {
                    parameterBuilder.SetCustomAttribute(AttributeBuilderFactory.CreateCustomAttributeBuilder(argumentAttribute));
                }
                if (parameter.HasDefaultValue)
                {
                    parameterBuilder.SetConstant(parameter.DefaultValue);
                }
            }
            /*
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldnull); // Works??
            generator.Emit(OpCodes.Ret);
            */

            ParameterExpression[] parameterExpressions = parameters.Length == 0 ? Array.Empty<ParameterExpression>() : Array.ConvertAll(
                parameters,
                parameter => Expression.Parameter(
                    parameter.ParameterType,
                    parameter.Name
                )
            );
            LambdaExpression lambdaExpression = Expression.Lambda(
                Expression.Call(
                    functionsExpression,
                    method,
                    parameterExpressions
                ),
                parameterExpressions
            );
            lambdaExpression.CompileToMethod(methodBuilder);
            return methodBuilder;
        }

        private static Type CreateType()
        {
            string name = string.Concat(typeof(DataFunctionsRegistration).Namespace, ".Functions");
            AssemblyBuilder dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module = dynamicAssembly.DefineDynamicModule(nameof(Module), string.Concat(name, ".dll"));
            TypeBuilder typeBuilder = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);
            Expression functionsExpression = Expression.Call(
                Expression.Field(
                    null,
                    typeof(Container).GetField(nameof(Container.Instance), BindingFlags.Public | BindingFlags.Static)
                ),
                nameof(IWindsorContainer.Resolve),
                new[] { typeof(IDataFunctions) }
            );
            foreach (MethodInfo method in typeof(IDataFunctions).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                DataFunctionsRegistration.CreateMethod(typeBuilder, method, functionsExpression);
            }
            return typeBuilder.CreateType();
        }

        private static class AttributeBuilderFactory
        {
            private static readonly MethodInfo _addMethod = typeof(List<(FieldInfo, object)>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            private static readonly object[] _emptyArray = Array.Empty<object>();
            private static readonly ConstructorInfo _listConstructor = typeof(List<(FieldInfo, object)>).GetConstructor(Type.EmptyTypes);
            private static readonly ConstructorInfo _valueTupleConstructor = typeof((FieldInfo, object)).GetConstructor(new[] { typeof(FieldInfo), typeof(object) });

            public static CustomAttributeBuilder CreateCustomAttributeBuilder<T>(T attribute)
                where T : Attribute
            {
                return CustomAttributeBuilderFactory<T>.Create(attribute);
            }

            private sealed class CustomAttributeBuilderFactory<T>
                where T : Attribute
            {
                private static readonly ConstructorInfo _constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                private static readonly Func<T, List<(FieldInfo, object)>> _getList = CustomAttributeBuilderFactory<T>.CreateGetList();

                public static CustomAttributeBuilder Create(T attribute)
                {
                    List<(FieldInfo, object)> list = CustomAttributeBuilderFactory<T>._getList(attribute);
                    if (list.Count == 0)
                    {
                        return new CustomAttributeBuilder(CustomAttributeBuilderFactory<T>._constructor, AttributeBuilderFactory._emptyArray);
                    }
                    FieldInfo[] fields = new FieldInfo[list.Count];
                    object[] values = new object[list.Count];
                    for (int index = 0; index < list.Count; index++)
                    {
                        (fields[index], values[index]) = list[index];
                    }
                    return new CustomAttributeBuilder(CustomAttributeBuilderFactory<T>._constructor, AttributeBuilderFactory._emptyArray, fields, values);
                }

                private static Func<T, List<(FieldInfo, object)>> CreateGetList()
                {
                    ParameterExpression attribute = Expression.Parameter(typeof(T));
                    ParameterExpression list = Expression.Variable(typeof(List<(FieldInfo, object)>));
                    List<Expression> statements = new List<Expression>
                    {
                        Expression.Assign(
                            list,
                            Expression.New(
                                AttributeBuilderFactory._listConstructor
                            )
                        )
                    };
                    statements.AddRange(
                        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(field => Expression.IfThen(
                            Expression.NotEqual(
                                Expression.Field(
                                    attribute,
                                    field
                                ),
                                Expression.Default(
                                    field.FieldType
                                )
                            ),
                            Expression.Call(
                                list,
                                AttributeBuilderFactory._addMethod,
                                Expression.New(
                                    AttributeBuilderFactory._valueTupleConstructor,
                                    Expression.Constant(
                                        field
                                    ),
                                    Expression.Convert(
                                        Expression.Field(
                                            attribute,
                                            field
                                        ),
                                        typeof(object)
                                    )
                                )
                            )
                        ))
                    );
                    statements.Add(list);
                    return Expression.Lambda<Func<T, List<(FieldInfo, object)>>>(
                        Expression.Block(
                            new[] { list },
                            statements
                        ),
                        new[] { attribute }
                    ).Compile();
                }
            }
        }
    }
}
