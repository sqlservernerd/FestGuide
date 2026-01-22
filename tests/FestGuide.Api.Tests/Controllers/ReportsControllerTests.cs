using System.Security.Claims;
using FluentAssertions;
using Moq;
using FestGuide.Api.Controllers;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FestGuide.Api.Tests.Controllers;

public class ReportsControllerTests
{
    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<ILogger<ReportsController>> _mockLogger;
    private readonly ReportsController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ReportsControllerTests()
    {
        _mockExportService = new Mock<IExportService>();
        _mockLogger = new Mock<ILogger<ReportsController>>();

        _sut = new ReportsController(
            _mockExportService.Object,
            _mockLogger.Object);

        SetupUserContext();
    }

    #region ExportEditionData Tests

    [Fact]
    public async Task ExportEditionData_WithValidRequest_ReturnsFileContent()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var request = new ExportRequest("csv", true, true, true, null, null);
        var exportResult = new ExportResultDto(
            "edition_export.csv",
            "text/csv",
            System.Text.Encoding.UTF8.GetBytes("test,data"));

        _mockExportService.Setup(s => s.ExportEditionDataAsync(editionId, _userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        // Act
        var result = await _sut.ExportEditionData(editionId, request, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeEquivalentTo(exportResult.Data);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("edition_export.csv");
    }

    [Fact]
    public async Task ExportEditionData_WithNonExistentEdition_Returns404NotFound()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var request = new ExportRequest("csv", true, true, true, null, null);

        _mockExportService.Setup(s => s.ExportEditionDataAsync(editionId, _userId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EditionNotFoundException(editionId));

        // Act
        var result = await _sut.ExportEditionData(editionId, request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("EDITION_NOT_FOUND");
    }

    [Fact]
    public async Task ExportEditionData_WithoutPermission_Returns403Forbidden()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var request = new ExportRequest("csv", true, true, true, null, null);

        _mockExportService.Setup(s => s.ExportEditionDataAsync(editionId, _userId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Access denied"));

        // Act
        var result = await _sut.ExportEditionData(editionId, request, CancellationToken.None);

        // Assert
        var forbiddenResult = result.Should().BeOfType<ObjectResult>().Subject;
        forbiddenResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var error = forbiddenResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task ExportEditionData_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var request = new ExportRequest("csv", true, true, true, null, null);
        var controller = new ReportsController(_mockExportService.Object, _mockLogger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.ExportEditionData(editionId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region ExportScheduleCsv Tests

    [Fact]
    public async Task ExportScheduleCsv_WithValidEditionId_ReturnsFileContent()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var exportResult = new ExportResultDto(
            "schedule.csv",
            "text/csv",
            System.Text.Encoding.UTF8.GetBytes("TimeSlotId,StageId,StageName"));

        _mockExportService.Setup(s => s.ExportScheduleCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        // Act
        var result = await _sut.ExportScheduleCsv(editionId, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeEquivalentTo(exportResult.Data);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("schedule.csv");
    }

    [Fact]
    public async Task ExportScheduleCsv_WithNonExistentEdition_Returns404NotFound()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockExportService.Setup(s => s.ExportScheduleCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EditionNotFoundException(editionId));

        // Act
        var result = await _sut.ExportScheduleCsv(editionId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("EDITION_NOT_FOUND");
    }

    [Fact]
    public async Task ExportScheduleCsv_WithoutPermission_Returns403Forbidden()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockExportService.Setup(s => s.ExportScheduleCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Access denied"));

        // Act
        var result = await _sut.ExportScheduleCsv(editionId, CancellationToken.None);

        // Assert
        var forbiddenResult = result.Should().BeOfType<ObjectResult>().Subject;
        forbiddenResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    #endregion

    #region ExportArtistsCsv Tests

    [Fact]
    public async Task ExportArtistsCsv_WithValidEditionId_ReturnsFileContent()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var exportResult = new ExportResultDto(
            "artists.csv",
            "text/csv",
            System.Text.Encoding.UTF8.GetBytes("ArtistId,Name,Genre"));

        _mockExportService.Setup(s => s.ExportArtistsCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        // Act
        var result = await _sut.ExportArtistsCsv(editionId, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeEquivalentTo(exportResult.Data);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("artists.csv");
    }

    [Fact]
    public async Task ExportArtistsCsv_WithNonExistentEdition_Returns404NotFound()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockExportService.Setup(s => s.ExportArtistsCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EditionNotFoundException(editionId));

        // Act
        var result = await _sut.ExportArtistsCsv(editionId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("EDITION_NOT_FOUND");
    }

    #endregion

    #region ExportAnalyticsCsv Tests

    [Fact]
    public async Task ExportAnalyticsCsv_WithValidEditionId_ReturnsFileContent()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var exportResult = new ExportResultDto(
            "analytics.csv",
            "text/csv",
            System.Text.Encoding.UTF8.GetBytes("Metric,Value"));

        _mockExportService.Setup(s => s.ExportAnalyticsCsvAsync(editionId, _userId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        // Act
        var result = await _sut.ExportAnalyticsCsv(editionId, null, null, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeEquivalentTo(exportResult.Data);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("analytics.csv");
    }

    [Fact]
    public async Task ExportAnalyticsCsv_WithDateRange_PassesParameters()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var fromUtc = DateTime.UtcNow.AddDays(-30);
        var toUtc = DateTime.UtcNow;
        var exportResult = new ExportResultDto(
            "analytics.csv",
            "text/csv",
            System.Text.Encoding.UTF8.GetBytes("Metric,Value"));

        _mockExportService.Setup(s => s.ExportAnalyticsCsvAsync(editionId, _userId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        // Act
        var result = await _sut.ExportAnalyticsCsv(editionId, fromUtc, toUtc, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.Should().NotBeNull();
        _mockExportService.Verify(s => s.ExportAnalyticsCsvAsync(
            editionId,
            _userId,
            fromUtc,
            toUtc,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportAnalyticsCsv_WithNonExistentEdition_Returns404NotFound()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockExportService.Setup(s => s.ExportAnalyticsCsvAsync(editionId, _userId, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EditionNotFoundException(editionId));

        // Act
        var result = await _sut.ExportAnalyticsCsv(editionId, null, null, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("EDITION_NOT_FOUND");
    }

    #endregion

    #region ExportAttendeeSavesCsv Tests

    [Fact]
    public async Task ExportAttendeeSavesCsv_WithValidEditionId_ReturnsFileContent()
    {
        // Arrange
        var editionId = Guid.NewGuid();
        var exportResult = new ExportResultDto(
            "attendee_saves.csv",
            "text/csv",
            System.Text.Encoding.UTF8.GetBytes("EngagementId,ArtistName,StageName,SaveCount"));

        _mockExportService.Setup(s => s.ExportAttendeeSavesCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        // Act
        var result = await _sut.ExportAttendeeSavesCsv(editionId, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeEquivalentTo(exportResult.Data);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("attendee_saves.csv");
    }

    [Fact]
    public async Task ExportAttendeeSavesCsv_WithNonExistentEdition_Returns404NotFound()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockExportService.Setup(s => s.ExportAttendeeSavesCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EditionNotFoundException(editionId));

        // Act
        var result = await _sut.ExportAttendeeSavesCsv(editionId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("EDITION_NOT_FOUND");
    }

    [Fact]
    public async Task ExportAttendeeSavesCsv_WithoutPermission_Returns403Forbidden()
    {
        // Arrange
        var editionId = Guid.NewGuid();

        _mockExportService.Setup(s => s.ExportAttendeeSavesCsvAsync(editionId, _userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Access denied"));

        // Act
        var result = await _sut.ExportAttendeeSavesCsv(editionId, CancellationToken.None);

        // Assert
        var forbiddenResult = result.Should().BeOfType<ObjectResult>().Subject;
        forbiddenResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var error = forbiddenResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("FORBIDDEN");
    }

    #endregion

    #region Helper Methods

    private void SetupUserContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
