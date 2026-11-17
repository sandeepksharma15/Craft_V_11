using Craft.Domain.Abstractions;
using Craft.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Craft.Repositories.Tests;

public class ValidationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Changes_RejectDetachedAddedDeletedAndUnsupportedEntities()
    {
        using var f = new Fixture(); using var db = f.Open(); var repository = new Repository<Product, int, ShopContext>(db, Fixture.Limits);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!, cancellationToken: Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(new Product(), cancellationToken: Token));
        var row = await repository.AddAsync(new Product(), false, Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(row, cancellationToken: Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(row, cancellationToken: Token));
        await repository.SaveChangesAsync(Token);
        var detached = await repository.GetAsync(row.Id, cancellationToken: Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(detached!, cancellationToken: Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.PermanentlyDeleteAsync(row, cancellationToken: Token));
        db.Remove(row);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(row, cancellationToken: Token));
        db.Entry(row).State = EntityState.Unchanged;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.DeleteAsync(row, cancellationToken: new(true)));
        var tags = new Repository<Tag, Guid, ShopContext>(db, Fixture.Limits);
        var tag = await tags.AddAsync(new Tag(), cancellationToken: Token);
        await Assert.ThrowsAsync<NotSupportedException>(() => tags.RestoreAsync(tag, cancellationToken: Token));
        Assert.Throws<NotSupportedException>(() => tags.LoadDeletedAsync(tag.Id, cancellationToken: Token));
        await tags.DeleteAsync(tag, cancellationToken: Token);
        Assert.Null(await tags.GetAsync(tag.Id, cancellationToken: Token));
        using var noSoft = f.Open(softDelete: false); var unsafeRepository = new Repository<Product, int, ShopContext>(noSoft, Fixture.Limits);
        var loaded = await unsafeRepository.LoadForUpdateAsync(row.Id, cancellationToken: Token);
        await Assert.ThrowsAsync<NotSupportedException>(() => unsafeRepository.DeleteAsync(loaded!, cancellationToken: Token));
        var codes = new Repository<Code, string, ShopContext>(db, Fixture.Limits);
        Assert.Throws<ArgumentNullException>(() => codes.GetAsync(null!, cancellationToken: Token));
        await codes.AddAsync(new Code(), cancellationToken: Token);
        Assert.NotNull(await codes.GetAsync("code", cancellationToken: Token));
    }

    [Fact]
    public async Task BatchValidation_HappensBeforeMutationsAndEnumeratesOnce()
    {
        using var f = new Fixture(); using var db = f.Open();
        var repository = new Repository<Product, int, ShopContext>(db, Options.Create(new RepositoryOptions { MaximumBatchSize = 2 }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddRangeAsync(null!, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddRangeAsync([new Product(), null!], cancellationToken: Token));
        Assert.Empty(db.ChangeTracker.Entries());
        var row = new Product();
        await Assert.ThrowsAsync<ArgumentException>(() => repository.AddRangeAsync([row, row], cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentException>(() => repository.AddRangeAsync([new Product(), new Product(), new Product()], cancellationToken: Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.AddRangeAsync([row], cancellationToken: new(true)));
        var enumerations = 0;
        IEnumerable<Product> Once() { enumerations++; yield return row; }
        await repository.AddRangeAsync(Once(), cancellationToken: Token);
        Assert.Equal(1, enumerations);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteRangeAsync([row, new Product()], cancellationToken: Token));
        Assert.Equal(EntityState.Unchanged, db.Entry(row).State);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void InvalidLimits_AreRejected(int invalid)
    {
        using var f = new Fixture(); using var db = f.Open(); var options = new RepositoryOptions();
        switch (invalid)
        {
            case 0: options.MaximumPageSize = 0; break;
            case 1: options.MaximumListSize = 0; break;
            case 2: options.MaximumListSize = int.MaxValue; break;
            case 3: options.MaximumBatchSize = 0; break;
            case 4: options.MaximumBatchSize = int.MaxValue; break;
        }
        Assert.Throws<OptionsValidationException>(() => new ReadRepository<Product, int, ShopContext>(db, Options.Create(options)));
    }

    [Fact]
    public void ConstructorsAndRegistration_ValidateArgumentsAndBindings()
    {
        using var f = new Fixture(); using var db = f.Open();
        Assert.Throws<ArgumentNullException>(() => new ReadRepository<Product, int, ShopContext>(null!, Fixture.Limits));
        Assert.Throws<ArgumentNullException>(() => new ReadRepository<Product, int, ShopContext>(db, null!));
        Assert.Throws<ArgumentNullException>(() => RepositoryRegistration.AddCraftRepositories<ShopContext>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddCraftRepositories<ShopContext>(null!));
        var services = new ServiceCollection();
        services.AddCraftDbContext<ShopContext>(o => o.UseSqlite(f.Connection), b => { foreach (var feature in Fixture.Features()) b.AddFeature(_ => feature); });
        services.AddCraftRepositories<ShopContext>(b => b.AddCustom<Product, int, DetailsRepository>().Add<Note>().Add<Tag, Guid>(), o => o.MaximumPageSize = 20);
        Assert.Throws<InvalidOperationException>(() => services.AddCraftRepositories<ShopContext>(b => b.Add<Product, int>()));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope(); var sp = scope.ServiceProvider;
        Assert.Same(sp.GetRequiredService<IRepository<Product, int>>(), sp.GetRequiredService<IReadRepository<Product, int>>());
        Assert.IsType<DetailsRepository>(sp.GetRequiredService<IReadRepository<Product, int>>());
        Assert.Same(sp.GetRequiredService<IRepository<Note>>(), sp.GetRequiredService<IReadRepository<Note>>());
        Assert.Same(sp.GetRequiredService<IRepository<Note>>(), sp.GetRequiredService<IRepository<Note, KeyType>>());
        Assert.Same(sp.GetRequiredService<IRepository<Note>>(), sp.GetRequiredService<IReadRepository<Note, KeyType>>());
        Assert.NotNull(sp.GetRequiredService<IRepository<Tag, Guid>>());
        Assert.NotNull(new ReadRepository<Note, ShopContext>(db, Fixture.Limits));
        var configured = new ServiceCollection(); configured.AddCraftRepositories<ShopContext>(_ => { }, o => o.MaximumPageSize = 0);
        using var invalid = configured.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => invalid.GetRequiredService<IOptions<RepositoryOptions>>().Value);
    }

    [Fact]
    public void KeyMappings_RequireTheEntityIdAsASinglePrimaryKey()
    {
        var options = new DbContextOptionsBuilder<InvalidContext>().UseSqlite("Data Source=:memory:").Options;
        using var db = new InvalidContext(options);
        Assert.Throws<NotSupportedException>(() => new ReadRepository<Unmapped, int, InvalidContext>(db, Fixture.Limits));
        Assert.Throws<NotSupportedException>(() => new ReadRepository<Keyless, int, InvalidContext>(db, Fixture.Limits));
        Assert.Throws<NotSupportedException>(() => new ReadRepository<Composite, int, InvalidContext>(db, Fixture.Limits));
        Assert.Throws<NotSupportedException>(() => new ReadRepository<WrongKey, int, InvalidContext>(db, Fixture.Limits));
        Assert.Throws<NotSupportedException>(() => new ReadRepository<ShadowKey, int, InvalidContext>(db, Fixture.Limits));
        Assert.Throws<NotSupportedException>(() => new ReadRepository<WrongType, int, InvalidContext>(db, Fixture.Limits));
    }
    private sealed class InvalidContext(DbContextOptions<InvalidContext> options) : CraftDbContext(options)
    {
        protected override void ConfigureModel(ModelBuilder m)
        {
            m.Entity<Keyless>().HasNoKey(); m.Entity<Composite>().HasKey(e => new { e.Id, e.Other });
            m.Entity<WrongKey>().HasKey(e => e.Other); m.Entity<ShadowKey>().Property<int>("Id"); m.Entity<ShadowKey>().HasKey("Id");
            m.Entity<WrongType>().HasKey(e => e.Id);
        }
    }
    private sealed class Unmapped : IEntity<int> { public int Id { get; set; } }
    private sealed class Keyless : IEntity<int> { public int Id { get; set; } }
    private sealed class Composite : IEntity<int> { public int Id { get; set; } public int Other { get; set; } }
    private sealed class WrongKey : IEntity<int> { public int Id { get; set; } public int Other { get; set; } }
    private sealed class ShadowKey : IEntity<int> { int Craft.Domain.Abstractions.IHasId<int>.Id { get; set; } }
    private sealed class WrongType : IEntity<int> { public Guid Id { get; set; } int Craft.Domain.Abstractions.IHasId<int>.Id { get; set; } }
}
