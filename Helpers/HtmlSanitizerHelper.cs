using System.Text.RegularExpressions;
using Ganss.Xss;

namespace RoomGoHanoi.Helpers;

public static class HtmlSanitizerHelper
{
    private static readonly HtmlSanitizer _sanitizer;

    static HtmlSanitizerHelper()
    {
        _sanitizer = new HtmlSanitizer();
        
        // Cấu hình allowed tags - Sử dụng phương thức khác
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("b");
        _sanitizer.AllowedTags.Add("i");
        _sanitizer.AllowedTags.Add("u");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("span");
        _sanitizer.AllowedTags.Add("div");
        _sanitizer.AllowedTags.Add("a");
        
        // Cấu hình allowed attributes
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("id");
        _sanitizer.AllowedAttributes.Add("rel");
        
        // Cấu hình allowed schemes
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
        
        // Cấu hình other options
        _sanitizer.KeepChildNodes = true;
    }

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Loại bỏ thẻ script
        input = Regex.Replace(input, @"<script.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Loại bỏ sự kiện onclick, onload,...
        input = Regex.Replace(input, @"on\w+\s*=\s*""[^""]*""", string.Empty, RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"on\w+\s*=\s*'[^']*'", string.Empty, RegexOptions.IgnoreCase);
        
        // Loại bỏ javascript:
        input = Regex.Replace(input, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
        
        // Sử dụng HtmlSanitizer
        input = _sanitizer.Sanitize(input);
        
        return input;
    }

    public static string SanitizePlainText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        // Loại bỏ tất cả thẻ HTML
        input = Regex.Replace(input, @"<[^>]*>", string.Empty);
        
        // Loại bỏ javascript
        input = Regex.Replace(input, @"javascript:", string.Empty, RegexOptions.IgnoreCase);
        
        // Giải mã HTML entities
        input = System.Net.WebUtility.HtmlDecode(input);
        
        return input;
    }

    public static bool ContainsDangerousContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var dangerousPatterns = new[]
        {
            @"<script",
            @"javascript:",
            @"onclick",
            @"onload",
            @"onerror",
            @"onmouse",
            @"onkey",
            @"onfocus",
            @"onblur",
            @"<iframe",
            @"<object",
            @"<embed",
            @"<applet",
            @"<form",
            @"<input",
            @"<button",
            @"data:text/html",
            @"vbscript:"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    // Phương thức mới: Kiểm tra và sanitize an toàn
    public static string SanitizeSafe(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Nếu chứa nội dung độc hại, chỉ giữ lại text thuần
        if (ContainsDangerousContent(input))
        {
            return SanitizePlainText(input);
        }

        return Sanitize(input);
    }
}