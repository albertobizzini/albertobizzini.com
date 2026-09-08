namespace KindleClippings.ConsoleApp.Ai;

public sealed record AiClipping(
    string Id,
    string Title,
    string? Author,
    string Text,
    string Language);
