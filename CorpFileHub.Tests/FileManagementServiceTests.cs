using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CorpFileHub.Tests;

public class FileManagementServiceTests
{
    private FileManagementService CreateService(
        Mock<IFileRepository>? fileRepo = null,
        Mock<IAccessControlService>? acs = null,
        Mock<IFileStorageService>? storage = null,
        Mock<IYandexDiskService>? disk = null,
        Mock<IAuditService>? audit = null,
        Mock<IConfiguration>? config = null)
    {
        fileRepo ??= new Mock<IFileRepository>();
        acs ??= new Mock<IAccessControlService>();
        storage ??= new Mock<IFileStorageService>();
        disk ??= new Mock<IYandexDiskService>();
        audit ??= new Mock<IAuditService>();
        config ??= new Mock<IConfiguration>();
        var logger = Mock.Of<ILogger<FileManagementService>>();
        return new FileManagementService(fileRepo.Object, acs.Object, storage.Object, disk.Object, audit.Object, logger, config.Object);
    }

    [Fact]
    public async Task GetFileWithAccessCheck_NoAccess_ReturnsNull()
    {
        var acs = new Mock<IAccessControlService>();
        acs.Setup(a => a.CanReadFileAsync(1, 2)).ReturnsAsync(false);

        var fileRepo = new Mock<IFileRepository>(MockBehavior.Strict);

        var service = CreateService(fileRepo, acs);

        var result = await service.GetFileWithAccessCheckAsync(1, 2);

        Assert.Null(result);
        fileRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetFileHashAsync_CalculatesCorrectHash()
    {
        var service = CreateService();
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));

        var hash = await service.GetFileHashAsync(ms);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        ms.Position = 0;
        var expected = Convert.ToHexString(sha256.ComputeHash(ms));

        Assert.Equal(expected, hash);
    }
}
