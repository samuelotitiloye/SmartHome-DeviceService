using DeviceService.Application.Devices.Models;
using Xunit;

public class DeviceFilterTests
{
    [Fact]
    public void Normalize_EmptyString_BecomeNull()
    {
        var f = new DeviceFilter
        {
            NameContains = "    ",
            Location = "",
            Type = "   "
        };

        f.Normalize();

        Assert.Null(f.NameContains);
        Assert.Null(f.Location);
        Assert.Null(f.Type);
    }

    [Fact]
    public void Normalize_TrimsWhitespace()
    {
        var f = new DeviceFilter
        {
            NameContains = "  kitchen sensor ",
            Location = " living room  ",
            Type = " camera "
        };
        f.Normalize();

        Assert.Equal("kitchen sensor", f.NameContains);
        Assert.Equal("living room", f.Location);
        Assert.Equal("camera", f.Type);
    }

    [Fact]
    public void Normalize_InvalidSortOrder_ResetsToDefault()
    {
        var f = new DeviceFilter
        {
            SortOrder = (SortOrder)999
        };

        f.Normalize();

        Assert.Equal(SortOrder.Desc, f.SortOrder);
    }
}