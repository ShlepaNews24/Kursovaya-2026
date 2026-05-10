using System.Collections.Generic;

namespace GamesPlatform.API.Models
{
    public class GamesQueryDto
    {
        public int PageNumber { get; set; } = 1;      // Номер страницы (по умолчанию 1)
        public int PageSize { get; set; } = 10;       // Количество элементов (по умолчанию 10)
        public string? Search { get; set; }           // Поиск по названию
        public int? GenreId { get; set; }             // Фильтр по жанру
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}