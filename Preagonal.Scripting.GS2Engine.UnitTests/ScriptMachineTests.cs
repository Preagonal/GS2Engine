using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Testing;
using Preagonal.Scripting.GS2Engine.Enums;
using Preagonal.Scripting.GS2Engine.Extensions;
using Preagonal.Scripting.GS2Engine.GS2.ByteCode;
using Preagonal.Scripting.GS2Engine.GS2.Script;
using Preagonal.Scripting.GS2Engine.Models;
using Preagonal.Scripting.GS2Engine.Models.Properties;
using Preagonal.Scripting.GS2Engine.UnitTests.Objects;
using Xunit.Abstractions;

namespace Preagonal.Scripting.GS2Engine.UnitTests;

public class ScriptMachineTests
{
	private          int                     _calledTimes;
	private readonly Dictionary<int, string> _receivedStrings = new();
	private readonly ScriptManager           _scriptManager;

	public ScriptMachineTests(ITestOutputHelper testOutputHelper)
	{
		_scriptManager = new(new FakeLogger<ScriptManager>(_ => { }));

		Tools.SetDebugFuncWrite(testOutputHelper.WriteLine);
		Tools.SetDebugFuncWriteLine(testOutputHelper.WriteLine);
		Tools.DEBUG_ON = true;

		ConcurrentDictionary<int, Drawing> drawings = new();
		ScriptProperties<ScriptMachineTests>.AddProperties(
			null,
			new()
			{
				{ "screenwidth", "The width of the game screen", _ => 1024 },
				{ "screenheight", "The height of the game screen", _ => 1024 },
			}
		);

		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{
					"echo",
					"",
					EchoCallback
				},
				{
					"showimg",
					"",
					(_, args) =>
					{
						if (!(args?.Length > 3)) return 0;
						try
						{
							var     index = (int)args[0]!.GetValue<double>();
							string? image = args[1]?.GetValue<TString>() ?? string.Empty;

							var x = (int)args[2]!.GetValue<double>();
							var y = (int)args[3]!.GetValue<double>();
							if (drawings.TryGetValue(index, out var value))
							{
								value.ShowImg(image, x, y);
							}
							else
							{
								value = new(image, x, y);
								drawings.AddOrUpdate(index, value, (_, _) => value);
							}
						}
						catch (Exception)
						{
							//_logger.LogDebug(e.Message);
						}

						return 0;
					}
				},
				{
					"findimg",
					"",
					(_, args) =>
					{
						if (!(args?.Length > 0)) return null;
						try
						{
							var index = (int)args[0]!.GetValue<double>();

							if (drawings.TryGetValue(index, out var value))
							{
								return value;
							}
						}
						catch (Exception)
						{
							//_logger.LogDebug(e.Message);
						}

						return null;
					}
				},
				{
					"getimgwidth",
					"",
					(_, args) =>
					{
						if (!(args?.Length > 0)) return 0;
						try
						{
							var image = args[0]!.GetValue<TString>();

							if (image != null) Console.WriteLine(image);

							return 1;

						}
						catch (Exception)
						{
							//_logger.LogDebug(e.Message);
						}

						return 0;
					}
				},
			}
		);

		foreach (var property in ScriptManager.GlobalProperties.Where(x => !x.Value.Compiled))
		{
			property.Value.Compile();
		}
	}

	private int EchoCallback(ScriptMachineTests _, IStackEntry[] args)
	{
		_receivedStrings[_calledTimes] = args[0]?.GetValue()?.ToString()??"";

		Console.WriteLine(_receivedStrings[_calledTimes]);

		_calledTimes++;

		return 0;
	}

	private Script CompileScript(string scriptText, string scriptName = "testScript", ScriptVariable? refObject = null)
	{
		var response = GS2Compiler.Interface.CompileCode(
			scriptText,
			"weapon",
			scriptName,
			withHeader: false
		);

		if (response.Success)
		{
			// Arrange
			return new(_scriptManager, scriptName, response.ByteCode, refObject);
		}

		throw new($"Script failure: {response.ErrMsg}");
	}

	private Script CompileLegacyBytecodeScript(string objectPath, string scriptName = "legacyScript") =>
		new(_scriptManager, scriptName, CreateObjFromStrBytecode(objectPath));

	private Script CompileLegacyParamsBytecodeScript(string scriptName = "legacyParamsScript") =>
		new(_scriptManager, scriptName, CreateParamsOpcodeBytecode());

	private Script CompileRawBytecodeScript(IReadOnlyCollection<byte> code, IReadOnlyList<string>? strings = null, string scriptName = "rawScript") =>
		new(_scriptManager, scriptName, CreateRawReturnBytecode(code, strings));

	private static byte[] CreateRawReturnBytecode(IReadOnlyCollection<byte> code, IReadOnlyList<string>? strings)
	{
		TString result = new();
		WriteSegment(result, BytecodeSegment.Gs1EventFlags, [0, 0, 0, 0]);

		TString functions = new();
		functions.writeInt(0);
		functions.writeCString("onCreated");
		WriteSegment(result, BytecodeSegment.FunctionNames, functions.toByteArray());

		TString stringSegment = new();
		if (strings != null)
		{
			foreach (var value in strings)
				stringSegment.writeCString(value);
		}
		WriteSegment(result, BytecodeSegment.Strings, stringSegment.toByteArray());

		List<byte> bytecode = new(code) { (byte)Opcode.OP_RET };
		WriteSegment(result, BytecodeSegment.Bytecode, bytecode);
		result.writeByte((byte)'\n');

		return result.toByteArray();
	}

	private static byte[] CreateObjFromStrBytecode(string objectPath)
	{
		TString result = new();
		WriteSegment(result, BytecodeSegment.Gs1EventFlags, [0, 0, 0, 0]);

		TString functions = new();
		functions.writeInt(0);
		functions.writeCString("onCreated");
		WriteSegment(result, BytecodeSegment.FunctionNames, functions.toByteArray());

		TString strings = new();
		strings.writeCString(objectPath);
		WriteSegment(result, BytecodeSegment.Strings, strings.toByteArray());

		byte[] code =
		[
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_OBJ_FROM_STR,
			(byte)Opcode.OP_RET,
		];
		WriteSegment(result, BytecodeSegment.Bytecode, code);
		result.writeByte((byte)'\n');

		return result.toByteArray();
	}

	private static byte[] CreateParamsOpcodeBytecode()
	{
		TString result = new();
		WriteSegment(result, BytecodeSegment.Gs1EventFlags, [0, 0, 0, 0]);

		TString functions = new();
		functions.writeInt(0);
		functions.writeCString("onCreated");
		WriteSegment(result, BytecodeSegment.FunctionNames, functions.toByteArray());

		WriteSegment(result, BytecodeSegment.Strings, []);

		byte[] code =
		[
			(byte)Opcode.OP_PARAMS,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			1,
			(byte)Opcode.OP_ARRAY,
			(byte)Opcode.OP_RET,
		];
		WriteSegment(result, BytecodeSegment.Bytecode, code);
		result.writeByte((byte)'\n');

		return result.toByteArray();
	}

	private static byte[] CreateFunctionBoundaryBytecode()
	{
		TString result = new();
		WriteSegment(result, BytecodeSegment.Gs1EventFlags, [0, 0, 0, 0]);

		TString functions = new();
		functions.writeInt(0);
		functions.writeCString("noReturn");
		functions.writeInt(3);
		functions.writeCString("nextFunction");
		WriteSegment(result, BytecodeSegment.FunctionNames, functions.toByteArray());

		TString strings = new();
		strings.writeCString("hit");
		WriteSegment(result, BytecodeSegment.Strings, strings.toByteArray());

		byte[] code =
		[
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			1,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			99,
			(byte)Opcode.OP_RET,
		];
		WriteSegment(result, BytecodeSegment.Bytecode, code);
		result.writeByte((byte)'\n');

		return result.toByteArray();
	}

	private static void WriteSegment(TString target, BytecodeSegment segment, IReadOnlyCollection<byte> bytes)
	{
		target.writeInt((int)segment);
		target.writeInt(bytes.Count);
		target.writeBytes(bytes);
	}

	private Script InitializePrebakedScript(string fileName) => new(_scriptManager, fileName);

	[Fact]
	public void When_script_is_faulty_Then_exception_is_thrown()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() 
						}
			""";


		//Act
		var result = Assert.Throws<Exception>(() => CompileScript(scriptText));;

		//Assert
		Assert.Equal("Script failure: malformed input at line 3: \t\t\t}\n", result.Message);
	}

	private void RegisterGlobalObject(string name, ScriptVariable collection) => _scriptManager.RegisterGlobalObject(name, collection);


	[Fact]
	public async Task When_calling_built_in_sin_Then_correct_sin_value_is_returned()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		var expectedSin = new List<double> { 1, 0, -1, 0 };
		var sin         = new List<double>();
		const string scriptText =
			"""
						//#CLIENTSIDE
						function test(dir) {
							temp.angle = (pi/2 * (dir+1));
							
							return sin(temp.angle);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		for (var i = 0; i < 4; i++) sin.Add((await script.Call("test", i)).GetValue<double>());

		//Assert
		for (var i = 0; i < 4; i++) Assert.Equal(expectedSin[i], sin[i]);
	}

	[Fact]
	public async Task When_calling_built_in_cos_Then_correct_cos_value_is_returned()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		var expectedCos = new List<double> { 0, -1, 0, 1 };
		var cos         = new List<double>();
		const string scriptText =
			"""
						//#CLIENTSIDE
						function test(dir) {
							temp.angle = (pi/2) * (dir+1);
							
							return cos(temp.angle);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		for (var i = 0; i < 4; i++) cos.Add((await script.Call("test", i)).GetValue<double>());

		//Assert
		for (var i = 0; i < 4; i++) Assert.Equal(expectedCos[i], cos[i]);
	}

	[Fact]
	public async Task Given_temp_var_When_returning_without_temp_prefix_Then_temp_var_should_be_returned()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.var = "test";
							
							temp.var2 = var;
							
							return var2;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("test", result.GetValue<TString>()!);
	}


	[Fact]
	public async Task Given_function_in_script2_When_calling_public_function_in_script1_Then_value_should_be_returned()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		const string scriptText1 =
			"""
						//#CLIENTSIDE
						public function PubFun() {
							temp.var = "PubFun";
							
							temp.var2 = var;
							
							return var2;
						}
			""";
			CompileScript(scriptText1, "script1");
			const string scriptText2 =
				"""
							//#CLIENTSIDE
							function onCreated() {
								return script1.PubFun();
							}
				""";
			var script2 = CompileScript(scriptText2);

			//Act
		var result = await script2.Call("onCreated");

		//Assert
		Assert.Equal("PubFun", result.GetValue()!.ToString());
	}

	[Fact]
	public async Task Given_function_in_script2_When_calling_public_function_with_argument_Then_argument_is_passed()
	{
		//Arrange
		const string scriptText1 =
			"""
						//#CLIENTSIDE
						public function PubFun(value) {
							return value;
						}
			""";
		CompileScript(scriptText1, "script1");
		const string scriptText2 =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return script1.PubFun("from-argument");
						}
			""";
		var script2 = CompileScript(scriptText2);

		//Act
		var result = await script2.Call("onCreated");

		//Assert
		Assert.Equal("from-argument", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_function_in_script2_When_calling_public_function_from_string_object_Then_value_should_be_returned()
	{
		//Arrange
		const string scriptText1 =
			"""
						//#CLIENTSIDE
						public function showOptions() {
							return "options-opened";
						}
			""";
		CompileScript(scriptText1, "-Serverlist_Options");
		const string scriptText2 =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return ("-Serverlist_Options").showOptions();
						}
			""";
		var script2 = CompileScript(scriptText2);

		//Act
		var result = await script2.Call("onCreated");

		//Assert
		Assert.Equal("options-opened", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_function_without_return_When_next_function_has_bytecode_Then_execution_stops_at_function_boundary()
	{
		//Arrange
		var script = new Script(_scriptManager, "boundaryScript", CreateFunctionBoundaryBytecode());

		//Act
		var result = await script.Call("noReturn");

		//Assert
		Assert.Equal(0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_script_join_When_class_is_joined_Then_class_script_is_requested()
	{
		//Arrange
		var requestedClasses = new List<string>();
		_scriptManager.SetClassScriptRequestHandler(requestedClasses.Add);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.join("joinedclass");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		Assert.Equal(["joinedclass"], requestedClasses);
	}

	[Fact]
	public async Task Given_script_joined_to_class_When_calling_class_function_Then_value_should_be_returned()
	{
		//Arrange
		const string classText =
			"""
						//#CLIENTSIDE
						function ClassValue() {
							return 42;
						}
			""";
		CompileScript(classText, "joinedclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.join("joinedclass");
							return this.ClassValue();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(42.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_script_joined_to_class_When_calling_class_function_without_this_Then_value_should_be_returned()
	{
		//Arrange
		const string classText =
			"""
						//#CLIENTSIDE
						function ClassValue() {
							return 42;
						}
			""";
		CompileScript(classText, "joinedclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.join("joinedclass");
							return ClassValue();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(42.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_script_joined_to_class_When_event_exists_only_in_class_Then_event_is_executed()
	{
		//Arrange
		const string classText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.marker = "from-class-event";
							return this.marker;
						}
			""";
		CompileScript(classText, "joinedclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function SomeOtherFunction() {
							return 0;
						}
			""";
		var script = CompileScript(scriptText);
		script.Join("joinedclass");

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("from-class-event", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_script_joined_to_class_When_event_exists_in_nested_class_Then_event_is_executed()
	{
		//Arrange
		const string nestedClassText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.marker = "from-nested-class-event";
							return this.marker;
						}
			""";
		CompileScript(nestedClassText, "nestedclass");
		const string joinedClassText =
			"""
						//#CLIENTSIDE
						function SomeClassFunction() {
							return 0;
						}
			""";
		var joinedClass = CompileScript(joinedClassText, "joinedclass");
		joinedClass.Join("nestedclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function SomeOtherFunction() {
							return 0;
						}
			""";
		var script = CompileScript(scriptText);
		script.Join("joinedclass");

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("from-nested-class-event", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_script_joined_to_class_When_class_function_writes_this_Then_joining_script_is_updated()
	{
		//Arrange
		const string classText =
			"""
						//#CLIENTSIDE
						function SetClassMarker() {
							this.marker = "from-class";
						}
			""";
		CompileScript(classText, "joinedclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.join("joinedclass");
							this.SetClassMarker();
							return this.marker;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("from-class", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_script_joined_to_class_When_class_function_has_argument_Then_argument_is_passed()
	{
		//Arrange
		const string classText =
			"""
						//#CLIENTSIDE
						function EchoClassValue(value) {
							return value;
						}
			""";
		CompileScript(classText, "joinedclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.join("joinedclass");
							return this.EchoClassValue("from-argument");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("from-argument", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_playero_When_member_read_Then_registered_playero_object_is_used()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		var playero = new ScriptVariable();
		playero.AddOrUpdate("account", "testaccount".ToStackEntry());
		RegisterGlobalObject("playero", playero);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return playero.account;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("testaccount", result.GetValue()!.ToString());
	}

	[Fact]
	public async Task Given_level_When_member_read_Then_registered_level_object_is_used()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		var level = new ScriptVariable();
		level.AddOrUpdate("name", "testlevel.nw".ToStackEntry());
		RegisterGlobalObject("level", level);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return level.name;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("testlevel.nw", result.GetValue()!.ToString());
	}

	[Fact]
	public async Task Given_this_var3_When_returning_without_this_prefix_Then_0_should_be_returned()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.var3 = "test2";
							
							return var3;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(0d, result.GetValue()!);
	}

	[Fact]
	public async Task Given_temp_var_When_creating_new_array_object_Then_result_should_be_empty_array_object()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		_scriptManager.GlobalVariables.Clear();
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.var = new[2];
							
							return temp.var == {0,0};
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue()!);
	}

	[Fact(Skip = "Waiting for fix in GS2Compiler")]
	public async Task Given_temp_update_When_comparing_multiple_or_and_one_variable_is_updated_Then_result_should_be_true()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		_scriptManager.GlobalVariables.Clear();
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.var1 = 30.5;
							temp.var2 = 30;
							temp.var3 = 2;
							temp.var4 = 0;
							temp.var5 = "myvar";
							this.oldData = {var1,var2,var3,var4,var5};
							var3 = -1;
							temp.update = var1 != this.oldData[0] ||
							    var2 != this.oldData[1] ||
							    (var3 == -1 && this.oldData[2] >=0);
							return temp.update;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(Convert.ToBoolean(result.GetValue()!));
	}

	[Fact(Skip = "Waiting for fix in GS2Compiler")]
	public async Task Given_temp_var_When_at_comparing_Then_result_should_be_true()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		_scriptManager.GlobalVariables.Clear();
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.var = {true,true};
							echo(@var);
							echo(""@{1,1});

							return @var == @{1,1};
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(true, result.GetValue()!);
	}

	[Fact]
	public async Task Given_temp_var3_is_array_object_When_comparing_values_with_another_array_object_with_identical_values_Then_result_should_be_true()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes   = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.var3 = {1,"asd",3};
							
							return temp.var3 == {1,"asd",3};
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue()!);
	}

	[Fact]
	public async Task Given_swedish_culture_When_number_is_joined_to_string_Then_decimal_separator_is_us_english()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
		var previousUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return "" @ 0.5;
						}
			""";
		var script = CompileScript(scriptText);

		try
		{
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");
			System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");

			//Act
			var result = await script.Call("onCreated");

			//Assert
			Assert.Equal("0.5", result.GetValue()?.ToString());
		}
		finally
		{
			System.Globalization.CultureInfo.CurrentCulture = previousCulture;
			System.Globalization.CultureInfo.CurrentUICulture = previousUiCulture;
		}
	}

	[Fact]
	public async Task Given_array_When_size_called_Then_returns_count()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 2, 3};
							return temp.values.size();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(3.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_index_called_Then_returns_matching_index()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 8, 3};
							return temp.values.index(8);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_type_called_Then_returns_array_type()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1};
							return temp.values.type();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(3.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_in_operator_called_Then_returns_true()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return 2 in {1, 2, 3};
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_number_When_in_range_called_Then_returns_true()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return 2 in |1, 3|;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_if_condition_When_value_is_non_one_nonzero_Then_branch_is_taken()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							if (2) return "true";
							return "false";
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("true", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_if_condition_When_value_is_zero_Then_branch_is_not_taken()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							if (0) return "true";
							return "false";
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("false", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_or_opcode_When_left_value_is_non_one_nonzero_Then_left_value_is_preserved()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			2,
			(byte)Opcode.OP_OR,
			0xF3,
			3,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			0,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("2", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_and_opcode_When_left_value_is_zero_Then_left_value_is_preserved()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			0,
			(byte)Opcode.OP_AND,
			0xF3,
			3,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			9,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_array_When_add_called_Then_appends_value()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 2};
							temp.values.add(3);
							return temp.values[2];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(3.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_delete_called_Then_removes_index()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 2, 3};
							temp.values.delete(1);
							return temp.values[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(3.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_insert_called_Then_inserts_value_at_index()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 3};
							temp.values.insert(1, 2);
							return temp.values[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(2.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_remove_called_Then_removes_first_matching_value()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 2, 3};
							temp.values.remove(2);
							return temp.values[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(3.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_replace_called_Then_replaces_value_at_index()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 2, 3};
							temp.values.replace(1, 8);
							return temp.values[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(8.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_subarray_called_Then_returns_requested_slice()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 8, 4};
							temp.tail = temp.values.subarray(1, 2);
							return temp.tail[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(4.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_cell_assigned_Then_returns_assigned_value()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1, 2, 3};
							temp.values[1] = 8;
							return temp.values[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(8.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_array_When_setarray_called_Then_resizes_array()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.values = {1};
							setarray(temp.values, 3);
							return temp.values.size();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(3.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_multidimensional_array_When_created_Then_nested_cell_defaults_to_zero()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.grid = new[2][3];
							return temp.grid[1, 2];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(0.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_multidimensional_array_When_cell_assigned_Then_returns_assigned_value()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.grid = new[2][3];
							temp.grid[1, 2] = 8;
							return temp.grid[1, 2];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(8.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_makevar_When_assigned_Then_target_variable_is_updated()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							makevar("this.dynamic") = 9;
							return this.dynamic;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(9.0d, result.GetValue());
	}

	[Fact]
	public async Task Given_registered_object_creator_When_new_object_called_Then_factory_creates_object()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		_scriptManager.RegisterObjectCreator(
			"TestCreatedObject",
			(id, _) =>
			{
				var created = new ScriptVariable(id);
				created.AddOrUpdate("kind", "registered".ToStackEntry());
				return created;
			}
		);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.created = new TestCreatedObject("test");
							return temp.created.kind;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("registered", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_new_object_name_operand_is_variable_When_variable_has_value_Then_creator_receives_operand_name()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		_scriptManager.RegisterObjectCreator(
			"TestCreatedObject",
			(id, _) =>
			{
				var created = new ScriptVariable(id);
				created.AddOrUpdate("id", id.ToStackEntry());
				return created;
			}
		);
		var script = CompileRawBytecodeScript(
			[
				(byte)Opcode.OP_TYPE_VAR,
				0xF0,
				0,
				(byte)Opcode.OP_TYPE_NUMBER,
				0xF6,
				(byte)'1',
				(byte)'.',
				(byte)'5',
				0,
				(byte)Opcode.OP_ASSIGN,
				(byte)Opcode.OP_TYPE_VAR,
				0xF0,
				0,
				(byte)Opcode.OP_TYPE_STRING,
				0xF0,
				1,
				(byte)Opcode.OP_NEW_OBJECT,
				(byte)Opcode.OP_TYPE_STRING,
				0xF0,
				2,
				(byte)Opcode.OP_MEMBER_ACCESS,
			],
			["temp.scale", "TestCreatedObject", "id"]
		);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("temp.scale", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_function_registered_twice_When_called_Then_latest_registration_is_used()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{ "duplicatefunctionregistration", "", (_, _) => "old" }
			}
		);
		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{ "duplicatefunctionregistration", "", (_, _) => "new" }
			}
		);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return duplicatefunctionregistration();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("new", result.GetValue()?.ToString());
	}

	[Fact]
	public void Given_child_script_properties_When_parent_has_same_property_Then_child_property_is_used()
	{
		//Arrange
		_ = new ParentPropertyMergeTestProperties();
		var properties = new ChildPropertyMergeTestProperties();
		properties.Compile();
		var instance = new ChildPropertyMergeTestObject();
		var property = properties.First(property => property.PropertyName == "value");

		//Act
		property.Write(instance, 7);

		//Assert
		Assert.Equal(0, instance.ParentValue);
		Assert.Equal(7, instance.ChildValue);
		Assert.Single(properties, prop => prop.PropertyName == "value");
	}

	[Fact]
	public async Task Given_gui_control_profile_When_font_size_is_set_Then_text_height_uses_font_size()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							profile = new GuiControlProfile("profile");
							profile.fontsize = 20;
							return profile.gettextheight();
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(20.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_profile_When_normal_bitmap_is_assigned_Then_registered_property_is_available()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								normalbitmap = "gui2001_button.png";
							}

							return profile.normalbitmap;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("gui2001_button.png", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_profile_When_bevel_highlight_color_is_assigned_Then_registered_property_is_available()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								bevelcolorhl = "255 255 255";
							}

							return profile.bevelcolorhl;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("255 255 255", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_profile_When_bevel_lowlight_color_is_assigned_Then_registered_property_is_available()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								bevelcolorll = "0 0 0";
							}

							return profile.bevelcolorll;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0 0 0", result.GetValue()?.ToString());
	}

	[Fact]
	public void Given_gui_control_profile_When_copied_Then_justify_is_preserved()
	{
		//Arrange
		var source = new GuiControlProfile("source")
		{
			Align = "left",
			Justify = "center",
		};
		var target = new GuiControlProfile("target");

		//Act
		target.CopyFrom(source);

		//Assert
		Assert.Equal("center", target.Justify);
	}

	[Fact]
	public void Given_gui_control_profile_When_copied_Then_bevel_highlight_color_is_preserved()
	{
		//Arrange
		var source = new GuiControlProfile("source")
		{
			BevelColorHl = "255 255 255",
		};
		var target = new GuiControlProfile("target");

		//Act
		target.CopyFrom(source);

		//Assert
		Assert.Equal("255 255 255", target.BevelColorHl);
	}

	[Fact]
	public void Given_gui_control_profile_When_copied_Then_bevel_lowlight_color_is_preserved()
	{
		//Arrange
		var source = new GuiControlProfile("source")
		{
			BevelColorLl = "0 0 0",
		};
		var target = new GuiControlProfile("target");

		//Act
		target.CopyFrom(source);

		//Assert
		Assert.Equal("0 0 0", target.BevelColorLl);
	}

	[Fact]
	public async Task Given_gui_control_profile_When_use_own_profile_copies_profile_Then_normal_bitmap_is_copied()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("buttonprofile") {
								normalbitmap = "gui2001_button.png";
								pressedbitmap = "gui2001_button_pressed.png";
							}

							new GuiControl("button") {
								profile = buttonprofile;
								useownprofile = true;
							}

							return button.profile.normalbitmap;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("gui2001_button.png", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_profile_is_assigned_by_string_Then_named_profile_is_resolved()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								transparency = 0.3;
							}

							new GuiControl("control") {
								profile = "profile";
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var control = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["control"].GetValue());
		Assert.Equal(0.3d, control.GetResolvedProfile()?.Transparency);
	}

	[Fact]
	public async Task Given_gui_control_When_use_own_profile_copies_string_profile_Then_profile_values_are_copied()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								fillcolor = { 0, 0, 0, 120 };
								transparency = 0.9;
							}

							new GuiControl("control") {
								profile = "profile";
								useownprofile = true;
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var control = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["control"].GetValue());
		Assert.Equal("0,0,0,120", control.GetResolvedProfile()?.FillColor);
		Assert.Equal(0.9d, control.GetResolvedProfile()?.Transparency);
	}

	[Fact]
	public async Task Given_gui_control_When_profile_is_assigned_after_use_own_profile_Then_own_profile_is_preserved()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl("control") {
								useownprofile = true;
								profile = MissingProfile;
								profile.fonttype = "friz";
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var control = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["control"].GetValue());
		Assert.Equal("friz", control.GetResolvedProfile()?.FontType);
	}

	[Fact]
	public async Task Given_gui_control_profile_When_justify_is_set_Then_align_matches()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								justify = "center";
							}

							return profile.align @ "|" @ profile.justify;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("center|center", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_profile_When_align_is_set_Then_justify_matches()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								align = "right";
							}

							return profile.align @ "|" @ profile.justify;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("right|right", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_chained_assignment_When_value_is_assigned_Then_each_target_gets_value()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							left = right = "value";

							return left @ "|" @ right;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("value|value", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_chained_member_assignment_When_value_is_assigned_Then_each_member_gets_value()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("profile") {
								normalbitmap = mouseoverbitmap = "gui2001_button.png";
							}

							return profile.normalbitmap @ "|" @ profile.mouseoverbitmap;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("gui2001_button.png|gui2001_button.png", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_old_object_from_string_bytecode_When_targeting_this_child_Then_script_variable_is_returned()
	{
		//Arrange
		var script = CompileLegacyBytecodeScript("this.legacychild");

		//Act
		var result = await script.Call("onCreated");

		//Assert
		var legacyChild = Assert.IsType<ScriptVariable>(result.GetValue());
		Assert.Equal("legacychild", legacyChild.Name);
		Assert.True(script.ContainsVariable("legacychild"));
		Assert.Same(legacyChild, script["legacychild"].GetValue<ScriptVariable>());
	}

	[Fact]
	public async Task Given_call_arguments_When_accessing_params_identifier_Then_indexed_value_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return params[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated", "left", "right");

		//Assert
		Assert.Equal("right", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_temp_member_parameter_When_called_Then_argument_is_bound_to_member()
	{
		// Arrange
		var script = CompileRawBytecodeScript(
			[
				(byte)Opcode.OP_TYPE_ARRAY,
				(byte)Opcode.OP_TEMP,
				(byte)Opcode.OP_UNKNOWN_234,
				0xF0,
				0,
				(byte)Opcode.OP_FUNC_PARAMS_END,
				(byte)Opcode.OP_TEMP,
				(byte)Opcode.OP_UNKNOWN_237,
				0xF0,
				0,
			],
			["gui"]
		);

		// Act
		var result = await script.Call("onCreated", "LoginCtrl_Container");

		// Assert
		Assert.Equal("LoginCtrl_Container", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_old_params_bytecode_When_accessing_array_index_Then_indexed_value_is_returned()
	{
		//Arrange
		var script = CompileLegacyParamsBytecodeScript();

		//Act
		var result = await script.Call("onCreated", "left", "right");

		//Assert
		Assert.Equal("right", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_ref_object_When_accessing_thiso_member_Then_ref_object_member_is_returned()
	{
		//Arrange
		var refObject = new ScriptVariable("refobject");
		refObject.AddOrUpdate("marker", "from-ref".ToStackEntry());
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return thiso.marker;
						}
			""";
		var script = CompileScript(scriptText, refObject: refObject);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("from-ref", result.GetValue()?.ToString());
	}

	[Fact(Skip = "fix later")]
	public async Task When_for_loop_with_items_Then_properly_for_loop_through_items()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.i = 0;
							temp.sounds = {
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
								"text_" @ temp.i++,
							};

							for (temp.sound : temp.sounds) {
								echo(temp.sound);
							}

							return "done!";
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = (await script.Call("onCreated")).GetValue<TString>();

		//Assert
		Assert.Equal("done!", result?.ToString());
		Assert.Equal(15, _calledTimes);
		Assert.Equal("text_11", _receivedStrings[3]);
		Assert.Equal("text_0", _receivedStrings[14]);
	}

	[Fact(Skip = "fix later")]
	public async Task When_for_loop_with_8_loops_Then_echo_is_called_8_times()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							for(this.i=0;this.i<8;this.i++) {
								echo(((this.i==6)?"test2":"test") @ "_text_" @ this.i);
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		_ = await script.Call("onCreated");

		//Assert
		Assert.Equal("test_text_3", _receivedStrings[3]);
		Assert.Equal("test2_text_6", _receivedStrings[6]);
	}

	[Fact(Skip = "fix later")]
	public async Task When_for_loop_with_8_loops_Then_echo_is_called_8_times_()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							with(test) {
								width = 12;
							};

							echo(test.width);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		_ = await script.Call("onCreated");

		//Assert
		Assert.Equal("12", _receivedStrings[0]);
	}

	[Fact]
	public async Task Given_gui_control_When_bounds_property_is_set_Then_bounds_reads_back_from_position_and_extent()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.bounds = "2 3 40 50";
							return test.bounds;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("2 3 40 50", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_width_property_is_set_below_one_Then_width_clamps_to_default_min_extent()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.width = 0;
							return test.width;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(8.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_When_resize_changes_extent_Then_onresize_is_called()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.resize = "";
							test = new GuiControl("test");
							test.resize(2, 3, 40, 50);
							return temp.resize;
						}

						function test.onResize(width, height) {
							temp.resize = width @ " " @ height;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("40 50", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_resize_changes_position_Then_onmove_is_called()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.move = "";
							test = new GuiControl("test");
							test.resize(2, 3, 40, 50);
							return temp.move;
						}

						function test.onMove(x, y) {
							temp.move = x @ " " @ y;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("2 3", result.GetValue()?.ToString());
	}

	[Fact]
	public void Given_registered_global_gui_control_When_set_size_changes_extent_Then_global_onresize_is_called()
	{
		//Arrange
		var control = new GuiControl("graalcontrol", new Script(_scriptManager, ScriptType.Weapon));
		_scriptManager.RegisterGlobalObject("graalcontrol", control);
		CompileScript(
			"""
						//#CLIENTSIDE
						function GraalControl.onResize(width, height) {
							resized = width @ " " @ height;
						}
			"""
		);

		//Act
		control.SetSize(300, 200);

		//Assert
		Assert.Equal("300 200", _scriptManager.GlobalVariables["resized"].GetValue()?.ToString());
	}

	[Fact]
	public void Given_registered_global_gui_control_When_notify_resize_is_called_Then_global_onresize_is_called()
	{
		//Arrange
		var control = new GuiControl("graalcontrol", new Script(_scriptManager, ScriptType.Weapon));
		_scriptManager.RegisterGlobalObject("graalcontrol", control);
		control.SetSize(300, 200);
		CompileScript(
			"""
						//#CLIENTSIDE
						function GraalControl.onResize(width, height) {
							resized = width @ " " @ height;
						}
			"""
		);

		//Act
		control.NotifyResize();

		//Assert
		Assert.Equal("300 200", _scriptManager.GlobalVariables["resized"].GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_temp_member_argument_inside_gui_with_block_When_member_name_exists_on_control_Then_temp_member_is_used()
	{
		//Arrange
		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{ "capturearg", "", (_, args) => args.Length > 0 ? args[0].GetValue()?.ToString() ?? string.Empty : string.Empty }
			}
		);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.text = "Nickname:";
							new GuiControl("label") {
								hint = capturearg(temp.text);
								text = _(temp.text);
							}

							return label.hint @ "," @ label.text;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("Nickname:,Nickname:", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_with_block_expression_When_current_property_is_referenced_Then_current_value_is_used()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl("parent") {
								height = 250;
								new GuiControl("child") {
									height = 26;
									y = (parent.height - height) - 50;
								}
							}

							return child.y;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(174.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_child_When_parent_extent_is_set_Then_child_width_sizing_uses_extent_delta()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							child = new GuiControl("child");
							parent.extent = "100 50";
							child.bounds = "10 5 20 10";
							child.horizsizing = "width";
							parent.addcontrol(child);
							parent.extent = "150 50";
							return child.width;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(70.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_When_position_uses_commas_Then_position_reads_back()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.position = "10,20";
							return test.position;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("10 20", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_area_click_priority_is_above_range_Then_value_is_clamped()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.areaclickpriority = 9;
							return test.areaclickpriority;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(2.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_When_local_to_global_coord_uses_parent_Then_offsets_are_applied()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							child = new GuiControl("child");
							parent.position = "10 20";
							child.position = "3 4";
							parent.addcontrol(child);
							return child.localtoglobalcoord("5 6");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("18,30", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_global_to_local_coord_uses_parent_Then_offsets_are_removed()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							child = new GuiControl("child");
							parent.position = "10 20";
							child.position = "3 4";
							parent.addcontrol(child);
							return child.globaltolocalcoord("18 30");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("5,6", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_global_function_with_incompatible_receiver_When_called_inside_with_control_Then_function_is_called()
	{
		//Arrange
		ScriptProperties<IAsyncDisposable>.AddFunctions(
			null,
			new()
			{
				{ "incompatibleglobalreceiver", "", (_, _) => "ok" }
			}
		);

		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							with (test) {
								return incompatibleglobalreceiver();
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("ok", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_nested_gui_initializer_inside_with_When_oncreated_runs_Then_controls_are_added_to_initializer_parents()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							root = new GuiControl("root");
							with (root) {
								new GuiControl("parent") {
									new GuiControl("child") {
									}
								}
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var root = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["root"].GetValue());
		var parent = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["parent"].GetValue());
		var child = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["child"].GetValue());
		Assert.Same(parent, Assert.Single(root.Controls));
		Assert.Same(child, Assert.Single(parent.Controls));
	}

	[Fact]
	public async Task Given_global_addcontrol_exists_When_nested_initializer_runs_inside_with_Then_with_parent_receives_child()
	{
		//Arrange
		var canvas = new GuiControl("graalcontrol", null!);
		_scriptManager.RegisterObjectCreator("GuiBitmapCtrl", (id, script) => new GuiBitmapCtrl(id, script));
		_scriptManager.RegisterObjectCreator("GuiTextCtrl", (id, script) => new GuiTextCtrl(id, script));
		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{
					"addcontrol",
					"",
					(_, args) =>
					{
						canvas.AddControl(args.FirstOrDefault()?.GetValue<GuiControl>());
						return 0;
					}
				}
			}
		);
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							root = new GuiControl("root");
							with (root) {
								new GuiBitmapCtrl("panel") {
									new GuiTextCtrl("label") {
									}
								}
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var root = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["root"].GetValue());
		var panel = Assert.IsType<GuiBitmapCtrl>(_scriptManager.GlobalVariables["panel"].GetValue());
		var label = Assert.IsType<GuiTextCtrl>(_scriptManager.GlobalVariables["label"].GetValue());
		Assert.Same(panel, Assert.Single(root.Controls));
		Assert.Same(label, Assert.Single(panel.Controls));
		Assert.Empty(canvas.Controls);
	}

	[Fact]
	public void Given_gui_control_showtop_When_called_Then_control_becomes_last_child()
	{
		//Arrange
		var root = new GuiControl("root", null!);
		var window = new GuiControl("window", null!);
		var sibling = new GuiControl("sibling", null!);
		root.AddControl(window);
		root.AddControl(sibling);

		//Act
		window.ShowTop();

		//Assert
		Assert.Same(window, root.Controls.Last());
	}

	[Fact]
	public void Given_gui_control_showtop_with_tab_child_When_called_Then_first_responder_is_propagated_to_root()
	{
		//Arrange
		var root = new GuiControl("root", null!);
		var window = new GuiControl("window", null!);
		var child = new GuiControl("child", null!)
		{
			Profile = new GuiControlProfile("childProfile") { Tab = true }
		};
		root.AddControl(window);
		window.AddControl(child);
		root.Awaken();

		//Act
		window.ShowTop();

		//Assert
		Assert.Same(child, root.FirstResponder);
	}

	[Fact]
	public async Task Given_gui_initializer_inside_with_When_named_control_property_is_read_Then_property_value_is_assigned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							container = new GuiControl("container");
							container.clientwidth = 320;
							container.clientheight = 240;
							with (container) {
								new GuiControl("screen") {
									width = container.clientwidth;
									height = container.clientheight;
								}
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var container = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["container"].GetValue());
		var screen = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["screen"].GetValue());
		Assert.Same(screen, Assert.Single(container.Controls));
		Assert.Equal(320, screen.Width);
		Assert.Equal(240, screen.Height);
	}

	[Fact]
	public async Task Given_named_gui_control_exists_When_created_again_inside_same_parent_Then_existing_control_is_reused()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							root = new GuiControl("root");
							with (root) {
								new GuiControl("container") {
									width = 100;
								}
								new GuiControl("container") {
									width = 200;
								}
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var root = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["root"].GetValue());
		var container = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["container"].GetValue());
		Assert.Same(container, Assert.Single(root.Controls));
		Assert.Equal(200, container.Width);
	}

	[Fact]
	public async Task Given_named_gui_control_is_destroyed_When_created_again_Then_new_active_control_is_created()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							root = new GuiControl("root");
							with (root) {
								new GuiControl("panel") {
									width = 100;
								}
							}
							panel.destroy();
							with (root) {
								new GuiControl("panel") {
									width = 200;
								}
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var root = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["root"].GetValue());
		var panel = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["panel"].GetValue());
		Assert.Same(panel, Assert.Single(root.Controls));
		Assert.True(panel.Active);
		Assert.Equal(200, panel.Width);
	}

	[Fact]
	public async Task Given_gui_expression_When_named_control_width_is_subtracted_Then_property_value_is_used()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							container = new GuiControl("container");
							container.clientwidth = 682;
							panel = new GuiControl("panel");
							panel.width = 500;
							panel.x = (container.clientwidth - panel.width) / 2;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var panel = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["panel"].GetValue());
		Assert.Equal(91, panel.X);
	}

	[Fact]
	public async Task Given_with_control_When_instance_function_is_called_Then_function_uses_with_target()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							child = new GuiControl("child");
							with (parent) {
								addcontrol(child);
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var parent = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["parent"].GetValue());
		var child = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["child"].GetValue());
		Assert.Single(parent.Controls);
		Assert.Same(child, parent.Controls.Single());
	}

	[Fact]
	public async Task Given_with_control_When_child_is_created_inside_with_Then_child_is_resolved_from_with_target()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							with (parent) {
								child = new GuiControl("child");
								addcontrol(child);
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var parent = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["parent"].GetValue());
		var child = Assert.IsType<GuiControl>(parent.GetVariable("child").GetValue());
		Assert.Single(parent.Controls);
		Assert.Same(child, parent.Controls.Single());
	}

	[Fact]
	public async Task Given_object_created_inside_with_When_scope_ends_Then_object_name_is_global()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							with (parent) {
								child = new GuiControl("child");
							}
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var parent = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["parent"].GetValue());
		var child = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["child"].GetValue());
		Assert.Same(child, parent.GetVariable("child").GetValue());
	}

	[Fact]
	public async Task Given_joined_gui_class_When_unqualified_addcontrol_is_called_Then_child_is_added_to_receiver()
	{
		//Arrange
		var canvas = new GuiControl("graalcontrol", null!);
		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{
					"addcontrol",
					"",
					(_, args) =>
					{
						canvas.AddControl(args.FirstOrDefault()?.GetValue<GuiControl>());
						return 0;
					}
				}
			}
		);
		const string classText =
			"""
						//#CLIENTSIDE
						public function addChild(childControl) {
							addcontrol(childControl);
						}
			""";
		CompileScript(classText, "guicontrolclass");
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							child = new GuiControl("child");
							parent.join("guicontrolclass");
							parent.addChild(child);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		await script.Call("onCreated");

		//Assert
		var parent = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["parent"].GetValue());
		var child = Assert.IsType<GuiControl>(_scriptManager.GlobalVariables["child"].GetValue());
		var childControl = Assert.Single(parent.Controls);
		Assert.Same(child, childControl);
		Assert.Empty(canvas.Controls);
	}

	[Fact]
	public async Task Given_array_assigned_to_string_property_When_property_is_written_Then_array_is_script_string()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							control = new GuiControl("control");
							control.position = {12, 34};
							return control.position;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("12 34", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_global_properties_in_array_literal_When_assigned_to_extent_Then_property_values_are_used()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							control = new GuiControl("control");
							control.extent = {screenwidth, screenheight};
							return control.extent;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1024 1024", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_client_width_global_property_When_assigning_control_client_width_Then_control_property_is_written()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							control = new GuiControl("control");
							control.clientwidth = screenwidth;
							control.clientheight = screenheight;
							return control.extent;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1024 1024", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_client_width_in_constructor_block_When_assigned_from_global_property_Then_control_property_is_written()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl("control") {
								clientwidth = screenwidth;
								clientheight = screenheight;
							}
							return control.extent;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1024 1024", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_in_parent_When_maximized_is_true_Then_control_matches_parent_extent()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							parent.extent = {300, 200};
							with (parent) {
								new GuiControl("child") {
									maximized = true;
								}
							}
							return child.bounds;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0 0 300 200", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_in_parent_When_maximized_is_false_Then_control_keeps_default_extent()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							parent = new GuiControl("parent");
							parent.extent = {300, 200};
							with (parent) {
								new GuiControl("child") {
									maximized = false;
								}
							}
							return child.bounds;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0 0 64 64", result.GetValue()?.ToString());
	}

	[Fact]
	public void Given_gui_control_When_controls_are_added_Then_children_keep_insertion_order()
	{
		//Arrange
		var parent = new GuiControl("parent", null!);
		var first = new GuiControl("first", null!);
		var second = new GuiControl("second", null!);

		//Act
		parent.AddControl(first);
		parent.AddControl(second);

		//Assert
		Assert.Equal(new[] { first, second }, parent.Controls.OfType<GuiControl>().ToArray());
	}

	[Fact]
	public void Given_gui_control_When_sort_controls_is_called_Then_children_are_ordered_by_position()
	{
		//Arrange
		var parent = new GuiControl("parent", null!);
		var second = new GuiControl("second", null!) { X = 20, Y = 10 };
		var first = new GuiControl("first", null!) { X = 10, Y = 10 };
		parent.AddControl(second);
		parent.AddControl(first);

		//Act
		parent.SortControls();

		//Assert
		Assert.Same(first, parent.Controls.OfType<GuiControl>().First());
	}

	[Fact]
	public async Task Given_global_property_When_used_as_function_argument_Then_property_value_is_passed()
	{
		//Arrange
		ScriptProperties<ScriptMachineTests>.AddProperties(
			null,
			new()
			{
				{ "testglobalargumentproperty", "", _ => 3.14d }
			}
		);
		ScriptProperties<ScriptMachineTests>.AddFunctions(
			null,
			new()
			{
				{ "readargument", "", (_, args) => args[0].GetValue()?.ToString() ?? string.Empty }
			}
		);

		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return readargument(testglobalargumentproperty);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("3.14", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_global_string_property_When_compared_equal_Then_property_value_is_used()
	{
		//Arrange
		var propertyName = $"globalcomparep{Guid.NewGuid():N}";
		ScriptProperties<ScriptMachineTests>.AddProperties(
			null,
			new()
			{
				{ propertyName, "", _ => "ready" }
			}
		);
		var script = CompileScript(
			$$"""
			//#CLIENTSIDE
			function onCreated() {
				return {{propertyName}} == "ready";
			}
			"""
		);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_empty_global_string_property_When_compared_not_equal_empty_Then_false_is_returned()
	{
		//Arrange
		var propertyName = $"globalcomparep{Guid.NewGuid():N}";
		ScriptProperties<ScriptMachineTests>.AddProperties(
			null,
			new()
			{
				{ propertyName, "", _ => string.Empty }
			}
		);
		var script = CompileScript(
			$$"""
			//#CLIENTSIDE
			function onCreated() {
				return {{propertyName}} != "";
			}
			"""
		);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(0.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_global_string_property_When_assigned_Then_property_setter_is_called()
	{
		//Arrange
		var propertyName = $"$pref::unit::p{Guid.NewGuid():N}";
		var storedValue = "en";
		ScriptProperties<ScriptMachineTests>.AddProperties(
			null,
			new()
			{
				{ propertyName, "", _ => storedValue, (_, value) => storedValue = value }
			}
		);
		var script = CompileScript(
			$$"""
			//#CLIENTSIDE
			function onCreated() {
				{{propertyName}} = "sv";
				return {{propertyName}};
			}
			"""
		);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("sv", result.GetValue<string>());
		Assert.Equal("sv", storedValue);
	}

	[Fact]
	public async Task Given_global_bool_property_When_assigned_Then_property_setter_is_called()
	{
		//Arrange
		var propertyName = $"$pref::unit::p{Guid.NewGuid():N}";
		var storedValue = false;
		ScriptProperties<ScriptMachineTests>.AddProperties(
			null,
			new()
			{
				{ propertyName, "", _ => storedValue, (_, value) => storedValue = value }
			}
		);
		var script = CompileScript(
			$$"""
			//#CLIENTSIDE
			function onCreated() {
				{{propertyName}} = 1;
				return {{propertyName}};
			}
			"""
		);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(1.0d, result.GetValue<double>());
		Assert.True(storedValue);
	}

	[Fact]
	public async Task Given_gui_control_When_hide_is_called_Then_visible_is_false()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.hide();
							return test.visible;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(0.0d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_When_color_uses_decimal_points_Then_color_reads_back_with_decimal_points()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.color = "0.5 0.25 1 0.75";
							return test.color;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0.5 0.25 1 0.75", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_createanimation_is_called_Then_animation_properties_are_script_accessible()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							temp.animation = test.createanimation();
							temp.animation.alpha = 0.5;
							return temp.animation.alpha;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(0.5d, result.GetValue<double>());
	}

	[Fact]
	public async Task Given_gui_control_When_visible_is_set_false_Then_inout_animation_is_stopped()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							temp.animation = test.createanimation();
							temp.animation.transition = "in";
							test.visible = false;
							return test.isinanimation;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(0.0d, result.GetValue<double>());
	}

	[Fact]
	public void Given_gui_control_When_createanimation_is_called_Then_control_is_visible_and_needs_repaint()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!) { Visible = false };

		//Act
		var animation = control.CreateAnimation();

		//Assert
		Assert.NotNull(animation);
		Assert.True(control.Visible);
		Assert.True(control.NeedsRepaint);
		Assert.True(control.IsInAnimation);
	}

	[Fact]
	public void Given_gui_animation_When_bounds_are_not_set_Then_owner_bounds_are_returned()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!) { Bounds = "2 3 40 50" };
		var animation = new TGUIAnimation(control);

		//Assert
		Assert.Equal("2 3 40 50", animation.Bounds);
	}

	[Fact]
	public void Given_gui_control_When_stopanimations_is_called_Then_animation_state_is_cleared()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!);
		control.CreateAnimation();

		//Act
		control.StopAnimations();

		//Assert
		Assert.Empty(control.Animations);
		Assert.False(control.IsInAnimation);
	}

	[Fact(Skip = "fix later")]
	public async Task When_for_loop_with_8_loops_Then_echo_is_called_8_times__()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							echo(""@screenwidth);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		_ = await script.Call("onCreated");

		//Assert
		Assert.Equal("1024", _receivedStrings[0]);
	}

	[Fact]
	public async Task Given_object_link_When_assigning_linked_variable_Then_original_variable_is_updated()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.items = {"a"};
							temp.ref = this.items.link();
							temp.ref = {"b"};
							return this.items[0];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("b", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_translate_opcode_When_no_translation_exists_Then_original_string_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.text = "Connect";
							return _(temp.text);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("Connect", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_foreach_loop_When_loop_variable_is_assigned_Then_current_array_cell_is_updated()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							this.items = {"a", "b"};
							for (temp.item: this.items) {
								temp.item = temp.item @ "_x";
							}
							return this.items[1];
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("b_x", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_with_statement_When_target_object_is_missing_Then_body_is_skipped()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							with ("missing_object") {
								return "inside";
							}
							return "outside";
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("outside", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_waitfor_opcode_When_event_waiting_is_unavailable_Then_zero_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							temp.done = waitfor(this, "Done", 1);
							return temp.done;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_dynamic_add_opcode_When_operands_are_numbers_Then_sum_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_DYNAMIC_ADD,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("11", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_dynamic_add_opcode_When_operand_is_string_Then_values_are_concatenated()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_DYNAMIC_ADD,
		], ["item"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("item7", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_object_compare_opcode_When_left_number_is_less_than_right_Then_minus_one_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_OBJ_COMPARE,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("-1", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_object_compare_opcode_When_strings_differ_only_by_case_Then_zero_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			1,
			(byte)Opcode.OP_OBJ_COMPARE,
		], ["Test", "test"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_equality_operator_When_string_contains_same_number_Then_values_are_equal()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return "4" == 4;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_equality_operator_When_string_contains_different_number_Then_values_are_not_equal()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return "4" == 5;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_less_than_operator_When_string_contains_smaller_number_Then_result_is_true()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return "4" < 5;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_less_than_operator_When_string_contains_larger_number_Then_result_is_false()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return "6" < 5;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_greater_than_opcode_When_left_string_is_lexically_larger_Then_result_is_true()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			1,
			(byte)Opcode.OP_GT,
		], ["beta", "Alpha"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_less_than_opcode_When_left_string_is_lexically_larger_Then_result_is_false()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			1,
			(byte)Opcode.OP_LT,
		], ["beta", "Alpha"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_min_function_When_operands_are_strings_Then_lexically_smaller_string_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			1,
			(byte)Opcode.OP_MIN,
		], ["beta", "Alpha"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("Alpha", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_max_function_When_operands_are_strings_Then_lexically_larger_string_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			1,
			(byte)Opcode.OP_MAX,
		], ["beta", "Alpha"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("beta", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_optimized_immediate_add_opcode_When_executed_Then_sum_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_UNKNOWN_200,
			0xF3,
			7,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("11", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_optimized_stack_and_opcode_When_right_operand_is_zero_Then_result_is_false()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_TRUE,
			(byte)Opcode.OP_TYPE_FALSE,
			(byte)Opcode.OP_UNKNOWN_66,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_optimized_immediate_or_opcode_When_left_is_zero_and_right_is_one_Then_result_is_true()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_FALSE,
			(byte)Opcode.OP_UNKNOWN_207,
			0xF3,
			1,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("1", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_optimized_immediate_less_than_opcode_When_left_is_greater_Then_result_is_false()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_UNKNOWN_224,
			0xF3,
			4,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_number_is_followed_by_add_Then_optimized_immediate_add_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_ADD,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_200, script.Bytecode[1].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[2].OpCode);
		Assert.Equal("11", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_number_is_followed_by_less_than_Then_optimized_comparison_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_LT,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_224, script.Bytecode[1].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[2].OpCode);
		Assert.Equal("0", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_immediate_add_is_assigned_Then_optimized_immediate_assignment_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_ADD,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
		], ["counter"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_216, script.Bytecode[5].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[6].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[7].OpCode);
		Assert.Equal("11", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_stack_add_is_assigned_Then_optimized_stack_assignment_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			7,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_ADD,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
		], ["counter", "amount"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_208, script.Bytecode[9].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[10].OpCode);
		Assert.Equal("11", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_register_opcodes_When_value_is_saved_and_loaded_Then_loaded_value_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_UNKNOWN_45,
			0xF3,
			0,
			(byte)Opcode.OP_INDEX_DEC,
			(byte)Opcode.OP_UNKNOWN_46,
			0xF3,
			0,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("4", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_register_float_conversion_opcode_When_string_number_is_loaded_Then_number_is_returned()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_STRING,
			0xF0,
			0,
			(byte)Opcode.OP_UNKNOWN_45,
			0xF3,
			0,
			(byte)Opcode.OP_INDEX_DEC,
			(byte)Opcode.OP_UNKNOWN_228,
			0xF3,
			0,
		], ["4.5"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("4.5", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_registered_variable_is_incremented_Then_optimized_increment_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_UNKNOWN_45,
			0xF3,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_UNKNOWN_46,
			0xF3,
			0,
			(byte)Opcode.OP_INC,
			(byte)Opcode.OP_INDEX_DEC,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
		], ["counter"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_231, script.Bytecode[4].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[5].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[6].OpCode);
		Assert.Equal("5", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_registered_variable_is_decremented_Then_optimized_decrement_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_UNKNOWN_45,
			0xF3,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			4,
			(byte)Opcode.OP_ASSIGN,
			(byte)Opcode.OP_UNKNOWN_46,
			0xF3,
			0,
			(byte)Opcode.OP_DEC,
			(byte)Opcode.OP_INDEX_DEC,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
		], ["counter"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_232, script.Bytecode[4].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[5].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[6].OpCode);
		Assert.Equal("3", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_registered_value_is_copied_Then_optimized_copy_is_used()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			6,
			(byte)Opcode.OP_UNKNOWN_45,
			0xF3,
			0,
			(byte)Opcode.OP_INDEX_DEC,
			(byte)Opcode.OP_UNKNOWN_46,
			0xF3,
			0,
			(byte)Opcode.OP_COPY_LAST_OP,
			(byte)Opcode.OP_ADD,
		]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_233, script.Bytecode[3].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[4].OpCode);
		Assert.Equal("12", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_variable_member_is_read_Then_cached_member_opcode_is_used()
	{
		//Arrange
		var obj = new ScriptVariable();
		obj.AddOrUpdate("value", "ok".ToStackEntry());
		RegisterGlobalObject("obj", obj);
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_MEMBER_ACCESS,
		], ["obj", "value"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_234, script.Bytecode[1].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[2].OpCode);
		Assert.Equal("ok", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_missing_variable_member_is_read_Then_cached_member_returns_default_value()
	{
		//Arrange
		var obj = new ScriptVariable();
		RegisterGlobalObject("obj", obj);
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_MEMBER_ACCESS,
		], ["obj", "missing"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_234, script.Bytecode[1].OpCode);
		Assert.Equal("", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_variable_member_is_converted_to_string_Then_cached_string_member_opcode_is_used()
	{
		//Arrange
		var obj = new ScriptVariable();
		obj.AddOrUpdate("value", 4.ToStackEntry());
		RegisterGlobalObject("obj", obj);
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_MEMBER_ACCESS,
			(byte)Opcode.OP_CONV_TO_STRING,
		], ["obj", "value"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_237, script.Bytecode[1].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[2].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[3].OpCode);
		Assert.Equal("4", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_variable_is_converted_to_object_Then_cached_object_opcode_is_used()
	{
		//Arrange
		var obj = new ScriptVariable();
		obj.AddOrUpdate("value", "ok".ToStackEntry());
		RegisterGlobalObject("obj", obj);
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_CONV_TO_OBJECT,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_MEMBER_ACCESS,
		], ["obj", "value"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_235, script.Bytecode[0].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[1].OpCode);
		Assert.Equal("ok", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_bytecode_optimizer_When_number_is_followed_by_array_access_Then_optimized_immediate_array_access_is_used()
	{
		//Arrange
		_scriptManager.GlobalVariables.AddOrUpdate("values", new List<object?> { 1.0d, 8.0d }.ToStackEntry());
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			1,
			(byte)Opcode.OP_ARRAY,
		], ["values"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal(Opcode.OP_UNKNOWN_240, script.Bytecode[1].OpCode);
		Assert.Equal(Opcode.OP_NONE, script.Bytecode[2].OpCode);
		Assert.Equal("8", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_member_assignment_opcode_When_assigning_object_member_Then_member_is_updated()
	{
		//Arrange
		var obj = new ScriptVariable();
		RegisterGlobalObject("obj", obj);
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			9,
			(byte)Opcode.OP_UNKNOWN_54,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			0,
			(byte)Opcode.OP_TYPE_VAR,
			0xF0,
			1,
			(byte)Opcode.OP_MEMBER_ACCESS,
		], ["obj", "value"]);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("9", result.GetValue()?.ToString());
	}

	[Fact]
	public void Given_fix_bad_bytecode_When_branch_target_is_past_end_Then_target_is_clamped_to_bytecode_length()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_IF,
			0xF3,
			99,
		]);

		//Assert
		Assert.Equal(script.Bytecode.Length, script.Bytecode[0].Value);
	}

	[Fact]
	public void Given_fix_bad_bytecode_When_branch_target_is_negative_Then_target_is_clamped_to_bytecode_length()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_IF,
			0xF3,
			unchecked((byte)-1),
		]);

		//Assert
		Assert.Equal(script.Bytecode.Length, script.Bytecode[0].Value);
	}

	[Fact]
	public void Given_check_only_functions_When_first_opcode_jumps_to_end_Then_has_only_functions_is_true()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_SET_INDEX,
			0xF3,
			3,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			1,
		]);

		//Assert
		Assert.True(script.HasOnlyFunctions);
	}

	[Fact]
	public void Given_check_only_functions_When_first_opcode_jumps_before_end_Then_has_only_functions_is_false()
	{
		//Arrange
		var script = CompileRawBytecodeScript([
			(byte)Opcode.OP_SET_INDEX,
			0xF3,
			1,
			(byte)Opcode.OP_TYPE_NUMBER,
			0xF3,
			1,
		]);

		//Assert
		Assert.False(script.HasOnlyFunctions);
	}

	[Fact]
	public async Task Given_function_only_script_When_function_is_called_first_time_Then_function_body_runs()
	{
		//Arrange
		var script = CompileScript(
			"""
			function onCreated() {
				this.called = true;
			}
			"""
		);

		//Act
		await script.Call("onCreated");

		//Assert
		Assert.True(script.GetVariable("called").GetValue<bool>());
	}

	[Fact]
	public async Task Given_gui_control_event_helper_When_action_is_triggered_Then_script_callback_runs()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return 0;
						}

						function ctrl.onAction() {
							temp.value = 5;
						}

						function readValue() {
							return temp.value;
						}
			""";
		var script = CompileScript(scriptText);
		var control = new TestEventGuiControl("ctrl", script);

		//Act
		control.TriggerAction();
		var result = await script.Call("readValue");

		//Assert
		Assert.Equal(5.0d, result.GetValue<double>());
	}

	[Fact]
	public void Given_gui_control_When_repaint_is_called_Then_repaint_state_is_set()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!);

		//Act
		control.Repaint();

		//Assert
		Assert.True(control.NeedsRepaint);
	}

	[Fact]
	public void Given_gui_control_When_startdrag_is_called_Then_drag_state_is_set()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!);

		//Act
		control.StartDrag();

		//Assert
		Assert.True(control.IsDragging);
	}

	[Fact]
	public void Given_gui_control_profile_When_preloadfont_is_called_Then_preloaded_state_is_set()
	{
		//Arrange
		var profile = new GuiControlProfile("profile");

		//Act
		profile.PreloadFont();

		//Assert
		Assert.True(profile.FontPreloaded);
	}

	[Fact]
	public void Given_gui_control_profile_When_created_Then_transparency_defaults_to_one()
	{
		//Arrange
		var profile = new GuiControlProfile("profile");

		//Assert
		Assert.Equal(1d, profile.Transparency);
	}

	[Fact]
	public void Given_unknown_profile_constructor_When_type_name_ends_with_profile_Then_profile_is_created()
	{
		//Arrange
		var script = new Script(_scriptManager, ScriptType.Weapon);

		//Act
		var created = _scriptManager.TryCreateObject("GuiBlueButtonProfile", "buttonprofile", script, out var createdObject);

		//Assert
		Assert.True(created);
		Assert.IsType<GuiControlProfile>(createdObject);
		Assert.True(_scriptManager.GlobalVariables.ContainsVariable("buttonprofile"));
	}

	[Fact]
	public void Given_profile_constructor_uses_existing_profile_name_When_created_Then_profile_values_are_copied()
	{
		//Arrange
		var script = new Script(_scriptManager, ScriptType.Weapon);
		_scriptManager.RegisterGlobalObject("baseprofile", new GuiControlProfile("baseprofile")
		{
			FontSize = 22,
			FillColor = "1 2 3"
		});

		//Act
		var created = _scriptManager.TryCreateObject("BaseProfile", "copyprofile", script, out var createdObject);

		//Assert
		Assert.True(created);
		var profile = Assert.IsType<GuiControlProfile>(createdObject);
		Assert.Equal(22, profile.FontSize);
		Assert.Equal("1 2 3", profile.FillColor);
	}

	[Fact]
	public async Task Given_gui_control_profile_constructor_When_default_profile_exists_Then_default_values_are_copied()
	{
		//Arrange
		const string scriptText =
			"""
			function onCreated() {
				new GuiControlProfile("GuiDefaultProfile") {
					fontType = "Arial";
					fontColor = "255 224 160";
				}

				new GuiControlProfile("childprofile") {
					fontSize = 12;
				}

				return childprofile.fonttype @ "," @ childprofile.fontcolor @ "," @ childprofile.fontsize;
			}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("Arial,255 224 160,12", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_gui_control_When_useownprofile_is_true_Then_profile_member_assignment_is_applied()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");
							test.useownprofile = true;
							test.profile.opaque = true;

							return test.profile.opaque;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_gui_control_constructor_block_When_profile_member_is_assigned_Then_own_profile_is_updated()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl("test") {
								useownprofile = true;
								profile.opaque = true;
							}

							return test.profile.opaque;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_gui_control_When_useownprofile_is_enabled_after_shared_profile_Then_profile_is_copied()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControlProfile("baseprofile") {
								border = 3;
							}

							new GuiControl("test") {
								profile = baseprofile;
								useownprofile = true;
								profile.border = 7;
							}

							return baseprofile.border @ "," @ test.profile.border;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.Equal("3,7", result.GetValue()?.ToString());
	}

	[Fact]
	public async Task Given_isobject_When_object_exists_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");

							return isObject("test");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_isobject_When_object_variable_exists_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl("test");

							return isObject(test);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_isobject_When_mixed_case_object_variable_exists_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							GR_LoginScreen = new GuiControl("GR_LoginScreen");

							return isObject(GR_LoginScreen);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_gui_control_constructor_block_When_parent_is_read_Then_new_root_control_has_null_parent()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl("test") {
								temp.result = parent == null;
							}

							return temp.result;
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public void Given_visible_gui_control_When_awakened_Then_onshow_is_called()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function ctrl.onShow() {
							shown = true;
						}
			""";
		var script = CompileScript(scriptText);
		var control = new GuiControl("ctrl", script);

		//Act
		control.Awaken();

		//Assert
		Assert.True(_scriptManager.GlobalVariables["shown"].GetValue<bool>());
	}

	[Fact]
	public void Given_hidden_gui_control_When_awakened_Then_onshow_is_not_called()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function ctrl.onShow() {
							shown = true;
						}
			""";
		var script = CompileScript(scriptText);
		var control = new GuiControl("ctrl", script) { Visible = false };

		//Act
		control.Awaken();

		//Assert
		Assert.False(_scriptManager.GlobalVariables.ContainsVariable("shown"));
	}

	[Fact]
	public void Given_gui_control_When_created_Then_native_clipping_defaults_are_used()
	{
		//Arrange
		var control = new GuiControl("test", null);

		//Assert
		Assert.True(control.ClipChildren);
		Assert.True(control.ClipMove);
		Assert.True(control.ClipToBounds);
	}

	[Fact]
	public void Given_gui_control_When_created_Then_native_move_resize_defaults_are_used()
	{
		//Arrange
		var control = new GuiControl("test", null);

		//Assert
		Assert.False(control.CanMove);
		Assert.False(control.CanResize);
	}

	[Fact]
	public async Task Given_new_gui_control_with_string_without_assignment_When_isobject_variable_is_called_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl("test");

							return isObject(test);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_new_gui_control_with_variable_without_assignment_When_isobject_variable_is_called_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							new GuiControl(test);

							return isObject(test);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_new_gui_control_without_name_assigned_to_variable_When_isobject_variable_is_called_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl();

							return isObject(test);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_new_gui_control_without_name_assigned_to_variable_When_isobject_string_is_called_Then_true_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							test = new GuiControl();

							return isObject("test");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.True(result.GetValue<bool>());
	}

	[Fact]
	public async Task Given_isobject_When_object_is_missing_Then_false_is_returned()
	{
		//Arrange
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							return isObject("missing");
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		var result = await script.Call("onCreated");

		//Assert
		Assert.False(result.GetValue<bool>());
	}

	[Fact]
	public void Given_gui_control_When_useownprofile_is_false_Then_profile_is_not_created()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!);

		//Assert
		Assert.False(control.UseOwnProfile);
		Assert.Null(control.Profile);
	}

	[Fact]
	public void Given_gui_control_When_child_is_inactive_or_invisible_Then_draw_skips_child()
	{
		//Arrange
		var parent = new GuiControl("parent", null!);
		var visible = new CountingGuiControl("visible", null!);
		var invisible = new CountingGuiControl("invisible", null!) { Visible = false };
		var inactive = new CountingGuiControl("inactive", null!) { Active = false };
		parent.AddControl(visible);
		parent.AddControl(invisible);
		parent.AddControl(inactive);

		//Act
		parent.Draw();

		//Assert
		Assert.Equal(1, visible.DrawCount);
		Assert.Equal(0, invisible.DrawCount);
		Assert.Equal(0, inactive.DrawCount);
	}

	[Fact]
	public void Given_gui_control_When_adding_itself_Then_control_is_not_added()
	{
		//Arrange
		var control = new GuiControl("control", null!);

		//Act
		control.AddControl(control);

		//Assert
		Assert.Empty(control.Controls);
		Assert.Null(control.Parent);
	}

	[Fact]
	public void Given_gui_control_When_adding_ancestor_Then_control_is_not_added()
	{
		//Arrange
		var parent = new GuiControl("parent", null!);
		var child = new GuiControl("child", null!);
		parent.AddControl(child);

		//Act
		child.AddControl(parent);

		//Assert
		Assert.Empty(child.Controls);
		Assert.Same(parent, child.Parent);
		Assert.Null(parent.Parent);
	}

	[Fact]
	public void Given_gui_control_When_parent_is_set_to_descendant_Then_parent_is_not_changed()
	{
		//Arrange
		var parent = new GuiControl("parent", null!);
		var child = new GuiControl("child", null!);
		parent.AddControl(child);

		//Act
		parent.Parent = child;

		//Assert
		Assert.Null(parent.Parent);
		Assert.Same(parent, child.Parent);
	}

	[Fact]
	public void Given_gui_control_When_reparented_Then_old_parent_no_longer_contains_child()
	{
		//Arrange
		var oldParent = new GuiControl("oldparent", null!);
		var newParent = new GuiControl("newparent", null!);
		var child = new GuiControl("child", null!);
		oldParent.AddControl(child);

		//Act
		newParent.AddControl(child);

		//Assert
		Assert.DoesNotContain(child, oldParent.Controls);
		Assert.Contains(child, newParent.Controls);
		Assert.Same(newParent, child.Parent);
	}

	[Fact]
	public void Given_gui_control_When_child_adds_control_during_draw_Then_new_control_is_drawn_next_frame()
	{
		//Arrange
		var parent = new GuiControl("parent", null!);
		var lateChild = new CountingGuiControl("latechild", null!);
		var mutatingChild = new MutatingGuiControl("mutatingchild", null!, parent, lateChild);
		parent.AddControl(mutatingChild);

		//Act
		parent.Draw();

		//Assert
		Assert.Equal(0, lateChild.DrawCount);

		//Act
		parent.Draw();

		//Assert
		Assert.Equal(1, lateChild.DrawCount);
	}

	[Fact]
	public void Given_gui_control_When_constructed_Then_default_sizing_matches_cpp_defaults()
	{
		//Arrange
		var control = new GuiControl("ctrl", null!);

		//Assert
		Assert.Equal("right", control.HorizSizing);
		Assert.Equal("bottom", control.VertSizing);
	}

	[Fact]
	public void Given_gui_control_child_When_parent_resizes_and_child_width_sizing_Then_child_width_grows_by_delta()
	{
		//Arrange
		var parent = new GuiControl("parent", null!) { Width = 100, Height = 50 };
		var child = new GuiControl("child", null!) { X = 10, Y = 5, Width = 20, Height = 10, HorizSizing = "width" };
		parent.AddControl(child);

		//Act
		parent.Resize(0, 0, 150, 50);

		//Assert
		Assert.Equal(70, child.Width);
	}

	[Fact]
	public void Given_gui_control_child_When_parent_set_size_and_child_width_sizing_Then_child_width_grows_by_delta()
	{
		//Arrange
		var parent = new GuiControl("parent", null!) { Width = 100, Height = 50 };
		var child = new GuiControl("child", null!) { X = 10, Y = 5, Width = 20, Height = 10, HorizSizing = "width" };
		parent.AddControl(child);

		//Act
		parent.SetSize(150, 50);

		//Assert
		Assert.Equal(70, child.Width);
	}

	[Fact]
	public void Given_gui_control_When_set_size_is_called_Then_size_changes_without_moving_control()
	{
		//Arrange
		var control = new GuiControl("control", null!) { X = 3, Y = 4, Width = 1, Height = 1 };

		//Act
		control.SetSize(640, 480);

		//Assert
		Assert.Equal(3, control.X);
		Assert.Equal(4, control.Y);
		Assert.Equal(640, control.Width);
		Assert.Equal(480, control.Height);
	}

	[Fact]
	public void Given_gui_control_When_min_size_is_set_Then_min_extent_reads_same_value()
	{
		//Arrange
		var control = new GuiControl("control", null!);

		//Act
		control.MinSize = "12 13";

		//Assert
		Assert.Equal("12 13", control.MinExtent);
	}

	[Fact]
	public void Given_gui_control_When_set_size_is_below_min_extent_Then_size_is_clamped()
	{
		//Arrange
		var control = new GuiControl("control", null!) { MinExtent = "12 13" };

		//Act
		control.SetSize(1, 2);

		//Assert
		Assert.Equal(12, control.Width);
		Assert.Equal(13, control.Height);
	}

	[Fact]
	public void Given_gui_control_created_by_script_When_checking_script_owner_Then_only_that_script_matches()
	{
		//Arrange
		var script = new Script(_scriptManager, ScriptType.Weapon);
		var otherScript = new Script(_scriptManager, ScriptType.Weapon);
		var control = new GuiControl("control", script);

		//Act
		var isOwnedByScript = control.IsOwnedBy(script);
		var isOwnedByOtherScript = control.IsOwnedBy(otherScript);

		//Assert
		Assert.True(isOwnedByScript);
		Assert.False(isOwnedByOtherScript);
	}

	[Fact(Skip = "fix later")]
	public async Task When_for_loop_with_8_loops_Then_echo_is_called_8_times___()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							echo(1.01+screenwidth);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		_ = await script.Call("onCreated");

		//Assert
		Assert.Equal("1025.01", _receivedStrings[0]);
	}

	[Fact(Skip = "fix later")]
	public async Task When_for_loop_with_8_loops_Then_echo_is_called_8_times____()
	{
		//Arrange
		_receivedStrings.Clear();
		_calledTimes = 0;
		const string scriptText =
			"""
						//#CLIENTSIDE
						function onCreated() {
							showimg(1402, "cog2.png", 85, 60);
							findimg(1402).rotation = 234;
							temp.rot2 = findimg(1402).rotation;
							echo(temp.rot2);
						}
			""";
		var script = CompileScript(scriptText);

		//Act
		_ = await script.Call("onCreated");

		//Assert
		Assert.Equal("234", _receivedStrings[0]);
	}

	private sealed class TestEventGuiControl(string id, Script script) : GuiControl(id, script)
	{
		public void TriggerAction() => CallAction();
	}

	private sealed class CountingGuiControl(string id, Script script) : GuiControl(id, script)
	{
		public int DrawCount { get; private set; }

		public override void Draw() => DrawCount++;
	}

	private sealed class MutatingGuiControl(string id, Script script, GuiControl parent, GuiControl childToAdd) : GuiControl(id, script)
	{
		public override void Draw() => parent.AddControl(childToAdd);
	}

	private class ParentPropertyMergeTestObject
	{
		public int ParentValue { get; set; }
	}

	private sealed class ChildPropertyMergeTestObject : ParentPropertyMergeTestObject
	{
		public int ChildValue { get; set; }
	}

	private sealed class ParentPropertyMergeTestProperties : ScriptProperties<ParentPropertyMergeTestObject>
	{
		public ParentPropertyMergeTestProperties() : base(null)
		{
			AddProperties(
				this,
				new PropertyDefinitions<ParentPropertyMergeTestObject>
				{
					{ "value", "", value => value.ParentValue, (value, propertyValue) => value.ParentValue = propertyValue }
				}
			);
		}
	}

	private sealed class ChildPropertyMergeTestProperties : ScriptProperties<ChildPropertyMergeTestObject>
	{
		public ChildPropertyMergeTestProperties() : base(typeof(ParentPropertyMergeTestObject))
		{
			AddProperties(
				this,
				new PropertyDefinitions<ChildPropertyMergeTestObject>
				{
					{ "value", "", value => value.ChildValue, (value, propertyValue) => value.ChildValue = propertyValue }
				}
			);
		}
	}
}
