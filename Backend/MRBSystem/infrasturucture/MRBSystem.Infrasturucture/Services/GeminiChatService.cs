using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MRBSystem.Contracts.DTOs.ChatDtos;
using MRBSystem.Contracts.DTOs.RoomDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Infrasturucture.AI.Gemini;
using System.Text;
using System.Text.Json;

namespace MRBSystem.Infrasturucture.Services;

public class GeminiChatService : IChatService
{
    private const string Model = "gemini-3.5-flash-lite";
    private const int MaxToolRounds = 3;

    private const string SystemPrompt =
        "Sən MeetingSpace görüş otağı rezervasiya sisteminin köməkçi botusan. " +
        "İstifadəçilərə hansı otaqların hansı vaxt boş olduğunu tapmaqda kömək edirsən. " +
        "Yalnız 'search_available_rooms' alətindən istifadə et — rezervasiya yaratma, silmə və ya başqa əməliyyat etmə, " +
        "yalnız məlumat ver. Cavablarını həmişə Azərbaycan dilində, qısa və aydın şəkildə ver. " +
        "Əgər bina adı tapılmasa, alət sənə mövcud bina adlarının siyahısını qaytaracaq — " +
        "həmin adlardan istifadəçinin nəzərdə tutduğuna ən yaxın olanı seçib yenidən cəhd et. " +
        "İstifadəçi tarix/saat vermirsə, aydınlaşdırıcı sual ver. " +
        "Cavablarında Markdown ulduzlarından (**) və ya başqa formatlaşdırma simvollarından istifadə etmə. " +
        "Emoji, stiker və ya dekorativ simvollar işlətmə. " +
        "Bir neçə otaq/nəticə olduqda məlumatları sadə, hər biri ayrı sətirdə olmaqla göstər — ulduz, tire və ya markdown işarəsi olmadan, sadəcə yeni sətirlə. " +
        "Tək bir nəticə olduqda süni siyahı yaratma, adi cümlə şəklində yaz.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IRoomService _roomService;
    private readonly IBuildingService _buildingService;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly ITimeZoneService _timeZoneService;

    public GeminiChatService(
        HttpClient httpClient,
        IConfiguration configuration,
        IRoomService roomService,
        IBuildingService buildingService,
        ILogger<GeminiChatService> logger,
        ITimeZoneService timeZoneService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _roomService = roomService;
        _buildingService = buildingService;
        _logger = logger;
        _timeZoneService = timeZoneService;
    }

    public async Task<ChatResponseDto> SendMessageAsync(ChatRequestDto dto)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API açarı konfiqurasiya olunmayıb.");
        }

        var contents = dto.History
            .Select(h => new GeminiContent { Role = h.Role, Parts = new List<GeminiPart> { new() { Text = h.Text } } })
            .ToList();

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart> { new() { Text = dto.Message } },
        });

        var today = _timeZoneService.ToLocal(DateTime.UtcNow).Date;
        var systemInstruction =
            $"{SystemPrompt} Bugünkü həqiqi tarix: {today:yyyy-MM-dd} ({today.DayOfWeek}). " +
            "İstifadəçi 'bugün', 'sabah', 'srağagün' kimi nisbi tarix ifadələri işlətsə, bu tarixə əsaslanaraq hesabla.";

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var response = await CallGeminiAsync(contents, apiKey, systemInstruction);
            var candidate = response.Candidates?.FirstOrDefault();
            var parts = candidate?.Content?.Parts ?? new List<GeminiPart>();

            var functionCallPart = parts.FirstOrDefault(p => p.FunctionCall != null);

            if (functionCallPart == null)
            {
                contents.Add(new GeminiContent { Role = "model", Parts = parts });
                var text = string.Join("\n", parts.Where(p => !string.IsNullOrEmpty(p.Text)).Select(p => p.Text));

                return new ChatResponseDto
                {
                    Reply = string.IsNullOrWhiteSpace(text)
                        ? "Üzr istəyirəm, cavab hazırlaya bilmədim. Zəhmət olmasa sualı yenidən yazın."
                        : text,
                    History = BuildHistoryDto(contents),
                };
            }

            contents.Add(new GeminiContent { Role = "model", Parts = parts });

            var toolResult = await SearchRoomsAsync(functionCallPart.FunctionCall!.Args);

            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new()
                    {
                        FunctionResponse = new { name = functionCallPart.FunctionCall.Name, response = new { result = toolResult } },
                    },
                },
            });
        }

        _logger.LogWarning("Gemini {MaxRounds} dövr ərzində son cavab vermədi.", MaxToolRounds);
        return new ChatResponseDto
        {
            Reply = "Üzr istəyirəm, sorğunuzu emal edərkən problem yarandı. Zəhmət olmasa sualı fərqli şəkildə yenidən yazın.",
            History = dto.History,
        };
    }

    private static List<ChatMessageDto> BuildHistoryDto(List<GeminiContent> contents)
    {
        return contents
            .Select(c => new ChatMessageDto
            {
                Role = c.Role ?? "user",
                Text = string.Join("\n", c.Parts.Where(p => !string.IsNullOrEmpty(p.Text)).Select(p => p.Text)),
            })
            .Where(m => !string.IsNullOrWhiteSpace(m.Text))
            .ToList();
    }

    private async Task<GeminiResponse> CallGeminiAsync(List<GeminiContent> contents, string apiKey, string systemInstructionText)
    {
        var requestBody = new
        {
            systemInstruction = new { parts = new[] { new { text = systemInstructionText } } },
            contents,
            tools = new[]
            {
                new
                {
                    functionDeclarations = new[]
                    {
                        new
                        {
                            name = "search_available_rooms",
                            description = "Müəyyən tarix və saat aralığında, seçilmiş binada boş (mövcud) görüş otaqlarını axtarır.",
                            parameters = BuildSearchRoomsSchema(),
                        },
                    },
                },
            },
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={apiKey}";
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException("Chatbot xidməti hazırda cavab vermir. Bir az sonra yenidən cəhd edin.");
        }

        return JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("Chatbot xidmətindən boş cavab alındı.");
    }

    private async Task<object> SearchRoomsAsync(JsonElement args)
    {
        var buildingName = TryGetString(args, "buildingName");
        var date = TryGetString(args, "date");
        var startTime = TryGetString(args, "startTime");
        var endTime = TryGetString(args, "endTime");
        var minCapacity = TryGetInt(args, "minCapacity") ?? 1;

        if (string.IsNullOrWhiteSpace(buildingName) || string.IsNullOrWhiteSpace(date)
            || string.IsNullOrWhiteSpace(startTime) || string.IsNullOrWhiteSpace(endTime))
        {
            return new { error = "buildingName, date, startTime və endTime tələb olunur." };
        }

        var buildings = await _buildingService.GetAllAsync();
        var building = buildings.FirstOrDefault(b => b.Name.Equals(buildingName, StringComparison.OrdinalIgnoreCase))
            ?? buildings.FirstOrDefault(b => b.Name.Contains(buildingName, StringComparison.OrdinalIgnoreCase));

        if (building == null)
        {
            return new
            {
                error = $"'{buildingName}' adlı bina tapılmadı.",
                availableBuildings = buildings.Select(b => b.Name).ToList(),
            };
        }

        if (!DateTime.TryParse($"{date}T{startTime}", out var localStart)
            || !DateTime.TryParse($"{date}T{endTime}", out var localEnd))
        {
            return new { error = "Tarix/saat formatı düzgün deyil. Tarix YYYY-MM-DD, saat HH:mm formatında olmalıdır." };
        }
        var startDate = _timeZoneService.ToUtc(localStart);
        var endDate = _timeZoneService.ToUtc(localEnd);

        try
        {
            var results = await _roomService.SearchAvailableAsync(new RoomAvailabilityRequestDto
            {
                StartDate = startDate,
                EndDate = endDate,
                BuildingId = building.Id,
                Capacity = minCapacity,
            });

            if (results.Count == 0)
            {
                return new { message = "Bu vaxt aralığında uyğun boş otaq tapılmadı." };
            }

            return new
            {
                rooms = results.Select(r => new
                {
                    r.RoomName,
                    r.BuildingName,
                    r.FloorName,
                    r.Capacity,
                    r.Equipment,
                }),
            };
        }
        catch (ArgumentException ex)
        {
            return new { error = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Otaq axtarışı zamanı xəta baş verdi.");
            return new { error = "Otaq axtarışı zamanı xəta baş verdi." };
        }
    }

    private static string? TryGetString(JsonElement args, string propertyName)
        => args.ValueKind == JsonValueKind.Object && args.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? TryGetInt(JsonElement args, string propertyName)
        => args.ValueKind == JsonValueKind.Object && args.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static object BuildSearchRoomsSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                buildingName = new { type = "string", description = "Binanın adı (məs. 'Baş bina', 'DIV Academy')." },
                date = new { type = "string", description = "Tarix, YYYY-MM-DD formatında (məs. '2026-09-05')." },
                startTime = new { type = "string", description = "Başlama saatı, HH:mm formatında (məs. '14:00')." },
                endTime = new { type = "string", description = "Bitmə saatı, HH:mm formatında (məs. '15:00')." },
                minCapacity = new { type = "integer", description = "Minimum tutum (opsional, verilməzsə 1 qəbul edilir)." },
            },
            required = new[] { "buildingName", "date", "startTime", "endTime" },
        };
    }
}