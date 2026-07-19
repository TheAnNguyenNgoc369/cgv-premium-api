using System.Text.RegularExpressions;
using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public sealed partial class IntentRouter : IIntentRouter
{
    public Task<IntentResult> ClassifyIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        var normalized = message.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(new IntentResult { Intent = "general", Confidence = 1.0m });
        }

        var movieScore = 0m;
        var fbScore = 0m;
        var promotionScore = 0m;
        var supportScore = 0m;
        string? extractedGenre = null;
        string? extractedKeyword = null;

        // Movie intent signals
        var movieKeywords = new Dictionary<string, decimal>
        {
            ["phim"] = 1.0m, ["movie"] = 1.0m, ["movies"] = 1.0m, ["film"] = 1.0m,
            ["đang chiếu"] = 1.0m, ["sắp chiếu"] = 1.0m, ["now showing"] = 1.0m,
            ["showtime"] = 0.9m, ["suất chiếu"] = 0.9m, ["lịch chiếu"] = 0.9m,
            ["recommend"] = 0.8m, ["recommend me"] = 0.9m, ["gợi ý"] = 0.9m,
            ["đề xuất"] = 0.9m, ["suggest"] = 0.8m, ["suggest me"] = 0.9m,
            ["thể loại"] = 0.8m, ["genre"] = 0.8m, ["genres"] = 0.8m,
            ["hành động"] = 0.9m, ["action"] = 0.9m,
            ["tình cảm"] = 0.9m, ["romance"] = 0.9m, ["romantic"] = 0.9m,
            ["hài"] = 0.9m, ["comedy"] = 0.9m, ["hài hước"] = 0.9m,
            ["kinh dị"] = 0.9m, ["horror"] = 0.9m,
            ["viễn tưởng"] = 0.9m, ["sci-fi"] = 0.9m, ["science fiction"] = 0.9m,
            ["hoạt hình"] = 0.9m, ["animation"] = 0.9m, ["animated"] = 0.9m,
            ["phiêu lưu"] = 0.9m, ["adventure"] = 0.9m,
            ["hình sự"] = 0.9m, ["crime"] = 0.9m,
            ["chiến tranh"] = 0.9m, ["war"] = 0.9m,
            ["âm nhạc"] = 0.9m, ["music"] = 0.9m, ["musical"] = 0.9m,
            ["thuyết minh"] = 0.9m, ["subtitle"] = 0.9m, ["phụ đề"] = 0.9m,
            ["diễn viên"] = 0.8m, ["actor"] = 0.8m, ["actress"] = 0.8m,
            ["đạo diễn"] = 0.8m, ["director"] = 0.8m,
            ["tuổi"] = 0.7m, ["age rating"] = 0.7m, ["cấm"] = 0.7m,
            ["thời lượng"] = 0.7m, ["duration"] = 0.7m, ["length"] = 0.7m,
            ["poster"] = 0.6m, ["trailer"] = 0.6m, ["review"] = 0.6m,
            ["rating"] = 0.6m, ["đánh giá"] = 0.6m, ["stars"] = 0.6m,
        };

        // F&B intent signals
        var fbKeywords = new Dictionary<string, decimal>
        {
            ["đồ ăn"] = 1.0m, ["đồ uống"] = 1.0m, ["food"] = 1.0m, ["drink"] = 1.0m,
            ["snack"] = 1.0m, ["bắp"] = 1.0m, ["popcorn"] = 1.0m, ["nước"] = 0.9m,
            ["combo"] = 0.9m, ["menu"] = 0.8m, ["món"] = 0.8m,
            ["giá"] = 0.6m, ["price"] = 0.6m, ["bao nhiêu"] = 0.6m,
            ["mua"] = 0.5m, ["buy"] = 0.5m, ["order"] = 0.5m,
            ["cola"] = 0.9m, ["pepsi"] = 0.9m, ["coca"] = 0.9m,
            ["kem"] = 0.8m, ["ice cream"] = 0.8m,
            ["ráng"] = 0.7m, ["hot dog"] = 0.7m,
        };

        // Promotion intent signals
        var promoKeywords = new Dictionary<string, decimal>
        {
            ["voucher"] = 1.0m, ["khuyến mãi"] = 1.0m, ["khuyen mai"] = 1.0m,
            ["promotion"] = 1.0m, ["discount"] = 1.0m, ["giảm giá"] = 1.0m,
            ["ưu đãi"] = 0.9m, ["deal"] = 0.9m, ["offer"] = 0.9m,
            ["membership"] = 0.8m, ["thành viên"] = 0.8m, ["member"] = 0.8m,
            ["loyalty"] = 0.8m, ["tích điểm"] = 0.8m, ["điểm"] = 0.7m,
            ["points"] = 0.7m, ["exchange"] = 0.8m, ["đổi"] = 0.8m,
            ["redeem"] = 0.8m, ["sử dụng"] = 0.7m, ["dùng"] = 0.7m,
            ["free"] = 0.7m, ["miễn phí"] = 0.7m, ["tặng"] = 0.7m,
            ["code"] = 0.6m, ["mã"] = 0.6m,
        };

        // Support intent signals
        var supportKeywords = new Dictionary<string, decimal>
        {
            ["booking"] = 1.0m, ["đặt vé"] = 1.0m, ["dat ve"] = 1.0m,
            ["reservation"] = 0.9m, ["đặt chỗ"] = 0.9m,
            ["hủy"] = 0.9m, ["cancel"] = 0.9m, ["refund"] = 0.9m,
            ["hoàn tiền"] = 0.9m, ["trả lại"] = 0.8m, ["return"] = 0.8m,
            ["payment"] = 0.8m, ["thanh toán"] = 0.8m, ["pay"] = 0.8m,
            ["where"] = 0.8m, ["ở đâu"] = 0.8m, ["location"] = 0.8m,
            ["address"] = 0.8m, ["địa chỉ"] = 0.8m, ["map"] = 0.7m,
            ["direction"] = 0.7m, ["đường"] = 0.6m,
            ["policy"] = 0.8m, ["chính sách"] = 0.8m, ["rule"] = 0.7m,
            ["quy định"] = 0.7m, ["help"] = 0.7m, ["giúp"] = 0.7m,
            ["hotline"] = 0.8m, ["contact"] = 0.7m, ["liên hệ"] = 0.7m,
            ["support"] = 0.7m, ["hỗ trợ"] = 0.7m,
            ["giờ"] = 0.6m, ["hour"] = 0.6m, ["open"] = 0.6m, ["mở cửa"] = 0.6m,
            ["seat"] = 0.7m, ["ghế"] = 0.7m, ["room"] = 0.6m, ["phòng"] = 0.6m,
            ["ticket"] = 0.7m, ["vé"] = 0.7m,
        };

        // Extract genre from message
        var genreMap = new Dictionary<string, string>
        {
            ["hành động"] = "Hành động", ["action"] = "Hành động",
            ["tình cảm"] = "Tình cảm", ["romance"] = "Tình cảm", ["romantic"] = "Tình cảm",
            ["hài"] = "Hài hước", ["comedy"] = "Hài hước", ["hài hước"] = "Hài hước",
            ["kinh dị"] = "Kinh dị", ["horror"] = "Kinh dị",
            ["viễn tưởng"] = "Viễn tưởng", ["sci-fi"] = "Viễn tưởng", ["science fiction"] = "Viễn tưởng",
            ["hoạt hình"] = "Hoạt hình", ["animation"] = "Hoạt hình", ["animated"] = "Hoạt hình",
            ["phiêu lưu"] = "Phiêu lưu", ["adventure"] = "Phiêu lưu",
            ["hình sự"] = "Hình sự", ["crime"] = "Hình sự",
            ["chiến tranh"] = "Chiến tranh", ["war"] = "Chiến tranh",
            ["âm nhạc"] = "Nhạc kịch", ["music"] = "Nhạc kịch", ["musical"] = "Nhạc kịch",
            ["tâm lý"] = "Tâm lý", ["drama"] = "Tâm lý",
            ["hình sự"] = "Hình sự", ["thriller"] = "Ly kỳ",
            ["viễn tây"] = "Viễn tây", ["western"] = "Viễn tây",
        };

        foreach (var kvp in movieKeywords)
        {
            if (normalized.Contains(kvp.Key, StringComparison.Ordinal))
            {
                movieScore += kvp.Value;
                if (genreMap.TryGetValue(kvp.Key, out var genre))
                    extractedGenre = genre;
                extractedKeyword ??= kvp.Key;
            }
        }

        foreach (var kvp in fbKeywords)
        {
            if (normalized.Contains(kvp.Key, StringComparison.Ordinal))
            {
                fbScore += kvp.Value;
                extractedKeyword ??= kvp.Key;
            }
        }

        foreach (var kvp in promoKeywords)
        {
            if (normalized.Contains(kvp.Key, StringComparison.Ordinal))
            {
                promotionScore += kvp.Value;
                extractedKeyword ??= kvp.Key;
            }
        }

        foreach (var kvp in supportKeywords)
        {
            if (normalized.Contains(kvp.Key, StringComparison.Ordinal))
            {
                supportScore += kvp.Value;
                extractedKeyword ??= kvp.Key;
            }
        }

        // Greeting detection
        var greetingPatterns = new[] { "xin chào", "hello", "hi", "hey", "chào", "alo",
            "good morning", "good afternoon", "good evening", "chào buổi" };
        var isGreeting = greetingPatterns.Any(g => normalized.Contains(g, StringComparison.Ordinal));

        // Determine winner
        var scores = new Dictionary<string, decimal>
        {
            ["movie"] = movieScore,
            ["fb"] = fbScore,
            ["promotion"] = promotionScore,
            ["support"] = supportScore,
        };

        var maxScore = scores.Values.Max();
        var winner = scores.FirstOrDefault(kv => kv.Value == maxScore).Key;

        if (maxScore == 0m)
        {
            winner = "general";
        }

        // Normalize confidence: divide by max possible (use 3.0 as reasonable cap)
        var confidence = maxScore > 0m ? Math.Min(maxScore / 3.0m, 1.0m) : 0.5m;

        return Task.FromResult(new IntentResult
        {
            Intent = winner,
            Confidence = confidence,
            ExtractedGenre = extractedGenre,
            ExtractedKeyword = extractedKeyword,
        });
    }
}
