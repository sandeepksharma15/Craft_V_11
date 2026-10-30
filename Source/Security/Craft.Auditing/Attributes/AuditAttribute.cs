namespace Craft.Auditing.Attributes;

/// <summary>
/// Opts an entity or individual property into auditing. DoNotAudit takes precedence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public sealed class AuditAttribute : Attribute;
