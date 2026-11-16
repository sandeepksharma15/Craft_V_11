using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Craft.Authentication.Tests;

public class CurrentUserTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("abc", false)]
    [InlineData("0", true)]
    [InlineData("-42", true)]
    public void NumericIds_AreParsedWithoutMutatingIdentity(string? id, bool expected)
    {
        var claims = id is null ? Array.Empty<Claim>() : [new Claim("sub", id)];
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "trusted")) } };
        var user = new CurrentUser<long>(accessor);
        Assert.True(user.IsAuthenticated);
        Assert.Equal(expected, user.TryGetId(out _));
    }

    [Fact]
    public void GuidStringAndMissingContexts_AreSupported()
    {
        Assert.Throws<ArgumentNullException>(() => new CurrentUser(null!));
        var accessor = new HttpContextAccessor(); var user = new CurrentUser(accessor);
        Assert.False(user.IsAuthenticated); Assert.Null(user.Name); Assert.False(user.TryGetId(out _));
        accessor.HttpContext = new DefaultHttpContext(); Assert.False(user.IsAuthenticated);
        var id = Guid.NewGuid();
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new("sub", id.ToString()), new(ClaimTypes.Name, "User")], "trusted"));
        Assert.True(new CurrentUser<Guid>(accessor).TryGetId(out var parsed)); Assert.Equal(id, parsed);
        Assert.True(new CurrentUser<string>(accessor).TryGetId(out var text)); Assert.Equal(id.ToString(), text); Assert.Equal("User", user.Name);
        accessor.HttpContext.User.AddIdentity(new ClaimsIdentity([new("sub", "untrusted")]));
        Assert.True(new CurrentUser<Guid>(accessor).TryGetId(out _));
        accessor.HttpContext.User.AddIdentity(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "conflict")], "other"));
        Assert.False(new CurrentUser<Guid>(accessor).TryGetId(out _));
    }
}
