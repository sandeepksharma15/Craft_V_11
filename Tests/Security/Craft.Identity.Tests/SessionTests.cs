using System.ComponentModel.DataAnnotations;

namespace Craft.Identity.Tests;

public class SessionTests
{
    [Fact]
    public void RefreshSecrets_AreRandomAndOnlyHashesArePersisted()
    {
        var token = RefreshTokenSecret.Generate();
        Assert.Equal(64, token.Length); Assert.NotEqual(token, RefreshTokenSecret.Generate());
        var hash = RefreshTokenSecret.Hash(token); Assert.NotEqual(token, hash); Assert.Equal(hash, RefreshTokenSecret.Hash(token));
        Assert.Throws<ArgumentNullException>(() => RefreshTokenSecret.Hash(null!));
        Assert.Throws<ArgumentException>(() => RefreshTokenSecret.Hash(" "));
        var id = Guid.NewGuid(); var family = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var session = new RefreshSession<string>(id, family, "user", hash, now.AddDays(1), now, now);
        Assert.Equal(id, session.Id); Assert.Equal(family, session.FamilyId); Assert.Equal("user", session.UserId); Assert.Equal(hash, session.TokenHash);
        Assert.Equal(now.AddDays(1), session.ExpiresAt); Assert.Equal(now, session.ConsumedAt); Assert.Equal(now, session.RevokedAt);
        var result = new RefreshRotationResult<string>(RefreshRotationStatus.Rotated, session.UserId);
        Assert.Equal(RefreshRotationStatus.Rotated, result.Status); Assert.Equal("user", result.UserId);
    }

    [Fact]
    public void AccountRequests_ValidateEmailAndRequiredFields()
    {
        var create = new CreateUserRequest { ClientURI = "https://example.test", Email = "a@example.test", FirstName = "A", LastName = "B", Password = "secret" };
        Assert.Equal("https://example.test", create.ClientURI); Assert.Equal("A", create.FirstName); Assert.Equal("B", create.LastName); Assert.Equal("secret", create.Password);
        Assert.True(Validator.TryValidateObject(create, new ValidationContext(create), [], true));
        create.Email = "invalid"; Assert.False(Validator.TryValidateObject(create, new ValidationContext(create), [], true));
        var confirmation = new EmailConfirmationRequest { Email = "a@example.test", Token = "token" };
        Assert.Equal("token", confirmation.Token); Assert.True(Validator.TryValidateObject(confirmation, new ValidationContext(confirmation), [], true));
        confirmation.Email = "invalid"; Assert.False(Validator.TryValidateObject(confirmation, new ValidationContext(confirmation), [], true));
    }
}
