using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Twiggle.Api;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static async Task Reject(Func<Task> action)
{
    try { await action(); } catch (AiProviderException) { return; }
    throw new Exception("Expected provider failure");
}
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AI:ApiKey"] = "test-not-a-real-key", ["AI:Model"] = "test-model" }).Build();
var turnId = Guid.NewGuid();
var draft = new InterviewDraft("You implemented a check.", "What happened on failure?", "Clarify behavior.", [new AiFact(turnId, "added a check", "I added a check.")]);
var handler = new RecordedHandler(JsonSerializer.Serialize(new { status = "completed", output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = JsonSerializer.Serialize(draft) } } } } }));
var ai = new AiInterviewer(new HttpClient(handler), config);
var result = await ai.GenerateAsync(new { answers = new[] { "I added a check." } }, CancellationToken.None);
Check(result.NextQuestion == draft.NextQuestion && result.Facts[0].SourceTurnId == turnId, "Structured output parsing failed");
Check(handler.RequestUri == "https://api.openai.com/v1/responses", "Unexpected destination");
Check(handler.Authorization == "Bearer test-not-a-real-key", "Server authorization missing");
using (var request = JsonDocument.Parse(handler.RequestBody!))
{
    Check(!request.RootElement.GetProperty("store").GetBoolean(), "Provider response storage must be disabled");
    Check(request.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean(), "Strict schema missing");
    Check(!request.RootElement.GetProperty("input").GetString()!.Contains("test-not-a-real-key"), "Key leaked into context");
}
var session = new InterviewSession { Turns = [new InterviewTurn { Id = turnId, Text = "I added a check." }] };
Check(AiInterviewEndpoints.HasValidSources(result, session), "Valid source rejected");
Check(!AiInterviewEndpoints.HasValidSources(draft with { Facts = [new AiFact(Guid.NewGuid(), "added a check", "Claim")] }, session), "Invented source accepted");
Check(!AiInterviewEndpoints.HasValidSources(draft with { Facts = [new AiFact(turnId, "designed the architecture", "Claim")] }, session), "Invented quote accepted");
Check(!AiInterviewEndpoints.HasValidSources(draft with { Facts = [new AiFact(turnId, "", "Claim")] }, session), "Empty quote accepted");
var missing = new AiInterviewer(new HttpClient(new RecordedHandler("{}")), new ConfigurationBuilder().Build());
Check(!missing.Configured, "Missing key reported ready");
await Reject(() => missing.GenerateAsync(new { }, CancellationToken.None));
await Reject(() => new AiInterviewer(new HttpClient(new RecordedHandler("{}", HttpStatusCode.Unauthorized)), config).GenerateAsync(new { }, CancellationToken.None));
await Reject(() => new AiInterviewer(new HttpClient(new RecordedHandler("{}", HttpStatusCode.TooManyRequests)), config).GenerateAsync(new { }, CancellationToken.None));
await Reject(() => new AiInterviewer(new HttpClient(new RecordedHandler("{\"status\":\"incomplete\"}")), config).GenerateAsync(new { }, CancellationToken.None));
Console.WriteLine("PASS: provider request contract, parsing, key isolation, invalid citations, missing configuration, auth/quota and incomplete response handling. Recorded fixtures only; no live API call.");
sealed class RecordedHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    public string? RequestUri { get; private set; }
    public string? RequestBody { get; private set; }
    public string? Authorization { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        RequestUri = request.RequestUri?.ToString(); Authorization = request.Headers.Authorization?.ToString();
        RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
