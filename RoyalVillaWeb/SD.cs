namespace RoyalVillaWeb
{
    public static class SD
    {
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
        public const string SessionToken = "JWTToken";
        public const string CurrentAPIVersion = "v2";
        public const string APIBaseUrl = "https://localhost:7297";
        
        public static string GetImageUrl(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return "/images/placeholder-villa.jpg"; // Fallback placeholder
            }
            
            // If imageUrl is already a full URL, return as is
            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
            {
                return imageUrl;
            }
            
            // If it's a relative path from API, construct full URL
            return $"{APIBaseUrl}{imageUrl}";
        }
    }
}
