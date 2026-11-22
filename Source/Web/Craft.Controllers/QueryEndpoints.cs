using System.Linq.Expressions;
using Craft.Core.Pagination;
using Craft.Core.Results;
using Craft.Domain.Abstractions;
using Craft.QuerySpec;
using Craft.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Craft.Controllers;

internal static class QueryEndpoints
{
    internal static async Task<ActionResult<Page<TResponse>>> SearchAsync<TEntity, TResponse, TKey>(ControllerBase controller,
        IQueryReadRepository<TEntity, TKey> repository, QueryPolicy<TEntity> policy,
        Expression<Func<TEntity, TResponse>> projection, QueryRequest request, int maximumPageSize, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TKey>
        where TKey : notnull
    {
        if (request.PageSize > maximumPageSize)
            return Invalid(controller, [new Error("query.invalid", "Page size exceeds the repository limit.", ErrorKind.Validation)]);
        Result<Query<TEntity>> result = policy.Build(request);
        if (result.IsFailure)
            return Invalid(controller, result.Errors);
        Page<TResponse> page = await repository.PageAsync(result.Value, projection, request.Page, request.PageSize, cancellationToken);
        return controller.Ok(page);
    }

    internal static async Task<ActionResult<long>> CountAsync<TEntity, TKey>(ControllerBase controller,
        IQueryReadRepository<TEntity, TKey> repository, QueryPolicy<TEntity> policy,
        QueryRequest request, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TKey>
        where TKey : notnull
    {
        Result<Query<TEntity>> result = policy.Build(request);
        if (result.IsFailure)
            return Invalid(controller, result.Errors);
        return controller.Ok(await repository.CountAsync(result.Value, cancellationToken));
    }

    internal static async Task<ActionResult<bool>> AnyAsync<TEntity, TKey>(ControllerBase controller,
        IQueryReadRepository<TEntity, TKey> repository, QueryPolicy<TEntity> policy,
        QueryRequest request, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TKey>
        where TKey : notnull
    {
        Result<Query<TEntity>> result = policy.Build(request);
        if (result.IsFailure)
            return Invalid(controller, result.Errors);
        return controller.Ok(await repository.AnyAsync(result.Value, cancellationToken));
    }

    private static ActionResult Invalid(ControllerBase controller, IReadOnlyList<Error> errors)
    {
        foreach (Error error in errors)
            controller.ModelState.AddModelError("query", error.Message);
        return controller.ValidationProblem(controller.ModelState);
    }
}
