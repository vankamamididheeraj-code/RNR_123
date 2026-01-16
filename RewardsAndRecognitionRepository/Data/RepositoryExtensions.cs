using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RewardsAndRecognitionRepository.Data
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Projects and pages an <see cref="IQueryable{TSource}"/> without loading the full set into memory.
        /// </summary>
        public static async Task<PagedResult<TDto>> ToPagedResultAsync<TSource, TDto>(
            this IQueryable<TSource> query,
            int pageNumber,
            int pageSize,
            Func<IQueryable<TSource>, IQueryable<TDto>> projector,
            CancellationToken ct = default)
            where TSource : class
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            const int MaxPageSize = 200;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var q = query.AsNoTracking();

            var total = await q.CountAsync(ct);

            var skipped = (pageNumber - 1) * pageSize;

            var pageQuery = q.Skip(skipped).Take(pageSize);

            var projected = projector(pageQuery);

            var items = await projected.ToArrayAsync(ct);

            return new PagedResult<TDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }
        /// <summary>
        /// Lightweight paged result used by repository paging helper.
        /// </summary>
        public class PagedResult<T>
        {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public long TotalCount { get; set; }
            public T[] Items { get; set; } = Array.Empty<T>();
        }
    }
}
