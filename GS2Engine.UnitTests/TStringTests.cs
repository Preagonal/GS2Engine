namespace GS2Engine.UnitTests;

public class TStringTests
{
	[Fact]
	public void When_input_is_byte_array_Then_return_TString_with_copy_of_byte_array_as_buffer()
	{
		//Arrange
		byte[] bytes = { 0, 1, 2, 3 };

		//Act
		TString fromBytes = bytes;

		//Assert
		Assert.Equal(fromBytes.buffer, bytes);
	}

	[Fact]
	public void When_input_is_none_Then_return_TString_with_empty_byte_array_as_buffer()
	{
		//Arrange
		//Act
		TString stringy = new();

		//Assert
		Assert.Equal(stringy.buffer, []);
	}

	[Fact]
	public void When_checking_length_Then_value_is_correct()
	{
		//Arrange
		TString stringy = "asd";

		//Act
		var length = stringy.length();


		//Assert
		Assert.Equal(length, stringy.Length);
	}

	[Fact]
	public void When_adding_tstring_Then_value_is_correct()
	{
		//Arrange
		TString string1 = "ASD";
		TString string2 = "123";

		//Act
		string1 += string2;
		string1.writeChar(64);

		//Assert
		Assert.Equal("asd123@", string1.ToLower().ToString());
	}

	[Fact]
	public void Given_equal_operator_When_comparing_tstring_Then_value_is_true()
	{
		//Arrange
		TString string1 = "ASD";
		TString string2 = "ASD";

		//Act
		//Assert
		Assert.True(string1 == string2);
	}


	[Fact]
	public void Given_equal_operator_When_comparing_tstring_Then_value_is_false()
	{
		//Arrange
		TString string1 = "ASD";
		TString string2 = "asd";

		//Act
		//Assert
		Assert.False(string1 == string2);
	}

	[Fact]
	public void Given_not_equal_operator_When_comparing_tstring_Then_value_is_false()
	{
		//Arrange
		TString string1 = "ASD";
		TString string2 = "ASD";

		//Act
		//Assert
		Assert.False(string1 != string2);
	}

	[Fact]
	public void Given_not_equal_operator_When_comparing_tstring_Then_value_is_true()
	{
		//Arrange
		TString string1 = "ASD";
		TString string2 = "asd";

		//Act
		//Assert
		Assert.True(string1 != string2);
	}

	[Fact]
	public void Given_empty_When_tstring_reading_integer_Then_value_is_zero()
	{
		//Arrange
		TString string1 = "";

		//Act
		var result = string1.readInt();

		//Assert
		Assert.Equal(0, result);
	}

	[Fact]
	public void Given_value_When_tstring_starts_with_value_Then_result_is_true()
	{
		//Arrange
		TString string1 = "asdasdasdasdasd";

		//Act
		var result = string1.StartsWith("asd", StringComparison.CurrentCulture);

		//Assert
		Assert.True(result);
	}

	[Fact]
	public void Given_value_When_tstring_not_starts_with_value_Then_result_is_false()
	{
		//Arrange
		TString string1 = "yasdasdasdasdasd";

		//Act
		var result = string1.StartsWith("asd", StringComparison.CurrentCulture);

		//Assert
		Assert.False(result);
	}
}