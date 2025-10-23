using System.Text.Json;

using Microsoft.Teams.Api;

namespace OS.Agent.Cards;

public static class Progress
{
    public static Attachment InProgress(string title, string? message = null)
    {
        return new Attachment()
        {
            ContentType = ContentType.AdaptiveCard,
            Content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            $$$"""
            {
                "type": "AdaptiveCard",
                "version": "1.5",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "{{{title}}}",
                        "wrap": true,
                        "size": "Large",
                        "weight": "Bolder"
                    },
                    {
                        "type": "ProgressBar"
                    },
                    {
                        "type": "TextBlock",
                        "text": "{{{message ?? "loading..."}}}",
                        "spacing": "ExtraSmall",
                        "size": "Small"
                    }
                ]
            }
            """) ?? throw new JsonException()
        };
    }

    public static Attachment Success(string title, string? message = null)
    {
        return new Attachment()
        {
            ContentType = ContentType.AdaptiveCard,
            Content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            $$$"""
            {
                "type": "AdaptiveCard",
                "version": "1.5",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "{{{title}}}",
                        "wrap": true,
                        "size": "Large",
                        "weight": "Bolder"
                    },
                    {
                        "type": "ProgressBar",
                        "value": 100,
                        "color": "Good"
                    },
                    {
                        "type": "ColumnSet",
                        "columns": [
                            {
                                "type": "Column",
                                "width": "auto",
                                "items": [
                                    {
                                        "type": "Icon",
                                        "name": "CheckmarkCircle",
                                        "size": "xxSmall",
                                        "color": "Good"
                                    }
                                ],
                                "verticalContentAlignment": "Center"
                            },
                            {
                                "type": "Column",
                                "width": "stretch",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "text": "{{{message ?? "success!"}}}",
                                        "spacing": "ExtraSmall",
                                        "size": "Small",
                                        "color": "Good"
                                    }
                                ],
                                "verticalContentAlignment": "Center",
                                "spacing": "Small"
                            }
                        ],
                        "spacing": "ExtraSmall"
                    }
                ]
            }
            """) ?? throw new JsonException()
        };
    }

    public static Attachment Error(string title, string? message = null)
    {
        return new Attachment()
        {
            ContentType = ContentType.AdaptiveCard,
            Content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            $$$"""
            {
                "type": "AdaptiveCard",
                "version": "1.5",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "{{{title}}}",
                        "wrap": true,
                        "size": "Large",
                        "weight": "Bolder"
                    },
                    {
                        "type": "ProgressBar",
                        "value": 100,
                        "color": "Attention"
                    },
                    {
                        "type": "ColumnSet",
                        "columns": [
                            {
                                "type": "Column",
                                "width": "auto",
                                "items": [
                                    {
                                        "type": "Icon",
                                        "name": "ErrorCircle",
                                        "size": "xxSmall",
                                        "color": "Attention"
                                    }
                                ],
                                "verticalContentAlignment": "Center"
                            },
                            {
                                "type": "Column",
                                "width": "stretch",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "text": "{{{message ?? "error!"}}}",
                                        "spacing": "ExtraSmall",
                                        "size": "Small",
                                        "color": "Attention"
                                    }
                                ],
                                "verticalContentAlignment": "Center",
                                "spacing": "Small"
                            }
                        ],
                        "spacing": "ExtraSmall"
                    }
                ]
            }
            """) ?? throw new JsonException()
        };
    }

    public static Attachment Warning(string title, string? message = null)
    {
        return new Attachment()
        {
            ContentType = ContentType.AdaptiveCard,
            Content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            $$$"""
            {
                "type": "AdaptiveCard",
                "version": "1.5",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "{{{title}}}",
                        "wrap": true,
                        "size": "Large",
                        "weight": "Bolder"
                    },
                    {
                        "type": "ProgressBar",
                        "value": 100,
                        "color": "Warning"
                    },
                    {
                        "type": "ColumnSet",
                        "columns": [
                            {
                                "type": "Column",
                                "width": "auto",
                                "items": [
                                    {
                                        "type": "Icon",
                                        "name": "Warning",
                                        "size": "xxSmall",
                                        "color": "Warning"
                                    }
                                ],
                                "verticalContentAlignment": "Center"
                            },
                            {
                                "type": "Column",
                                "width": "stretch",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "text": "{{{message ?? "error!"}}}",
                                        "spacing": "ExtraSmall",
                                        "size": "Small",
                                        "color": "Warning"
                                    }
                                ],
                                "verticalContentAlignment": "Center",
                                "spacing": "Small"
                            }
                        ],
                        "spacing": "ExtraSmall"
                    }
                ]
            }
            """) ?? throw new JsonException()
        };
    }
}