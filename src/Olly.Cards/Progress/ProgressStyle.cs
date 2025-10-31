using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace Olly.Cards.Progress;

[JsonConverter(typeof(JsonConverter<ProgressStyle>))]
public class ProgressStyle(string value) : StringEnum(value)
{
    public static readonly ProgressStyle InProgress = new("in-progress");
    public bool IsInProgress => InProgress.Equals(Value);

    public static readonly ProgressStyle Success = new("success");
    public bool IsSuccess => Success.Equals(Value);

    public static readonly ProgressStyle Warning = new("warning");
    public bool IsWarning => Warning.Equals(Value);

    public static readonly ProgressStyle Error = new("error");
    public bool IsError => Error.Equals(Value);

    public string Icon => Value switch
    {
        "in-progress" => "Info",
        "success" => "CheckmarkCircle",
        "warning" => "Warning",
        "error" => "ErrorCircle",
        _ => throw new InvalidDataException($"{Value} is not a valid ProgressStyle")
    };

    public TextColor Color => Value switch
    {
        "in-progress" => TextColor.Accent,
        "success" => TextColor.Good,
        "warning" => TextColor.Warning,
        "error" => TextColor.Attention,
        _ => throw new InvalidDataException($"{Value} is not a valid ProgressStyle")
    };

    public ContainerStyle ContainerStyle => Value switch
    {
        "in-progress" => ContainerStyle.Accent,
        "success" => ContainerStyle.Good,
        "warning" => ContainerStyle.Warning,
        "error" => ContainerStyle.Attention,
        _ => throw new InvalidDataException($"{Value} is not a valid ProgressStyle")
    };

    public string Message => Value switch
    {
        "in-progress" => "Please Wait...",
        "success" => "Success!",
        "warning" => "Warning",
        "error" => "Error!",
        _ => throw new InvalidDataException($"{Value} is not a valid ProgressStyle")
    };

    public void Validate()
    {
        if (!IsError && !IsWarning && !IsSuccess && !IsInProgress)
        {
            throw new InvalidDataException("invalid style, supported values are 'in-progress', 'success', 'warning', 'error'");
        }
    }
}