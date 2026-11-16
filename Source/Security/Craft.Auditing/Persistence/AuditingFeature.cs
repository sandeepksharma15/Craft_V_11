using Craft.Auditing.Capture;
using Craft.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Craft.Auditing.Persistence;

public sealed class AuditingFeature : PersistenceFeature
{
    private readonly AuditTrailFactory _factory;

    public AuditingFeature(AuditTrailFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public override void ConfigureModel(DbContext context, ModelBuilder modelBuilder) => modelBuilder.ConfigureAuditing();

    public override Action CaptureChanges(DbContext context)
    {
        var pending = _factory.Capture(context);
        return () => context.AddRange(pending.Select(audit => audit.Complete()));
    }
}

public static class AuditingRegistration
{
    extension(CraftDataBuilder builder)
    {
        public CraftDataBuilder UseAuditing() => builder.AddFeature(services => new AuditingFeature(services.GetRequiredService<AuditTrailFactory>()));
    }
}
