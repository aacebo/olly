using System.Net;
using System.Text.Json.Serialization;

namespace OS.Agent.Errors;

public class HttpException : Exception
{
    [JsonPropertyName("code")]
    public HttpStatusCode Code { get; init; }

    [JsonPropertyName("message")]
    public override string Message { get; }

    [JsonPropertyName("errors")]
    public IList<Exception> Errors { get; } = [];

    public HttpException(string message, params Exception[] errors) : base(message)
    {
        Code = HttpStatusCode.InternalServerError;
        Message = message;
        Errors = errors;
    }

    public HttpException(HttpStatusCode code, params Exception[] errors) : base(code.ToNameString())
    {
        Message = code.ToNameString();
        Errors = errors;
    }

    public HttpException(string message, HttpStatusCode code, params Exception[] errors) : base(message)
    {
        Code = code;
        Message = message;
        Errors = errors;
    }

    public HttpException AddMessage(string message)
    {
        return new HttpException(message, Code, Errors.ToArray());
    }

    public HttpException AddError(Exception error)
    {
        Errors.Add(error);
        return this;
    }

    public static HttpException UnAuthorized(params Exception[] errors) => new(HttpStatusCode.Unauthorized, errors);
    public static HttpException NotFound(params Exception[] errors) => new(HttpStatusCode.NotFound, errors);
    public static HttpException Conflict(params Exception[] errors) => new(HttpStatusCode.Conflict, errors);
    public static HttpException BadRequest(params Exception[] errors) => new(HttpStatusCode.BadRequest, errors);
}

public static class HttpStatusCodeExtensions
{
    public static string ToNameString(this HttpStatusCode code)
    {
        return code switch
        {
            HttpStatusCode.Unauthorized => "UnAuthorized",
            HttpStatusCode.NotFound => "NotFound",
            HttpStatusCode.OK => "Ok",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.BadRequest => "BadRequest",
            HttpStatusCode.Conflict => "Conflict",
            HttpStatusCode.Created => "Created",
            HttpStatusCode.InternalServerError => "InternalServerError",
            _ => code.ToString()
        };
    }
}