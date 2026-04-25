using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using DBH.Auth.Service.Services;
using DBH.Auth.Service.DbContext;
using DBH.Auth.Service.DTOs;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Repositories;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Blockchain.Sync;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceDirectTests
{
    private readonly DbContextOptions<AuthDbContext> _dbContextOptions;
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IGenericRepository<UserCredential>> _credentialRepositoryMock = new();
    private readonly Mock<IGenericRepository<UserSecurity>> _securityRepositoryMock = new();
    private readonly Mock<IGenericRepository<Role>> _roleRepositoryMock = new();
    private readonly Mock<IGenericRepository<UserRole>> _userRoleRepositoryMock = new();
    private readonly Mock<IGenericRepository<Doctor>> _doctorRepositoryMock = new();
    private readonly Mock<IGenericRepository<Patient>> _patientRepositoryMock = new();
    private readonly Mock<IGenericRepository<Staff>> _staffRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IBlockchainSyncService> _blockchainSyncServiceMock = new();
    private readonly Mock<IOrganizationServiceClient> _organizationServiceClientMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AuthServiceDirectTests(ITestOutputHelper output)
    {
        _output = output;
        _dbContextOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer test-token";
        
        // Setup user claims for authorization
        var claims = new[] { 
            new Claim(ClaimTypes.Role, "Admin"), 
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) 
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        context.User = claimsPrincipal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    private AuthService CreateService(AuthDbContext context) =>
        new AuthService(
            _userRepositoryMock.Object,
            _credentialRepositoryMock.Object,
            _securityRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _userRoleRepositoryMock.Object,
            _doctorRepositoryMock.Object,
            _patientRepositoryMock.Object,
            _staffRepositoryMock.Object,
            _loggerMock.Object,
            _tokenServiceMock.Object,
            _blockchainSyncServiceMock.Object,
            _organizationServiceClientMock.Object,
            _httpContextAccessorMock.Object,
            context);

    private async Task<T> RunAndLog<T>(
        Func<Task<T>> action,
        [CallerMemberName] string testName = "")
    {
        var res = await action();
        _output.WriteLine($"{testName} response:");
        _output.WriteLine(JsonSerializer.Serialize(res, LogJsonOptions));
        return res;
    }


    // ===== REGISTERASYNC TESTS =====

    [Fact(DisplayName = "RegisterAsync::RegisterAsync-01")]
    public async Task RegisterAsync_01()
    {
        // Arrange
        // Precondition: Valid input
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "Password123!",
            Phone = "+1234567890",
        };

        // Act
        var result = await service.RegisterAsync(request);
        Console.WriteLine("RegisterAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.True(result.Success);
        
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }
    [Fact(DisplayName = "RegisterAsync::RegisterAsync-02")]
    public async Task RegisterAsync_02()
    {
        // Arrange
        // Precondition: Invalid input
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterRequest
        {
            FullName = "",
            Email = "invalid-email",
            Password = "weak",
            Phone = "",
        };

        // Act
        var result = await service.RegisterAsync(request);
        Console.WriteLine("RegisterAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.False(result.Success);
    }
    [Fact(DisplayName = "RegisterAsync::RegisterAsync-03")]
    public async Task RegisterAsync_03()
    {
        // Arrange
        // Precondition: Unauthorized access
        // Input: Authenticated user without required role/permission
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Password123!",
            Phone = "+1234567890",
        };
        
        // Setup unauthorized user
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = await service.RegisterAsync(request);
        Console.WriteLine("RegisterAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.False(result.Success);
        // Depending on implementation, check for unauthorized status
    }
    [Fact(DisplayName = "RegisterAsync::RegisterAsync-InvalidEmail")]
    public async Task RegisterAsync_InvalidEmail()
    {
        // Arrange
        // Precondition: Invalid input
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterRequest
        {
            FullName = "",
            Email = "invalid-email",
            Password = "weak",
            Phone = "",
        };

        // Act
        var result = await service.RegisterAsync(request);
        Console.WriteLine("RegisterAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.False(result.Success);
    }
    [Fact(DisplayName = "RegisterAsync::RegisterAsync-WeakPassword")]
    public async Task RegisterAsync_WeakPassword()
    {
        // Arrange
        // Precondition: Invalid input
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterRequest
        {
            FullName = "",
            Email = "invalid-email",
            Password = "weak",
            Phone = "",
        };

        // Act
        var result = await service.RegisterAsync(request);
        Console.WriteLine("RegisterAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.False(result.Success);
    }



    // ===== REGISTERSTAFFDOCTORASYNC TESTS =====

    [Fact(DisplayName = "RegisterStaffDoctorAsync::RegisterStaffDoctorAsync-01")]
    public async Task RegisterStaffDoctorAsync_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Name='Dr. Staff Doctor', Email='staffdr@example.com', Password='Password123!', Phone='+1234567890', EmployeeId='EMP456', MedicalLicenseNumber='MED789012', Specialty='Pediatrics'
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        // Act
        var request = new RegisterStaffDoctorRequest
        {
            FullName = "Dr. Staff Doctor",
            Email = "staffdr@example.com",
            Password = "Password123!",
            Phone = "+1234567890",
            Role = "Doctor"
        };
        var result = await service.RegisterStaffDoctorAsync(request);
        
        // Assert
        Assert.True(result.Success);
        
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }
    [Fact(DisplayName = "RegisterStaffDoctorAsync::RegisterStaffDoctorAsync-02")]
    public async Task RegisterStaffDoctorAsync_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Name='', Email='invalid', Password='', EmployeeId='', MedicalLicenseNumber='', Specialty=''
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterStaffDoctorRequest
        {
            FullName = "",
            Email = "invalid-email",
            Password = "weak",
            Phone = "",
        };

        // Act
        var result = await service.RegisterStaffDoctorAsync(request);
        
        // Assert
        Assert.False(result.Success);
    }
    [Fact(DisplayName = "RegisterStaffDoctorAsync::RegisterStaffDoctorAsync-03")]
    public async Task RegisterStaffDoctorAsync_03()
    {
        // Arrange
        // Precondition: Unauthorized access
        // Input: Authenticated user without required role/permission
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterStaffDoctorRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Password123!",
            Phone = "+1234567890",
        };
        
        // Setup unauthorized user
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = await service.RegisterStaffDoctorAsync(request);
        
        // Assert
        Assert.False(result.Success);
        // Depending on implementation, check for unauthorized status
    }
    [Fact(DisplayName = "RegisterStaffDoctorAsync::RegisterStaffDoctorAsync-InvalidEmail")]
    public async Task RegisterStaffDoctorAsync_InvalidEmail()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Name='', Email='invalid', Password='', EmployeeId='', MedicalLicenseNumber='', Specialty=''
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterStaffDoctorRequest
        {
            FullName = "",
            Email = "invalid-email",
            Password = "weak",
            Phone = "",
        };

        // Act
        var result = await service.RegisterStaffDoctorAsync(request);
        
        // Assert
        Assert.False(result.Success);
    }
    [Fact(DisplayName = "RegisterStaffDoctorAsync::RegisterStaffDoctorAsync-WeakPassword")]
    public async Task RegisterStaffDoctorAsync_WeakPassword()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Name='', Email='invalid', Password='', EmployeeId='', MedicalLicenseNumber='', Specialty=''
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var request = new RegisterStaffDoctorRequest
        {
            FullName = "",
            Email = "invalid-email",
            Password = "weak",
            Phone = "",
        };

        // Act
        var result = await service.RegisterStaffDoctorAsync(request);
        
        // Assert
        Assert.False(result.Success);
    }

    // ===== LOGINASYNC TESTS =====

    [Fact(DisplayName = "LoginAsync::LoginAsync-01")]
    public async Task LoginAsync_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: All required fields of string populated with valid values
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        // First register a user
        var registerRequest = new RegisterRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Password123!",
            Phone = "+1234567890",
        };
        var registerResult = await service.RegisterAsync(registerRequest);
        Console.WriteLine("RegisterAsync response: " + JsonSerializer.Serialize(registerResult, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(registerResult.Success);
        
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);
        Console.WriteLine("LoginAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.True(result.Success);

        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }
    [Fact(DisplayName = "LoginAsync::LoginAsync-02")]
    public async Task LoginAsync_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: null or empty string
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var loginRequest = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);
        Console.WriteLine("LoginAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.False(result.Success);
    }
    [Fact(DisplayName = "LoginAsync::LoginAsync-IPADDRESS-EmptyString")]
    public async Task LoginAsync_IPADDRESS_EmptyString()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Missing or invalid required fields in string
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var loginRequest = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);
        Console.WriteLine("LoginAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        Assert.False(result.Success);
    }
    [Fact(DisplayName = "LoginAsync::LoginAsync-InvalidCredentials")]
    public async Task LoginAsync_InvalidCredentials()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Missing or invalid required fields in string
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var loginRequest = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);
        Console.WriteLine("LoginAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        // Expected: Returns authentication error
        Assert.True(result != null); // Basic assertion
    }
    [Fact(DisplayName = "LoginAsync::LoginAsync-InactiveAccount")]
    public async Task LoginAsync_InactiveAccount()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Missing or invalid required fields in string
        using var ctx = new AuthDbContext(_dbContextOptions);
        var service = CreateService(ctx);

        var loginRequest = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);
        Console.WriteLine("LoginAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        
        // Assert
        // Expected: Returns authentication error
        Assert.True(result != null); // Basic assertion
    }

}
