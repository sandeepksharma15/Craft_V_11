using Microsoft.EntityFrameworkCore;

namespace Craft.Data;

/// <summary>Features are immutable; per-save state belongs in the completion returned by CaptureChanges.</summary>
public enum PersistenceStage { Ownership, Deletion, Enrichment }

public abstract class PersistenceFeature
{
    public virtual PersistenceStage Stage => PersistenceStage.Enrichment;
    public virtual void ConfigureModel(DbContext context, ModelBuilder modelBuilder) { }
    public virtual void ValidateChanges(DbContext context) { }
    public virtual void PrepareChanges(DbContext context) { }
    public virtual Action? CaptureChanges(DbContext context) => null;
}
