using TeamTalk.Domain.Common;
using TeamTalk.Domain.Entities;

namespace TeamTalk.Domain.Tests.Entities;

public class WorkspaceTests
{
    [Fact]
    public void Constructor_WithValidName_SetsProperties()
    {
        // arrange
        var id = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        // act
        var workspace = new Workspace(id, "  Engineering  ", createdByUserId, createdAtUtc);

        // assert
        Assert.Equal(id, workspace.Id);
        Assert.Equal("Engineering", workspace.Name);
        Assert.Equal(createdByUserId, workspace.CreatedByUserId);
        Assert.Equal(createdAtUtc, workspace.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string name)
    {
        // act & assert
        Assert.Throws<DomainException>(() =>
            new Workspace(Guid.NewGuid(), name, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithNameExceedingMaxLength_Throws()
    {
        // arrange
        var tooLong = new string('a', Workspace.MaxNameLength + 1);

        // act & assert
        Assert.Throws<DomainException>(() =>
            new Workspace(Guid.NewGuid(), tooLong, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithNameOfExactlyMaxLength_SetsName()
    {
        // arrange
        var name = new string('a', Workspace.MaxNameLength);

        // act
        var workspace = new Workspace(Guid.NewGuid(), name, Guid.NewGuid(), DateTime.UtcNow);

        // assert
        Assert.Equal(name, workspace.Name);
    }

    [Fact]
    public void Constructor_WithPaddedNameWhoseTrimmedLengthIsMax_SetsTrimmedName()
    {
        // arrange
        var name = new string('a', Workspace.MaxNameLength);

        // act
        var workspace = new Workspace(Guid.NewGuid(), $"  {name}  ", Guid.NewGuid(), DateTime.UtcNow);

        // assert
        Assert.Equal(name, workspace.Name);
    }

    [Fact]
    public void Constructor_SetsUpdatedAtToCreatedAtAndHasNoIcon()
    {
        // arrange
        var createdAtUtc = DateTime.UtcNow;

        // act
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), createdAtUtc);

        // assert
        Assert.Equal(createdAtUtc, workspace.UpdatedAtUtc);
        Assert.False(workspace.HasIcon);
        Assert.Null(workspace.Description);
    }

    [Fact]
    public void UpdateDetails_WithValidValues_TrimsAndUpdatesFields()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var nowUtc = DateTime.UtcNow.AddMinutes(5);

        // act
        workspace.UpdateDetails("  Platform  ", "  The platform team  ", nowUtc);

        // assert
        Assert.Equal("Platform", workspace.Name);
        Assert.Equal("The platform team", workspace.Description);
        Assert.Equal(nowUtc, workspace.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithBlankDescription_ClearsDescription(string? description)
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.UpdateDetails("Engineering", "Existing description", DateTime.UtcNow);

        // act
        workspace.UpdateDetails("Engineering", description, DateTime.UtcNow);

        // assert
        Assert.Null(workspace.Description);
    }

    [Fact]
    public void UpdateDetails_WithDescriptionOfExactlyMaxLength_SetsDescription()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var description = new string('d', Workspace.MaxDescriptionLength);

        // act
        workspace.UpdateDetails("Engineering", description, DateTime.UtcNow);

        // assert
        Assert.Equal(description, workspace.Description);
    }

    [Fact]
    public void UpdateDetails_WithDescriptionExceedingMaxLength_Throws()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var tooLong = new string('d', Workspace.MaxDescriptionLength + 1);

        // act & assert
        Assert.Throws<DomainException>(() => workspace.UpdateDetails("Engineering", tooLong, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithBlankName_Throws(string name)
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);

        // act & assert
        Assert.Throws<DomainException>(() => workspace.UpdateDetails(name, null, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateDetails_WithNameExceedingMaxLength_Throws()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var tooLong = new string('a', Workspace.MaxNameLength + 1);

        // act & assert
        Assert.Throws<DomainException>(() => workspace.UpdateDetails(tooLong, null, DateTime.UtcNow));
    }

    [Fact]
    public void SetIcon_WithValidData_SetsIconAndUpdatedAt()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        var iconData = new byte[] { 1, 2, 3 };
        var nowUtc = DateTime.UtcNow.AddMinutes(1);

        // act
        workspace.SetIcon(iconData, "image/png", nowUtc);

        // assert
        Assert.True(workspace.HasIcon);
        Assert.Equal(iconData, workspace.IconData);
        Assert.Equal("image/png", workspace.IconContentType);
        Assert.Equal(nowUtc, workspace.UpdatedAtUtc);
    }

    [Fact]
    public void SetIcon_WithEmptyData_Throws()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);

        // act & assert
        Assert.Throws<DomainException>(() => workspace.SetIcon([], "image/png", DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetIcon_WithBlankContentType_Throws(string contentType)
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);

        // act & assert
        Assert.Throws<DomainException>(() => workspace.SetIcon([1], contentType, DateTime.UtcNow));
    }

    [Fact]
    public void RemoveIcon_WhenIconIsSet_ClearsIconAndUpdatesTimestamp()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([1, 2, 3], "image/png", DateTime.UtcNow);
        var nowUtc = DateTime.UtcNow.AddMinutes(2);

        // act
        workspace.RemoveIcon(nowUtc);

        // assert
        Assert.False(workspace.HasIcon);
        Assert.Null(workspace.IconData);
        Assert.Null(workspace.IconContentType);
        Assert.Equal(nowUtc, workspace.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveIcon_WhenNoIconIsSet_DoesNotThrow()
    {
        // arrange
        var workspace = new Workspace(Guid.NewGuid(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);

        // act
        workspace.RemoveIcon(DateTime.UtcNow);

        // assert
        Assert.False(workspace.HasIcon);
    }
}
