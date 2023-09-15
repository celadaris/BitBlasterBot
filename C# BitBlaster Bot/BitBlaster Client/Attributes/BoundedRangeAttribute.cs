using System;

[AttributeUsage(AttributeTargets.Property)]
internal class Vector2BoundsAttribute : Attribute
{
    public float[] xBounds { get; set; }
    public float[] yBounds { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
internal class Vector3BoundsAttribute : Attribute
{
    public float[] xBounds { get; set; }
    public float[] yBounds { get; set; }
    public float[] zBounds { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
internal class Vector4BoundsAttribute : Attribute
{
    public float[] xBounds { get; set; }
    public float[] yBounds { get; set; }
    public float[] zBounds { get; set; }
    public float[] wBounds { get; set; }
}