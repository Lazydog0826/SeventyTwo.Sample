namespace SeventyTwo.Sample.Application.Users;

public interface IUserApplication { }

public sealed record LoginOutput(string AccessToken, string RefreshToken);
