namespace Craft.Auditing.Attributes;

/// <summary>
/// Records that a value changed without storing its contents.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class AuditRedactAttribute : Attribute;
