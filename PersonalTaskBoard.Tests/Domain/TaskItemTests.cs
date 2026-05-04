using PersonalTaskBoard.Domain.Enums;

namespace PersonalTaskBoard.Tests.Domain;

public class TaskItemTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Priority_ValidValues_AreAccepted(int value)
    {
        var priority = (Priority)value;
        Assert.True(Enum.IsDefined(typeof(Priority), priority));
    }

    [Fact]
    public void Priority_Value4_IsNotDefined()
    {
        var priority = (Priority)4;
        Assert.False(Enum.IsDefined(typeof(Priority), priority));
    }
}
