using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;
using CompanySearch.Domain.ValueObjects;

namespace CompanySearch.Application.Common.Services;

public sealed class WebsiteScoringService : IWebsiteScoringService
{
    public WebsiteAnalysis Create(Guid businessId, WebsiteCrawlSnapshot snapshot)
    {
        var score = 100;
        var issues = new List<WebsiteIssue>();

        CheckSeo(snapshot, issues, ref score);
        CheckPerformance(snapshot, issues, ref score);
        CheckUserExperience(snapshot, issues, ref score);
        CheckSecurity(snapshot, issues, ref score);
        CheckTechnical(snapshot, issues, ref score);

        score = Math.Clamp(score, 0, 100);

        var summary = issues.Count == 0
            ? "Ana sayfada önemli bir sorun tespit edilmedi."
            : $"{issues.Count} adet iyileştirilebilir sorun tespit edildi. En yüksek etki: " +
              $"{string.Join(", ", issues.OrderByDescending(issue => issue.Penalty).Take(3).Select(issue => issue.TitleTr ?? issue.Title))}.";

        return WebsiteAnalysis.Create(businessId, score, summary, snapshot, issues);
    }

    private static void CheckSeo(WebsiteCrawlSnapshot snapshot, List<WebsiteIssue> issues, ref int score)
    {
        if (string.IsNullOrWhiteSpace(snapshot.Title))
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Seo,
                IssueSeverity.Error,
                "seo_title_missing",
                "Missing page title",
                "The homepage does not expose a usable <title> tag.",
                "Add a concise title tag that names the business and primary service.",
                "Sayfa başlığı eksik",
                "Ana sayfada kullanılabilir bir <title> etiketi yok.",
                "İşletmeyi ve ana hizmeti anlatan kısa bir başlık etiketi ekleyin.",
                12);
        }

        if (string.IsNullOrWhiteSpace(snapshot.MetaDescription))
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Seo,
                IssueSeverity.Warning,
                "seo_meta_description_missing",
                "Missing meta description",
                "Search engines cannot show an optimized page summary because the meta description is absent.",
                "Add a unique meta description between 140 and 160 characters.",
                "Meta açıklama eksik",
                "Meta description olmadığı için arama sonuçlarında optimize bir özet gösterilemiyor.",
                "140–160 karakter aralığında benzersiz bir meta description ekleyin.",
                10);
        }

        if (snapshot.H1Tags.Count == 0)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Seo,
                IssueSeverity.Warning,
                "seo_h1_missing",
                "Missing H1",
                "The page does not contain a top-level heading.",
                "Add one descriptive H1 that matches the primary offer.",
                "H1 başlığı yok",
                "Sayfada üst düzey bir başlık (H1) bulunmuyor.",
                "Ana teklifle uyumlu, açıklayıcı tek bir H1 ekleyin.",
                10);
        }

        if (snapshot.HasDuplicateHeadings)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Seo,
                IssueSeverity.Warning,
                "seo_duplicate_headings",
                "Duplicate heading structure",
                "The page repeats headings, which weakens content hierarchy.",
                "Keep headings unique and in a clear semantic order.",
                "Yinelenen başlık yapısı",
                "Başlıklar tekrarlanıyor; bu durum içerik hiyerarşisini zayıflatır.",
                "Başlıkları benzersiz tutun ve anlamsal sırayı netleştirin.",
                6);
        }
    }

    private static void CheckPerformance(WebsiteCrawlSnapshot snapshot, List<WebsiteIssue> issues, ref int score)
    {
        if (snapshot.ResponseTimeMs > 4000)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Performance,
                IssueSeverity.Critical,
                "perf_slow_response",
                "Slow response time",
                "The homepage response took longer than four seconds.",
                "Reduce server latency, cache responses, and optimize third-party scripts.",
                "Yavaş yanıt süresi",
                "Ana sayfa yanıtı dört saniyeden uzun sürdü.",
                "Sunucu gecikmesini azaltın, önbellekleme kullanın ve üçüncü taraf betikleri optimize edin.",
                18,
                $"{snapshot.ResponseTimeMs}ms");
        }
        else if (snapshot.ResponseTimeMs > 2000)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Performance,
                IssueSeverity.Warning,
                "perf_moderate_response",
                "Moderate response time",
                "The homepage response time is above the recommended threshold.",
                "Aim for a sub-two-second response time.",
                "Orta düzeyde yavaş yanıt",
                "Ana sayfa yanıt süresi önerilen eşiğin üzerinde.",
                "İki saniyenin altında yanıt süresi hedefleyin.",
                10,
                $"{snapshot.ResponseTimeMs}ms");
        }

        if (snapshot.LargestImageBytes > 1_500_000)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Performance,
                IssueSeverity.Error,
                "perf_large_images",
                "Oversized hero images",
                "The page serves at least one very large image.",
                "Compress large images and prefer modern formats such as WebP or AVIF.",
                "Çok büyük görseller",
                "Sayfa çok büyük en az bir görsel sunuyor.",
                "Görselleri sıkıştırın; WebP veya AVIF gibi modern formatları tercih edin.",
                12,
                $"{snapshot.LargestImageBytes} bytes");
        }
        else if (snapshot.LargestImageBytes > 600_000)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Performance,
                IssueSeverity.Warning,
                "perf_heavy_images",
                "Heavy images",
                "The page has images that are likely slowing down render time.",
                "Compress images and lazy-load non-critical media.",
                "Ağır görseller",
                "Sayfadaki görseller oluşturma süresini yavaşlatıyor olabilir.",
                "Görselleri sıkıştırın; kritik olmayan medyada lazy-load kullanın.",
                6,
                $"{snapshot.LargestImageBytes} bytes");
        }

        if (snapshot.JavaScriptFileCount > 10)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Performance,
                IssueSeverity.Warning,
                "perf_too_many_scripts",
                "Too many JavaScript files",
                "The page loads a high number of JavaScript files.",
                "Bundle or defer non-critical scripts.",
                "Çok fazla JavaScript dosyası",
                "Sayfa yüksek sayıda JavaScript dosyası yüklüyor.",
                "Kritik olmayan betikleri paketleyin veya erteleyin.",
                5,
                snapshot.JavaScriptFileCount.ToString());
        }

        if (snapshot.StylesheetFileCount > 5)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Performance,
                IssueSeverity.Warning,
                "perf_too_many_stylesheets",
                "Too many CSS files",
                "The page loads many stylesheet files, which can block rendering.",
                "Bundle CSS and remove unused styles.",
                "Çok fazla CSS dosyası",
                "Sayfa çok sayıda stil dosyası yüklüyor; oluşturmayı geciktirebilir.",
                "CSS’i paketleyin ve kullanılmayan stilleri kaldırın.",
                4,
                snapshot.StylesheetFileCount.ToString());
        }
    }

    private static void CheckUserExperience(WebsiteCrawlSnapshot snapshot, List<WebsiteIssue> issues, ref int score)
    {
        if (!snapshot.HasViewportMeta)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.UserExperience,
                IssueSeverity.Error,
                "ux_mobile_viewport_missing",
                "Mobile viewport is missing",
                "The page does not advertise responsive viewport settings.",
                "Add a viewport meta tag and verify responsive layouts.",
                "Mobil viewport eksik",
                "Sayfa duyarlı (responsive) viewport ayarlarını bildirmiyor.",
                "Viewport meta etiketi ekleyin ve duyarlı yerleşimleri doğrulayın.",
                10);
        }

        if (snapshot.ImagesWithoutAltCount > 0)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.UserExperience,
                IssueSeverity.Warning,
                "ux_image_alt_missing",
                "Images without alt text",
                "Some images are missing alt attributes, which hurts accessibility and SEO.",
                "Add descriptive alt text for meaningful images.",
                "Alt metni olmayan görseller",
                "Bazı görsellerde alt özniteliği yok; erişilebilirlik ve SEO olumsuz etkilenir.",
                "Anlamlı görseller için açıklayıcı alt metin ekleyin.",
                5,
                $"{snapshot.ImagesWithoutAltCount}/{snapshot.ImageCount}");
        }

        if (snapshot.BrokenLinks.Count > 0)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.UserExperience,
                IssueSeverity.Error,
                "ux_broken_links",
                "Broken internal links",
                "The page links to destinations that returned an error.",
                "Fix or remove broken internal links.",
                "Kırık dahili bağlantılar",
                "Sayfa hata dönen hedeflere bağlantı veriyor.",
                "Kırık dahili bağlantıları düzeltin veya kaldırın.",
                Math.Min(15, 5 + snapshot.BrokenLinks.Count * 2),
                string.Join(", ", snapshot.BrokenLinks.Take(5)));
        }
    }

    private static void CheckSecurity(WebsiteCrawlSnapshot snapshot, List<WebsiteIssue> issues, ref int score)
    {
        if (!snapshot.UsesHttps)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Security,
                IssueSeverity.Critical,
                "security_https_missing",
                "HTTPS is not enforced",
                "The site is served without HTTPS.",
                "Enable HTTPS and redirect all traffic to the secure origin.",
                "HTTPS kullanılmıyor",
                "Site HTTPS olmadan sunuluyor.",
                "HTTPS’i etkinleştirin ve tüm trafiği güvenli kaynağa yönlendirin.",
                20);
        }

        if (snapshot.MissingSecurityHeaders.Count > 0)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Security,
                IssueSeverity.Warning,
                "security_headers_missing",
                "Security headers are missing",
                "The response is missing important hardening headers.",
                "Add headers such as Content-Security-Policy, X-Frame-Options, and Strict-Transport-Security.",
                "Güvenlik başlıkları eksik",
                "Yanıtta önemli sertleştirme başlıkları yok.",
                "Content-Security-Policy, X-Frame-Options ve Strict-Transport-Security gibi başlıkları ekleyin.",
                Math.Min(16, snapshot.MissingSecurityHeaders.Count * 4),
                string.Join(", ", snapshot.MissingSecurityHeaders));
        }
    }

    private static void CheckTechnical(WebsiteCrawlSnapshot snapshot, List<WebsiteIssue> issues, ref int score)
    {
        if (snapshot.StatusCode >= 400)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Technical,
                IssueSeverity.Critical,
                "tech_http_error",
                "Homepage returns an error response",
                "The homepage returned an unsuccessful HTTP response.",
                "Resolve the server-side error and return a stable 200-series response.",
                "Ana sayfa hata yanıtı veriyor",
                "Ana sayfa başarısız bir HTTP yanıtı döndürdü.",
                "Sunucu tarafı hatayı giderin ve kararlı 2xx yanıtı sağlayın.",
                30,
                snapshot.StatusCode.ToString());
        }

        if (snapshot.InternalLinks.Count == 0)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Technical,
                IssueSeverity.Warning,
                "tech_navigation_thin",
                "Thin internal navigation",
                "The crawler found no meaningful internal links on the homepage.",
                "Expose clear service and contact paths in the main navigation.",
                "Zayıf dahili gezinme",
                "Tarayıcı ana sayfada anlamlı dahili bağlantı bulamadı.",
                "Ana menüde hizmet ve iletişim yollarını net şekilde gösterin.",
                5);
        }

        if (snapshot.HasConsoleErrorHints)
        {
            AddIssue(
                issues,
                ref score,
                AnalysisCategory.Technical,
                IssueSeverity.Warning,
                "tech_console_error_hints",
                "Potential console errors detected",
                "The page source suggests unresolved console error logging or failing scripts.",
                "Audit the browser console and resolve failing scripts.",
                "Olası konsol hataları",
                "Sayfa kaynağı çözülmemiş konsol hataları veya başarısız betiklere işaret ediyor.",
                "Tarayıcı konsolunu denetleyin ve hatalı betikleri giderin.",
                6);
        }
    }

    private static void AddIssue(
        List<WebsiteIssue> issues,
        ref int score,
        AnalysisCategory category,
        IssueSeverity severity,
        string code,
        string title,
        string description,
        string recommendation,
        string titleTr,
        string descriptionTr,
        string recommendationTr,
        int penalty,
        string? evidence = null)
    {
        issues.Add(new WebsiteIssue(
            category,
            severity,
            code,
            title,
            description,
            recommendation,
            penalty,
            evidence,
            titleTr,
            descriptionTr,
            recommendationTr));
        score -= penalty;
    }
}
