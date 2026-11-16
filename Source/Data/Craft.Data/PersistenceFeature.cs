using Microsoft.EntityFrameworkCore;

namespace Craft.Data;

/// <summary>Features are immutable; per-save state belongs in the completion returned by CaptureChanges.</summary>
public abstract class PersistenceFeature
{
    public virtual void ConfigureModel(DbContext context, ModelBuilder modelBuilder) { }
    public virtual void ValidateChanges(DbContext context) { }
    public virtual void PrepareChanges(DbContext context) { }
    public virtual Action? CaptureChanges(DbContext context) => null;
}
