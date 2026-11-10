using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Core.MappingProfiles;
using LetsTalk.Server.Authentication.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace LetsTalk.Server.API.Core.Tests.MappingProfiles;

[TestFixture]
public class AuthenticationProfileTests
{
    private static readonly DateTime AccessExpiresAt = new(2030, 1, 1, 0, 15, 0, DateTimeKind.Utc);
    private static readonly DateTime RefreshExpiresAt = new(2030, 1, 31, 0, 0, 0, DateTimeKind.Utc);

    private IMapper _mapper;

    [SetUp]
    public void SetUp()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<AuthenticationProfile>(),
            NullLoggerFactory.Instance);
        _mapper = configuration.CreateMapper();
    }

    [Test]
    public void Map_WhenSessionHasBothTokens_ShouldCopyEveryField()
    {
        var session = new AuthSession("access", AccessExpiresAt, "refresh", RefreshExpiresAt);

        var result = _mapper.Map<SessionResult>(session);

        result.Should().Be(new SessionResult("access", AccessExpiresAt, "refresh", RefreshExpiresAt));
    }

    [Test]
    public void Map_WhenSessionHasNoRefreshToken_ShouldKeepTheRefreshFieldsEmpty()
    {
        var session = new AuthSession("access", AccessExpiresAt, null, null);

        var result = _mapper.Map<SessionResult>(session);

        result.Should().Be(new SessionResult("access", AccessExpiresAt, null, null));
    }
}
