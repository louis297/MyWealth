using MyWealth.Application.Common.Models;

namespace MyWealth.Application.Common.Mappings;

public static class MappingExtensions
{
    public static Task<PaginatedList<TDestination>> ProjectToListAsync<TDestination>(
        this IQueryable queryable,
        IConfigurationProvider configuration,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        where TDestination : class
        => PaginatedList<TDestination>.CreateAsync(
            queryable.ProjectTo<TDestination>(configuration).AsNoTracking(),
            pageNumber,
            pageSize,
            cancellationToken);
}
