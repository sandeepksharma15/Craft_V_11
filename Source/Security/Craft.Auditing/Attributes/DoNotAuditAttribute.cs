namespace Craft.Auditing.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public sealed class DoNotAuditAttribute : Attribute;
