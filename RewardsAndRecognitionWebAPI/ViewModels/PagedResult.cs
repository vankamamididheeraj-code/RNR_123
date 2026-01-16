// filepath: c:\Users\VV018423\source\repos\Intern-Projects\2026\RNR_RewardsAndRecognition\RewardsAndRecognitionWebAPI\ViewModels\PagedResult.cs
using System;

namespace RewardsAndRecognitionWebAPI.ViewModels
{
    /// <summary>
    /// Generic paged result returned by list endpoints.
    /// </summary>
    /// <typeparam name="T">Type of items in the page.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// 1-based page number.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items requested per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items available across all pages.
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// The items on the current page.
        /// </summary>
        public T[] Items { get; set; } = Array.Empty<T>();
    }
}
