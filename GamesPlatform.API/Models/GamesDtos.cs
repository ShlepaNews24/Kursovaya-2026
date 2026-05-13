namespace GamesPlatform.API.Models
{
    public class GameDto
    {
        public int GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? Logo { get; set; }
        public int GenreId { get; set; }
        public int DeveloperId { get; set; }
        public string GameUrl { get; set; } = string.Empty;
        public string? GenreName { get; set; }
        public string? DeveloperName { get; set; }
    }

    public class GamesQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? GenreId { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}