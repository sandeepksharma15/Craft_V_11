namespace Craft.Auditing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public sealed class DoNotAuditAttribute : Attribute;