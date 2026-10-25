#if !NET5_0_OR_GREATER && !NET6_0_OR_GREATER && !NET7_0_OR_GREATER && !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Needed for C# 9 init-only setters
    public sealed class IsExternalInit { }
}
#endif

#if !NET7_0_OR_GREATER && !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Needed for C# 11 required members
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName) { }
        public bool IsOptional { get; init; }
    }
}
#endif

#if !NET6_0_OR_GREATER && !NET7_0_OR_GREATER && !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Needed for C# 10 CallerArgumentExpression
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName) { }
    }
}
#endif

#if !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Preview shim for C# 12 collection expressions
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName) { }
    }
}
#endif