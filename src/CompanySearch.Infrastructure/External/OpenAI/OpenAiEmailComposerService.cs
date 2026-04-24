using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CompanySearch.Application.Abstractions.AI;
using CompanySearch.Application.Common.Models;
using CompanySearch.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompanySearch.Infrastructure.External.OpenAI;

public sealed class OpenAiEmailComposerService(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiEmailComposerService> logger)
    : IEmailComposerService
{
    public const string HttpClientName = "openai-email-composer";

    private readonly OpenAiOptions _options = options.Value;

    public async Task<GeneratedEmailContent> GenerateAsync(EmailGenerationPrompt prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("OpenAI API key is missing. Falling back to deterministic email generation.");
            return BuildFallback(prompt);
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var payload = new
            {
                model = _options.Model,
                max_output_tokens = _options.MaxOutputTokens,
                input = new object[]
                {
                    new
                    {
                        role = "system",
                        content = new[]
                        {
                            new
                            {
                                type = "input_text",
                                text = "Sen deneyimli bir B2B satış temsilcisisin. Kisa, samimi, profesyonel ve spam olmayan soguk e-postalar yaz. Cikti yalnizca verilen JSON semasina uymali. Acik teklifler kullan, gereksiz abartidan kacın ve en fazla 180-220 kelime kullan."
                            }
                        }
                    },
                    new
                    {
                        role = "user",
                        content = new[]
                        {
                            new
                            {
                                type = "input_text",
                                text = BuildPrompt(prompt)
                            }
                        }
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "sales_email",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                subject = new
                                {
                                    type = "string",
                                    description = "Concise Turkish email subject."
                                },
                                body = new
                                {
                                    type = "string",
                                    description = "Short Turkish sales email body with greeting, observed issues, value proposition, and CTA."
                                }
                            },
                            required = new[] { "subject", "body" }
                        }
                    }
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenAI response failed with status {StatusCode}: {Body}", response.StatusCode, content);
                return BuildFallback(prompt);
            }

            var outputJson = ExtractOutputText(content);
            if (string.IsNullOrWhiteSpace(outputJson))
            {
                logger.LogWarning("OpenAI response did not contain structured output. Falling back.");
                return BuildFallback(prompt);
            }

            var email = JsonSerializer.Deserialize<StructuredEmailResponse>(outputJson, JsonSerializerOptions.Web);
            if (email is null || string.IsNullOrWhiteSpace(email.Subject) || string.IsNullOrWhiteSpace(email.Body))
            {
                return BuildFallback(prompt);
            }

            return new GeneratedEmailContent(email.Subject.Trim(), email.Body.Trim(), _options.Model);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "OpenAI email generation failed. Falling back to deterministic output.");
            return BuildFallback(prompt);
        }
    }

    private static string BuildPrompt(EmailGenerationPrompt prompt)
    {
        var issues = prompt.Issues.Count == 0
            ? "Belirgin bulgu yok."
            : string.Join(Environment.NewLine, prompt.Issues
                .OrderByDescending(issue => issue.Penalty)
                .Take(5)
                .Select(issue =>
                {
                    var t = issue.TitleTr ?? issue.Title;
                    var d = issue.DescriptionTr ?? issue.Description;
                    var r = issue.RecommendationTr ?? issue.Recommendation;
                    return $"- [{issue.Category}] {t}: {d}. Öneri: {r}";
                }));

        return $"""
Isletme adi: {prompt.BusinessName}
Web sitesi: {prompt.Website ?? "Yok"}
Skor: {prompt.Score}
Ozet: {prompt.Summary}
Bilinen iletisim email'i: {prompt.RecipientEmail ?? "Yok"}

Tespit edilen bulgular:
{issues}

Beklenti:
- Turkce yaz
- 1 kisa konu satiri uret
- Govdede 4 kisa paragrafi gecme
- 2 veya 3 somut bulguya degin
- Yardim teklif et
- Baskici olmayan bir CTA ile bitir
""";
    }

    private static string? ExtractOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (root.TryGetProperty("output", out var outputArray) &&
            outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputArray.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                    {
                        return textElement.GetString();
                    }
                }
            }
        }

        return null;
    }

    private GeneratedEmailContent BuildFallback(EmailGenerationPrompt prompt)
    {
        var topIssues = prompt.Issues
            .OrderByDescending(issue => issue.Penalty)
            .Take(3)
            .Select(issue => issue.TitleTr ?? issue.Title)
            .ToArray();

        var issueSentence = topIssues.Length == 0
            ? "dijital tarafta hizli kazanilabilecek birkac iyilestirme alani"
            : string.Join(", ", topIssues);

        var subject = $"{prompt.BusinessName} sitesi icin hizli bir iyilestirme fikri";
        var body = $"""
Merhaba,

{prompt.BusinessName} icin web varligini inceleyince ozellikle {issueSentence} tarafinda gelistirme firsati gordum. Bu tip dokunuslar genelde hem ilk izlenimi guclendiriyor hem de reklam trafiginden gelen donusumu yukari cekiyor.

Isterseniz size 15 dakikada uygulanabilir, onceliklendirilmis bir mini aksiyon listesi hazirlayabilirim. Ekibimiz benzer sirketlerde SEO, hiz ve mobil deneyim kaynakli kayiplari azaltmaya odaklaniyor.

Uygun olursaniz bu hafta kisa bir gorusme ayarlayip bulgulari paylasayim.

Sevgiler,
CompanySearch
""";

        return new GeneratedEmailContent(subject, body, "fallback-template");
    }

    private sealed record StructuredEmailResponse(string Subject, string Body);
}
