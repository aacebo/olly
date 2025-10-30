using Json.Schema;

using Microsoft.Teams.AI.Models.OpenAI;

namespace OS.Agent.Prompts.Extensions;

public static class OpenAIChatPromptExtensions
{
    public static OpenAIChatPrompt AddPrompt(this OpenAIChatPrompt prompt, OpenAIChatPrompt other, CancellationToken cancellationToken = default)
    {
        prompt.Function(
            other.Name,
            other.Description,
            new JsonSchemaBuilder().Type(SchemaValueType.Object).Properties(
                ("message", new JsonSchemaBuilder().Type(SchemaValueType.String).Description("message to send"))
            ).Required("message"),
            async (string message) =>
            {
                var res = await other.Send(message, new()
                {
                    Request = new()
                    {
                        Temperature = 0
                    }
                }, null, cancellationToken);

                return res.Content;
            }
        );

        return prompt;
    }
}