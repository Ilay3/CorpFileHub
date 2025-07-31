using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using CorpFileHub.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace CorpFileHub.Tests;

public class AccessControlServiceTests
{
    private AccessControlService CreateService(
        Mock<IFileRepository>? fileRepo = null,
        Mock<IFolderRepository>? folderRepo = null,
        Mock<IUserRepository>? userRepo = null)
    {
        fileRepo ??= new Mock<IFileRepository>();
        folderRepo ??= new Mock<IFolderRepository>();
        userRepo ??= new Mock<IUserRepository>();
        var logger = Mock.Of<ILogger<AccessControlService>>();
        return new AccessControlService(fileRepo.Object, folderRepo.Object, userRepo.Object, logger);
    }

    [Fact]
    public async Task GetFileAccessLevel_AdminUser_ReturnsAdmin()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsAdmin = true, IsActive = true });
        var service = CreateService(userRepo: userRepo);

        var level = await service.GetFileAccessLevelAsync(10, 1);

        Assert.Equal(AccessLevel.Admin, level);
    }

    [Fact]
    public async Task GetFileAccessLevel_FileOwner_ReturnsAdmin()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new User { Id = 2, IsActive = true });

        var fileRepo = new Mock<IFileRepository>();
        fileRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new FileItem { Id = 5, OwnerId = 2, FolderId = 1 });

        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Folder { Id = 1, OwnerId = 2 });

        var service = CreateService(fileRepo, folderRepo, userRepo);

        var level = await service.GetFileAccessLevelAsync(5, 2);

        Assert.Equal(AccessLevel.Admin, level);
    }

    [Fact]
    public async Task GetFileAccessLevel_ExplicitRule_ReturnsWrite()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new User { Id = 3, IsActive = true });

        var accessRule = new AccessRule { UserId = 3, AccessLevel = AccessLevel.Write, IsActive = true };
        var file = new FileItem { Id = 20, OwnerId = 1, FolderId = 2, AccessRules = new List<AccessRule> { accessRule } };

        var fileRepo = new Mock<IFileRepository>();
        fileRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(file);

        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Folder { Id = 2, OwnerId = 1 });

        var service = CreateService(fileRepo, folderRepo, userRepo);

        var level = await service.GetFileAccessLevelAsync(20, 3);

        Assert.Equal(AccessLevel.Write, level);
    }
}
