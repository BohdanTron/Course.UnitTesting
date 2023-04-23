using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Users.Api.Logging;
using Users.Api.Models;
using Users.Api.Repositories;
using Users.Api.Services;
using Xunit;

namespace Users.Api.Tests.Unit.Application;

public class UserServiceTests
{
    private readonly UserService _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILoggerAdapter<UserService> _logger = Substitute.For<ILoggerAdapter<UserService>>();

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository, _logger);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        _userRepository.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsers_WhenSomeUsersExist()
    {
        // Arrange
        var nickChapsas = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nick Chapsas"
        };
        var expectedUsers = new[]
        {
            nickChapsas
        };
        _userRepository.GetAllAsync().Returns(expectedUsers);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        //result.Single().Should().BeEquivalentTo(nickChapsas);
        result.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogMessages_WhenInvoked()
    {
        // Arrange
        _userRepository.GetAllAsync().Returns(Enumerable.Empty<User>());

        // Act
        await _sut.GetAllAsync();

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Retrieving all users"));
        _logger.Received(1).LogInformation(Arg.Is("All users retrieved in {0}ms"), Arg.Any<long>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldLogMessageAndException_WhenExceptionIsThrown()
    {
        // Arrange
        var sqliteException = new SqliteException("Something went wrong", 500);
        _userRepository.GetAllAsync()
            .Throws(sqliteException);

        // Act
        var requestAction = async () => await _sut.GetAllAsync();

        // Assert
        await requestAction.Should()
            .ThrowAsync<SqliteException>().WithMessage("Something went wrong");
        _logger.Received(1).LogError(Arg.Is(sqliteException), Arg.Is("Something went wrong while retrieving all users"));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User
        {
            Id = userId,
            FullName = "Bohdan Tron"
        };

        _userRepository.GetByIdAsync(userId).Returns(expectedUser);

        // Act
        var actualUser = await _sut.GetByIdAsync(userId);

        // Assert
        expectedUser.Should().BeEquivalentTo(actualUser);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNoUserExists()
    {
        // Arrange
        _userRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogMessage_WhenRetrievingUsers()
    {
        // Arrange
        _userRepository.GetByIdAsync(Guid.Empty).Returns(new User());

        // Act
        await _sut.GetByIdAsync(Guid.Empty);

        // Assert
        _logger.Received(1).LogInformation(Arg.Is("Retrieving user with id: {0}"), Arg.Is(Guid.Empty));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLogMessage_WhenExceptionThrown()
    {
        // Arrange
        _userRepository.GetByIdAsync(Guid.Empty).Throws<Exception>();

        // Act
        var action = () => _sut.GetByIdAsync(Guid.Empty);

        // Assert
        await action.Should().ThrowAsync<Exception>();
        //_sut.GetByIdAsync(Guid.Empty).Should().Throws<Exception>();

        _logger.Received(1).LogError(
            Arg.Any<Exception>(), 
            Arg.Is("Something went wrong while retrieving user with id {0}"),
            Arg.Is(Guid.Empty));
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WhenValidDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "Bohdan Tron"
        };
        _userRepository.CreateAsync(user).Returns(true);

        // Act
        var result = await _sut.CreateAsync(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldLogMessage_WhenCreatingUser()
    {
        // Arrange
        var user = new User();
        _userRepository.CreateAsync(user).Returns(true);

        // Act
        await _sut.CreateAsync(user);

        // Assert
        _logger.Received(1).LogInformation(
            Arg.Is("Creating user with id {0} and name: {1}"),
            Arg.Any<Guid>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task CreateAsync_ShouldLogMessage_WhenExceptionThrown()
    {
        // Arrange
        var user = new User();
        _userRepository.CreateAsync(user).Throws<Exception>();

        // Act
        var action = () => _sut.CreateAsync(user);

        // Assert
        await action.Should().ThrowAsync<Exception>();
        _logger.Received(1).LogError(Arg.Any<Exception>(), "Something went wrong while creating a user");
    }


    [Fact]
    public async Task DeleteByIdAsync_ShouldDeleteUser_WhenExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.DeleteByIdAsync(userId).Returns(true);

        // Act
        var result = await _sut.DeleteByIdAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteByIdAsync_ShouldNotDeleteUser_WhenNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.DeleteByIdAsync(userId).Returns(false);

        // Act
        var result = await _sut.DeleteByIdAsync(userId);

        // Assert
        result.Should().BeFalse();
    }
}
