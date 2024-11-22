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
		Assert.Equal(stringy.buffer, Array.Empty<byte>());
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
}