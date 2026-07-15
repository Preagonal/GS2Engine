namespace Preagonal.Scripting.GS2Engine.UnitTests;

public class ToolsTests
{
	[Fact]
	public void Given_hex_precision_format_When_formatting_rgb_values_Then_values_are_zero_padded()
	{
		var formatted = Tools.Format("#%.2x%.2x%.2x", 0, 224, 255);

		Assert.Equal("#00e0ff", formatted);
	}
}
