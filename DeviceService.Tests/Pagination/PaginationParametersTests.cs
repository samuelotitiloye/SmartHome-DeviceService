using DeviceService.Application.Common.Models;
using Xunit;

public class PaginationParametersTests
{
    [Fact]
    public void PageNumber_LessThanOne_DefaultsToOne()
    {
        var p = new PaginationParameters(0, 10);
        Assert.Equal(1, p.PageNumber);
    }

    [Fact]
    public void PageSize_LessThanOne_DefaultsToTen()
    {
        var p = new PaginationParameters(1, 0);
        Assert.Equal(10, p.PageSize);
    }

    [Fact]
    public void PageSize_AboveMax_IsClamped()
    {
        var p = new PaginationParameters(1, 500);
        Assert.Equal(100, p.PageSize);
    }

    [Fact]
    public void Skip_CalculatesCorrectly()
    {
        var p = new PaginationParameters(3, 10);
        Assert.Equal(20, p.Skip);
    }

    [Fact]
    public void Take_EqualsPageSize()
    {
        var p = new PaginationParameters(2, 15);
        Assert.Equal(15, p.Take);
    }
}