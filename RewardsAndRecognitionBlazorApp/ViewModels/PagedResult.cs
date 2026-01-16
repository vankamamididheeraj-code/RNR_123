// filepath: c:\Users\VV018423\source\repos\Intern-Projects\2026\RNR_RewardsAndRecognition\RewardsAndRecognitionBlazorApp\ViewModels\PagedResult.cs
using System;

namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    /// <summary>
    /// Client-side copy of the server PagedResult for deserialization.
    /// </summary>
    public class PagedResult<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public T[] Items { get; set; } = Array.Empty<T>();
    }
}
