using LineAdjustment.Algorithm;
using Xunit;

namespace LineAdjustment.Tests.Algorithm;

public class LineAdjustmentAlgorithmTest
{
    [Theory]
    [InlineData("Слово", 10)]
    public void Should_Adjust_Line_With_One_Word_And_Whitespace_When_Pass_Text(string text, int lineWidth)
    {
        //Arrange
        var sut = new LineAdjustmentAlgorithm();

        //Act
        var result = sut.Transform(text, lineWidth);

#if DEBUG
        //Assert
        Assert.Equal("Слово+++++", result);
#else
        //Assert
        Assert.Equal("Слово     ", result);
#endif
    }
    
    [Theory]
    [InlineData("Слово второе слово", 20)]
    public void Should_Adjust_Line_With_Words_And_Equal_Number_Of_Whitespace_When_Pass_Text(string text, int lineWidth)
    {
        //Arrange
        var sut = new LineAdjustmentAlgorithm();

        //Act
        var result = sut.Transform(text, lineWidth);

#if DEBUG
        //Assert
        Assert.Equal("Слово++второе++слово", result);
#else
        //Assert
        Assert.Equal("Слово  второе  слово", result);
#endif
    }
    
    [Theory]
    [InlineData("Слово второе слово", 21)]
    public void Should_Adjust_Line_With_Words_And_Not_Equal_Number_Of_Whitespace_When_Pass_Text(string text, int lineWidth)
    {
            //Arrange
            var sut = new LineAdjustmentAlgorithm();

            //Act
            var result = sut.Transform(text, lineWidth);

#if DEBUG
            //Assert
            Assert.Equal("Слово+++второе++слово", result);
#else
        //Assert
        Assert.Equal("Слово   второе  слово", result);
#endif
    }
}