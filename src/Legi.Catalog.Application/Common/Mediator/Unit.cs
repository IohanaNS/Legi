namespace Legi.Catalog.Application.Common.Mediator;

/// <summary>
/// Represents a void type, since void is not a valid return type in C#.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// Default and only value of the Unit type.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    /// <param name="other">The Unit to compare with this object.</param>
    /// <returns>Always returns true since all Unit values are equal.</returns>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Compares the current object with another object.
    /// </summary>
    /// <param name="obj">The object to compare with this object.</param>
    /// <returns>True if the object is a Unit; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>Always returns 0 since all Unit values are equal.</returns>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns the string representation of the Unit value.
    /// </summary>
    /// <returns>Returns "()".</returns>
    public override string ToString() => "()";

    public static bool operator ==(Unit left, Unit right) => true;

    public static bool operator !=(Unit left, Unit right) => false;
}