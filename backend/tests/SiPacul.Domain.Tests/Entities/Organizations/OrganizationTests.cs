using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Domain.Tests.Entities.Organizations;

public sealed class OrganizationTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateOrganization()
    {
        var organization = Organization.Create(
            "  org-001  ",
            "  Bisnis Pertanian Internal  ",
            "  PT Pertanian Internal  ",
            "  Asia/Makassar  ");

        Assert.NotEqual(
            Guid.Empty,
            organization.Id);

        Assert.Equal(
            "ORG-001",
            organization.Code);

        Assert.Equal(
            "Bisnis Pertanian Internal",
            organization.Name);

        Assert.Equal(
            "PT Pertanian Internal",
            organization.LegalName);

        Assert.Equal(
            "Asia/Makassar",
            organization.TimeZone);

        Assert.True(organization.IsActive);
        Assert.False(organization.IsDeleted);
        Assert.Null(organization.UpdatedAt);
    }

    [Fact]
    public void Create_WithoutTimeZone_ShouldUseDefaultTimeZone()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Bisnis Pertanian Internal");

        Assert.Equal(
            Organization.DefaultTimeZone,
            organization.TimeZone);

        Assert.Null(organization.LegalName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_ShouldThrowArgumentException(
        string invalidCode)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                invalidCode,
                "Bisnis Pertanian Internal"));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("ORG 001")]
    [InlineData("ORG/001")]
    [InlineData("ORG.001")]
    public void Create_WithInvalidCodeCharacters_ShouldThrowArgumentException(
        string invalidCode)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                invalidCode,
                "Bisnis Pertanian Internal"));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_WithCodeExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var invalidCode = new string(
            'A',
            Organization.MaxCodeLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                invalidCode,
                "Bisnis Pertanian Internal"));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowArgumentException(
        string invalidName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                "ORG-001",
                invalidName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithNameExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var invalidName = new string(
            'A',
            Organization.MaxNameLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                "ORG-001",
                invalidName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithLegalNameExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var invalidLegalName = new string(
            'A',
            Organization.MaxLegalNameLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                "ORG-001",
                "Bisnis Pertanian Internal",
                invalidLegalName));

        Assert.Equal(
            "legalName",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithTimeZoneExceedingMaximumLength_ShouldThrowArgumentException()
    {
        var invalidTimeZone = new string(
            'A',
            Organization.MaxTimeZoneLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            Organization.Create(
                "ORG-001",
                "Bisnis Pertanian Internal",
                null,
                invalidTimeZone));

        Assert.Equal(
            "timeZone",
            exception.ParamName);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateOrganization()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Nama Awal");

        organization.Update(
            "  Nama Baru  ",
            "  PT Nama Baru  ",
            "  Asia/Jayapura  ");

        Assert.Equal(
            "Nama Baru",
            organization.Name);

        Assert.Equal(
            "PT Nama Baru",
            organization.LegalName);

        Assert.Equal(
            "Asia/Jayapura",
            organization.TimeZone);

        Assert.NotNull(organization.UpdatedAt);
    }

    [Fact]
    public void Update_WithUnchangedData_ShouldNotSetUpdatedAt()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Bisnis Pertanian Internal",
            "PT Pertanian Internal",
            "Asia/Jakarta");

        organization.Update(
            "  Bisnis Pertanian Internal  ",
            "  PT Pertanian Internal  ",
            "  Asia/Jakarta  ");

        Assert.Null(organization.UpdatedAt);
    }

    [Fact]
    public void Update_WithEmptyLegalName_ShouldSetLegalNameToNull()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Bisnis Pertanian Internal",
            "PT Pertanian Internal");

        organization.Update(
            "Bisnis Pertanian Internal",
            "   ",
            "Asia/Jakarta");

        Assert.Null(organization.LegalName);
        Assert.NotNull(organization.UpdatedAt);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeOrganizationStatus()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Bisnis Pertanian Internal");

        organization.Deactivate();

        Assert.False(organization.IsActive);
        Assert.NotNull(organization.UpdatedAt);

        organization.Activate();

        Assert.True(organization.IsActive);
        Assert.NotNull(organization.UpdatedAt);
    }

    [Fact]
    public void ActivateAndDeactivate_WhenStateIsUnchanged_ShouldNotChangeTimestamp()
    {
        var organization = Organization.Create(
            "ORG-001",
            "Bisnis Pertanian Internal");

        organization.Activate();

        Assert.Null(organization.UpdatedAt);

        organization.Deactivate();

        var deactivatedAt = organization.UpdatedAt;

        organization.Deactivate();

        Assert.Equal(
            deactivatedAt,
            organization.UpdatedAt);

        organization.Activate();

        var activatedAt = organization.UpdatedAt;

        organization.Activate();

        Assert.Equal(
            activatedAt,
            organization.UpdatedAt);
    }
}
