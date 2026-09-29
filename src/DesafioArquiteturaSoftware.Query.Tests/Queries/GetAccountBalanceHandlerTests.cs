using DesafioArquiteturaSoftware.Query.Models;
using DesafioArquiteturaSoftware.Query.Queries.GetAccountBalance;
using DesafioArquiteturaSoftware.Query.Repositories;
using Moq;

namespace DesafioArquiteturaSoftware.Query.Tests.Queries;

public class GetAccountBalanceHandlerTests
{
    private readonly Mock<IAccountBalanceRepository>
        _repository;

    private readonly GetAccountBalanceHandler _handler;

    public GetAccountBalanceHandlerTests()
    {
        _repository = new Mock<IAccountBalanceRepository>();

        _handler = new GetAccountBalanceHandler(
            _repository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBalance()
    {
        var accountId = Guid.NewGuid();
        var at = DateTime.UtcNow;

        var expected = new AccountBalance
        {
            AccountId = accountId,
            Balance = 1500,
            AsOf = at
        };

        _repository
            .Setup(x => x.GetBalanceAsync(
                accountId,
                at,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var query = new GetAccountBalanceQuery(
            accountId,
            at);

        var result = await _handler.HandleAsync(
            query,
            CancellationToken.None);

        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(1500, result.Balance);
        Assert.Equal(at, result.AsOf);

        _repository.Verify(
            x => x.GetBalanceAsync(
                accountId,
                at,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseCurrentTime_WhenAtIsNotProvided()
    {
        var accountId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetBalanceAsync(
                accountId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Guid id,
                DateTime date,
                CancellationToken _) =>
                    new AccountBalance
                    {
                        AccountId = id,
                        Balance = 100,
                        AsOf = date
                    });

        var query = new GetAccountBalanceQuery(
            accountId,
            null);

        var result = await _handler.HandleAsync(
            query,
            CancellationToken.None);

        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(100, result.Balance);

        _repository.Verify(
            x => x.GetBalanceAsync(
                accountId,
                It.Is<DateTime>(
                    value => value > DateTime.UtcNow.AddSeconds(-5)
                        && value <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenAccountIdIsEmpty()
    {
        var query = new GetAccountBalanceQuery(
            Guid.Empty,
            DateTime.UtcNow);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.HandleAsync(
                    query,
                    CancellationToken.None));

        Assert.Equal(
            "AccountId is required.",
            exception.Message);

        _repository.Verify(
            x => x.GetBalanceAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}