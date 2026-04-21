namespace Alchemist.Domain.Colors
{
    /// <summary>Immutable recipe record (A + B => Output), loadable from ScriptableObject.</summary>
    public readonly struct Recipe
    {
        /// <summary>First input color.</summary>
        public readonly ColorId A;
        /// <summary>Second input color.</summary>
        public readonly ColorId B;
        /// <summary>Resulting output color.</summary>
        public readonly ColorId Output;
        /// <summary>True if recipe requires a Prism wildcard participant.</summary>
        public readonly bool RequiresPrism;

        /// <summary>Construct a recipe tuple.</summary>
        public Recipe(ColorId a, ColorId b, ColorId output, bool requiresPrism = false)
        {
            A = a;
            B = b;
            Output = output;
            RequiresPrism = requiresPrism;
        }
    }
}
