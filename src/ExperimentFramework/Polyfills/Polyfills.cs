#if NETSTANDARD2_1
global using static System.ArgumentNullExceptionPolyfill;
global using static System.Threading.Tasks.ValueTaskPolyfill;
global using static System.Linq.EnumerablePolyfill;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for init-only properties in .NET Standard 2.1
    /// </summary>
    internal static class IsExternalInit
    {
    }
    
    /// <summary>
    /// Polyfill for required members in .NET Standard 2.1
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }

    /// <summary>
    /// Polyfill for compiler feature required attribute in .NET Standard 2.1
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }
        public bool IsOptional { get; init; }
    }
}

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Polyfill for ArgumentNullException.ThrowIfNull in .NET Standard 2.1
    /// </summary>
    internal static class ArgumentNullExceptionPolyfill
    {
        public static void ThrowIfNull(object? argument, string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}

// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks
{
    /// <summary>
    /// Polyfill for ValueTask.FromResult in .NET Standard 2.1
    /// </summary>
    internal static class ValueTaskPolyfill
    {
        public static ValueTask<T> FromResult<T>(T result)
        {
            return new ValueTask<T>(result);
        }
    }
}

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    /// <summary>
    /// Polyfill for Order extension methods in .NET Standard 2.1
    /// </summary>
    internal static class EnumerablePolyfill
    {
        public static IEnumerable<T> Order<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => x);
        }
        
        public static IEnumerable<T> OrderDescending<T>(this IEnumerable<T> source)
        {
            return source.OrderByDescending(x => x);
        }
    }
}
#endif
