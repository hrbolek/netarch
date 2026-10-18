using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace Core.Host.Integration.Tests.SecurityBoundary;

internal static class CompiledBoundaryInspector
{
    private static readonly IReadOnlyDictionary<short, OpCode> Codes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(code => code.Value);

    internal static bool IsPersistence(Type type)
    {
        if (type.HasElementType)
            return IsPersistence(type.GetElementType()!);
        return typeof(DbContext).IsAssignableFrom(type) ||
            typeof(IDBLayer).IsAssignableFrom(type) ||
            typeof(IAuthorizationLayer).IsAssignableFrom(type) ||
            typeof(System.Data.Common.DbConnection).IsAssignableFrom(type) ||
            typeof(System.Data.Common.DbCommand).IsAssignableFrom(type) ||
            typeof(System.Data.Common.DbDataSource).IsAssignableFrom(type) ||
            typeof(System.Data.IDbConnection).IsAssignableFrom(type) ||
            typeof(System.Data.IDbCommand).IsAssignableFrom(type) ||
            typeof(DatabaseFacade).IsAssignableFrom(type) ||
            type.GetInterfaces().Append(type).Any(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IBaseStore<>)) ||
            type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(DbSet<>) ||
                type.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
            type.IsGenericType && type.GetGenericArguments().Any(IsPersistence);
    }

    internal static IEnumerable<string> Inspect(Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            if (IsPersistence(field.FieldType))
                yield return $"{type.FullName}.{field.Name}: field of persistence type {field.FieldType}";

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)))
        {
            foreach (var parameter in method.GetParameters())
                if (IsPersistence(parameter.ParameterType))
                    yield return $"{type.FullName}.{method.Name}: parameter {parameter.ParameterType}";
            if (method is MethodInfo info && IsPersistence(info.ReturnType))
                yield return $"{type.FullName}.{method.Name}: returns persistence type {info.ReturnType}";

            foreach (var member in ReferencedMembers(method))
            {
                var dependency = member as Type ?? member.DeclaringType;
                if (dependency is not null && IsPersistence(dependency) ||
                    member is MethodBase target && target.GetParameters().Any(parameter => IsPersistence(parameter.ParameterType)) ||
                    member is MethodInfo targetMethod && (IsPersistence(targetMethod.ReturnType) ||
                        targetMethod.GetGenericArguments().Any(IsPersistence)) ||
                    member is FieldInfo targetField && IsPersistence(targetField.FieldType))
                    yield return $"{type.FullName}.{method.Name}: compiled reference to {member.DeclaringType?.FullName}.{member.Name}";
            }
        }
    }

    internal static IEnumerable<MemberInfo> ReferencedMembers(MethodBase method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes is null)
            yield break;
        var position = 0;
        while (position < bytes.Length)
        {
            short value = bytes[position++];
            if (value == 0xfe)
                value = (short)(0xfe00 | bytes[position++]);
            var code = Codes[value];
            var size = code.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, position),
                _ => 4
            };
            if (code.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
            {
                var token = BitConverter.ToInt32(bytes, position);
                var member = method.Module.ResolveMember(token, method.DeclaringType?.GetGenericArguments(),
                    method is MethodInfo info ? info.GetGenericArguments() : null);
                if (member is not null)
                    yield return member;
            }
            position += size;
        }
    }
}
