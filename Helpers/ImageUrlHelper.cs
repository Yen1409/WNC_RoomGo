using Microsoft.AspNetCore.Http;
using RoomGoHanoi.Models;

namespace RoomGoHanoi.Helpers;

public static class ImageUrlHelper
{
    public static string ResolveUrl(HttpContext? context, string? url)
    {
        // Nếu URL null hoặc rỗng -> trả về ảnh mặc định
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/images/rooms/default-room.svg";
        }

        // Nếu URL đã là đường dẫn tuyệt đối (http, https)
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        // Nếu URL bắt đầu bằng "/" -> giữ nguyên
        if (url.StartsWith("/"))
        {
            return url;
        }

        // Nếu URL không bắt đầu bằng "/" -> thêm vào
        return $"/{url.TrimStart('~', '/')}";
    }

    // Phương thức lấy ảnh đầu tiên của listing
    public static string GetFirstImage(Listing listing)
    {
        if (listing == null || listing.Images == null || listing.Images.Count == 0)
        {
            return "/images/rooms/default-room.svg";
        }

        var url = listing.Images.FirstOrDefault()?.Url;
        return ResolveUrl(null, url);
    }
}