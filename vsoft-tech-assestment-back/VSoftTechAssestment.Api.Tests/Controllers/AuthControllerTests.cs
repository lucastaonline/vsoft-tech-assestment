using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VSoftTechAssestment.Api.Controllers;
using VSoftTechAssestment.Api.Models.DTOs.Auth;
using VSoftTechAssestment.Api.Services;

namespace VSoftTechAssestment.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
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

        var expectedResponse = new RegisterResponse
        {
            Success = true,
            Message = "Usuário criado com sucesso",
            UserId = "user-id"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<RegisterResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Contain("sucesso");
        _authServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
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

        var expectedResponse = new RegisterResponse
        {
            Success = false,
            Message = "Usuário com este email já existe",
            Errors = new List<string> { "Email já está em uso" }
        };

        _authServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<RegisterResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("já existe");
        _authServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
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

        var expectedResponse = new LoginResponse
        {
            Success = true,
            Message = "Login realizado com sucesso",
            Token = "fake-jwt-token",
            UserId = "user-id",
            UserName = "testuser",
            Email = "test@example.com",
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Token.Should().NotBeNullOrEmpty();
        response.UserId.Should().Be("user-id");
        _authServiceMock.Verify(x => x.LoginAsync(request), Times.Once);
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

        var expectedResponse = new LoginResponse
        {
            Success = false,
            Message = "Usuário ou senha inválidos"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Token.Should().BeNull();
        _authServiceMock.Verify(x => x.LoginAsync(request), Times.Once);
    }
}

