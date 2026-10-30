using System.ComponentModel;

namespace Craft.Auditing.Model;

public enum AuditOperation
{
    [Description("Created")]
    Created,

    [Description("Updated")]
    Updated,

    [Description("Deleted")]
    Deleted,

    [Description("SoftDeleted")]
    SoftDeleted,

    [Description("Restored")]
    Restored
}
