using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using VSoftTechAssestment.Api.Controllers;
using VSoftTechAssestment.Api.Models.DTOs.Auth;

namespace VSoftTechAssestment.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<SignInManager<IdentityUser>> _signInManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
            _userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, null, null, null);

        _configurationMock = new Mock<IConfiguration>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Secret"]).Returns("YourSuperSecretKeyThatMustBeAtLeast32CharactersLong!");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        jwtSection.Setup(x => x.GetValue<int>("ExpirationMinutes", 60)).Returns(60);
        _configurationMock.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _controller = new AuthController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            UserName = "testuser",
            Password = "Password123"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((IdentityUser?)null);
        _userManagerMock.Setup(x => x.FindByNameAsync(request.UserName))
            .ReturnsAsync((IdentityUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<RegisterResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Contain("sucesso");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            UserName = "newuser",
            Password = "Password123"
        };

        var existingUser = new IdentityUser { Email = request.Email };
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<RegisterResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("já existe");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserNameOrEmail = "testuser",
            Password = "Password123"
        };

        var user = new IdentityUser
        {
            Id = "user-id",
            UserName = "testuser",
            Email = "test@example.com"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(request.UserNameOrEmail))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Token.Should().NotBeNullOrEmpty();
        response.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserNameOrEmail = "testuser",
            Password = "WrongPassword"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(request.UserNameOrEmail))
            .ReturnsAsync((IdentityUser?)null);
        _userManagerMock.Setup(x => x.FindByEmailAsync(request.UserNameOrEmail))
            .ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Token.Should().BeNull();
    }
}

