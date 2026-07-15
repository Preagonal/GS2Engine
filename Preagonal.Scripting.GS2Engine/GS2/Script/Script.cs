using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Preagonal.Scripting.GS2Engine.Enums;
using Preagonal.Scripting.GS2Engine.Extensions;
using Preagonal.Scripting.GS2Engine.GS2.ByteCode;
using Preagonal.Scripting.GS2Engine.Models;
using static System.IO.File;

namespace Preagonal.Scripting.GS2Engine.GS2.Script;

public class Script : ScriptVariable
{
	private static readonly string[] Gs1EventNames =
	[
		"playerenters", "playertouchsme", "playertouchsother", "playerchats", "playerhurt", "playerdies",
		"playerlaysitem", "playerendsreading", "compusdied", "emoticon", "mousedown", "mouseup", "mousewheel",
		"exploded", "wasshot", "waspelt", "keypressed", "actionprojectile2", "", "", "", "playerleaves",
		"washit", "", "", "shapetrigger", "playanimation", "pkzonechanges",
	];

	private static readonly Dictionary<string, string> Gs1EventAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["playertouchesme"] = "playertouchsme",
		["playertouchesother"] = "playertouchsother",
		["playerhurted"] = "playerhurt",
		["wasshooted"] = "wasshot",
	};

	public delegate            IStackEntry                        Command(ScriptMachine machine, IStackEntry[]? args);
	public new static readonly ScriptObjProperties                PropertiesInstance = [];
	public override            IScriptProperties                  Properties => PropertiesInstance;
	private readonly           List<TString>                      _strings  = [];
	private readonly           Dictionary<string, Dictionary<Script, string>> _eventCatchers = new(StringComparer.OrdinalIgnoreCase);
	private readonly           object                             _functionsLock = new();
	public readonly            Dictionary<string, FunctionParams> Functions = new();
	private                    ScriptCom[]                        _bytecode = [];
	public readonly            ScriptVariable?                    RefObject = null;
	public                     bool                               ExecutionEnabled { get; private set; } = true;
	public                     bool                               HasOnlyFunctions { get; private set; } = true;
	public                     TString                            File             { get; set; }
	public                     ScriptType                         Type             { get; }
	private                    int                                Gs1Flags         { get; set; }
	public                     ScriptMachine                      Machine          { get; }
	public                     DateTime?                          Timer            { get; set; }
	public                     ScriptCom[]                        Bytecode         => _bytecode;



	public Script(IScriptManager scriptManager, ScriptType type)
	{
		_             = Properties;
		ScriptManager = scriptManager;
		Name          = string.Empty;
		File          = string.Empty;
		RefObject     = this;
		Machine       = new(this);
		Type          = type;
	}

	public Script(
		IScriptManager scriptManager,
		TString bytecodeFile,
		ScriptVariable? refObject = null,
		ScriptType? type = null
	)
	{
		_             = Properties;
		ScriptManager = scriptManager;
		Name      = Path.GetFileNameWithoutExtension(bytecodeFile);
		File      = bytecodeFile;
		RefObject = refObject;
		Machine   = new(this);
		Type      = type ?? ScriptType.Weapon;

		SetStream(ReadAllBytes(bytecodeFile));

		Init();
	}

	public Script(
		IScriptManager scriptManager,
		TString name,
		byte[] bytecode,
		ScriptVariable? refObject = null,
		ScriptType? type = null
	)
	{
		_             = Properties;
		ScriptManager = scriptManager;
		Name          = name;
		File          = "";
		RefObject     = refObject;
		Machine       = new(this);
		Type          = type ?? ScriptType.Weapon;

		SetStream(bytecode);

		Init();
	}

	~Script()
	{
		ScriptManager.UnregisterGlobalScript(this);
	}

	public void UpdateFromFile(string scriptFile)
	{
		Name = Path.GetFileNameWithoutExtension(scriptFile);
		File = scriptFile;
		SetStream(ReadAllBytes(scriptFile));

		Init();
	}

	public void UpdateFromByteCode(TString name, byte[] byteCode)
	{
		Name = name;
		File = "";
		SetStream(byteCode);

		Init();
	}

	public void HaltExecution() => ExecutionEnabled = false;

	public void EnableExecution() => ExecutionEnabled = true;

	private void Init()
	{
		ScriptManager.RegisterGlobalScript(this);

		EnableExecution();
	}

	private void Reset()
	{
		Machine.Reset();
		lock (_functionsLock)
			Functions.Clear();
		_bytecode = [];
		_strings.Clear();
		Clear();
		Gs1Flags = 0;
		HasOnlyFunctions = true;
		HaltExecution();
	}

	private void SetStream(TString bytecodeParam)
	{
		var oIndex = 0;
		bytecodeParam.setRead(0);

		CheckHeader(bytecodeParam);

		Reset();

		while (bytecodeParam.bytesLeft() > 0)
		{
			Tools.DebugLine($"Bytes left: {bytecodeParam.bytesLeft()}");

			if (bytecodeParam.bytesLeft() == 1)
				if (bytecodeParam.readChar() == '\n')
					break;

			var segmentType = (BytecodeSegment)bytecodeParam.readInt();

			if (segmentType is < BytecodeSegment.Gs1EventFlags or > BytecodeSegment.Bytecode)
			{
				Tools.Debug($"Segment: Unknown ({segmentType})\n");
				break;
			}

			Tools.Debug($"Segment: {segmentType.BytecodeSegmentToString()}\n");

			var segmentLength = bytecodeParam.readInt();

			var segmentSection = bytecodeParam.readChars(segmentLength);

			switch (segmentType)
			{
				case BytecodeSegment.Gs1EventFlags:
				{
					var flags = 0;
					if (3 < segmentSection.length())
						flags = segmentSection.readInt();
					Gs1Flags = flags;
					break;
				}

				case BytecodeSegment.FunctionNames:
				{
					if (0 < segmentSection.length())
						while (segmentSection.bytesLeft() > 0)
						{
							TString functionName = new();
							var     pos          = segmentSection.readInt();

							while (true)
							{
								var ch = segmentSection.readChar();
								if (ch == '\0') break;
								functionName.writeByte(ch);
							}

							var isPublic = functionName.starts("public.");
							if (isPublic) functionName.removeStart(7);
							AddFunction(functionName, pos, isPublic);

							Tools.Debug($"Function[{pos}]: {functionName}\n");
						}

					break;
				}

				case BytecodeSegment.Strings:
				{
					if (0 < segmentSection.length())
						while (segmentSection.bytesLeft() > 0)
						{
							TString stringName = new();
							while (true)
							{
								var ch = segmentSection.readChar();
								if (ch == '\0') break;
								stringName.writeByte(ch);
							}

							_strings.Add(stringName);
							Tools.Debug($"String: {stringName}\n");
						}

					break;
				}

				case BytecodeSegment.Bytecode:
				{
					ScriptCom op = new();
					while (segmentSection.bytesLeft() > 0)
					{
						var bytecodeByte = segmentSection.readChar();
						if (bytecodeByte is >= 0xF0 and <= 0xF6)
							Tools.Debug($" (Opcode: 0x{bytecodeByte}) ");

						switch (bytecodeByte)
						{
							case 0xF0:
							{
								var varIndex = segmentSection.readChar();

								op.VariableName = _strings[varIndex];

								Tools.Debug($" - variable[{varIndex}]({op.VariableName}) (byte)\n");
								break;
							}
							case 0xF1:
							{
								var varIndex = segmentSection.readShort();

								op.VariableName = _strings[varIndex];

								Tools.Debug($" - string({op.VariableName}) (word)\n");
								break;
							}
							case 0xF2:
							{
								var varIndex = segmentSection.readInt();

								op.VariableName = _strings[varIndex];

								Tools.Debug($" - string({op.VariableName}) (dword)\n");
								break;
							}
							case 0xF3:
							{
								var varIndex = (sbyte)segmentSection.readChar();
								op.Value = varIndex;
								Tools.Debug($" - double({op.Value}) (byte)\n");
								break;
							}
							case 0xF4:
							{
								var varIndex = segmentSection.readShort();
								op.Value = varIndex;
								Tools.Debug($" - double({op.Value}) (word)\n");
								break;
							}
							case 0xF5:
							{
								var varIndex = segmentSection.readInt();
								op.Value = varIndex;
								Tools.Debug($" - double({op.Value}) (dword)\n");
								break;
							}
							case 0xF6:
							{
								TString doubleString = new();
								while (true)
								{
									var ch = segmentSection.readChar();
									if (ch == '\0') break;
									doubleString.writeByte(ch);
								}

								doubleString = doubleString.ToString().Replace("--", "");
								op.Value     = double.Parse(doubleString.ToString(), CultureInfo.InvariantCulture);
								Tools.Debug($" - double({op.Value}) (string)\n");
								break;
							}

							default:
							{
								if (oIndex >= _bytecode.Length) Array.Resize(ref _bytecode, oIndex + 0x100);
								//BytecodeLength = oIndex + 0x100;
								op        = _bytecode[oIndex] = new();
								op.OpCode = (Opcode)bytecodeByte;
								++oIndex;
								break;
							}
						}
					}

					Tools.DebugLine("Bytecode done");
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		Array.Resize(ref _bytecode, oIndex);

		OnScriptUpdated();
	}

	private static void CheckHeader(TString bytecodeParam)
	{
		if (bytecodeParam.bytesLeft() < 1) return;
		var isPacket = bytecodeParam.readChar();
		if (isPacket != 0xAC)
		{
			bytecodeParam.setRead(0);
			var segmentType = (BytecodeSegment)bytecodeParam.readInt();

			if (segmentType is < BytecodeSegment.Gs1EventFlags or > BytecodeSegment.Bytecode)
			{
				bytecodeParam.setRead(0);
			}
			else
			{
				bytecodeParam.setRead(0);
				return;
			}
		}

		Tools.DebugLine("GServer packet header included");
		var infoSectionLength = (ushort)bytecodeParam.readGShort();
		Tools.DebugLine($"Length of information section: {infoSectionLength}");

		var infoSection = bytecodeParam.readChars(infoSectionLength);

		var data = infoSection.ToString().Split(',');

		var target = data[0];
		var name   = data[1];

		Tools.DebugLine($"Code target: {target}");
		Tools.DebugLine($"Target name: {name}");

		int.TryParse(data[2], out var saveScriptToFileInt);
		var saveScriptToFile = saveScriptToFileInt == 1 ? "Yes" : "No";

		Tools.DebugLine($"Save script to file: {saveScriptToFile}");

		/*
		infoSection = data[3];
		int keys  = 0;
		while (infoSection.bytesLeft() > 0)
		{
			uint key = infoSection.readGInt5().toUInt();
			//scriptKeys[keys] = CString(key);
			Tools.DebugLine($"Key({keys}): {key}");
			keys++;
		}
		*/
	}

	private void AddFunction(TString functionName, int pos, bool isPublic)
	{
		lock (_functionsLock)
			Functions[functionName.ToString().ToLowerInvariant()] = new() { BytecodePosition = pos, IsPublic = isPublic };
	}

	private void OnScriptUpdated()
	{
		FixBadByteCode();
		CheckOnlyFunctions();
		OptimizeByteCode();
	}

	private void FixBadByteCode()
	{
		var bytecodeLength = _bytecode.Length;
		for (var index = 0; index < bytecodeLength; index++)
		{
			var op = _bytecode[index];
			var branchOffset = (byte)op.OpCode - (byte)Opcode.OP_SET_INDEX;
			if (branchOffset is < 0 or >= 5)
				continue;

			if (op.Value >= 0.0d && op.Value <= bytecodeLength)
				continue;

			Tools.DebugLine("Script: bad script stream, game might have errors.");
			op.Value = bytecodeLength;
		}
	}

	private void CheckOnlyFunctions()
	{
		if (_bytecode.Length < 1)
		{
			HasOnlyFunctions = true;
			return;
		}

		HasOnlyFunctions = _bytecode[0].OpCode == Opcode.OP_SET_INDEX &&
		                   _bytecode.Length <= _bytecode[0].Value;
	}

	private void OptimizeByteCode()
	{
		if (_bytecode.Length < 2)
			return;

		for (var index = 0; index < _bytecode.Length - 1; index++)
		{
			var op = _bytecode[index];
			var next = _bytecode[index + 1];

			if (op.OpCode == Opcode.OP_TYPE_NUMBER)
			{
				if (next.OpCode == Opcode.OP_ARRAY)
				{
					op.OpCode = Opcode.OP_UNKNOWN_240;
					SetNoOp(index + 1);
					index++;
					continue;
				}

				var hasAssignAfterNext = index + 2 < _bytecode.Length && _bytecode[index + 2].OpCode == Opcode.OP_ASSIGN;
				var replacement = hasAssignAfterNext
					? GetOptimizedImmediateAssignOpcode(next.OpCode)
					: GetOptimizedImmediateOpcode(next.OpCode);

				if (replacement != null)
				{
					op.OpCode = replacement.Value;
					SetNoOp(index + 1);
					if (hasAssignAfterNext)
					{
						SetNoOp(index + 2);
						index += 2;
					}
					else
					{
						index++;
					}
				}

				continue;
			}

			if (op.OpCode == Opcode.OP_TYPE_VAR)
			{
				if (next.OpCode == Opcode.OP_CONV_TO_OBJECT)
				{
					op.OpCode = Opcode.OP_UNKNOWN_235;
					SetNoOp(index + 1);
					index++;
					continue;
				}

				if (next.OpCode == Opcode.OP_MEMBER_ACCESS)
				{
					var replacement = Opcode.OP_UNKNOWN_234;
					var consumed = 1;
					if (index + 2 < _bytecode.Length)
					{
						switch (_bytecode[index + 2].OpCode)
						{
							case Opcode.OP_CONV_TO_FLOAT:
								replacement = Opcode.OP_UNKNOWN_236;
								consumed = 2;
								break;
							case Opcode.OP_CONV_TO_STRING:
								replacement = Opcode.OP_UNKNOWN_237;
								consumed = 2;
								break;
							case Opcode.OP_CONV_TO_OBJECT:
								replacement = Opcode.OP_UNKNOWN_238;
								consumed = 2;
								break;
							case Opcode.OP_UNKNOWN_47
								when index + 3 >= _bytecode.Length || _bytecode[index + 3].OpCode != Opcode.OP_UNKNOWN_45:
								replacement = Opcode.OP_UNKNOWN_239;
								consumed = 2;
								break;
						}
					}

					op.OpCode = replacement;
					for (var offset = 1; offset <= consumed; offset++)
						SetNoOp(index + offset);
					index += consumed;
					continue;
				}
			}

			if (op.OpCode == Opcode.OP_UNKNOWN_46)
			{
				var replacement = GetOptimizedRegisterOpcode(next.OpCode);
				if (replacement != null)
				{
					op.OpCode = replacement.Value;
					SetNoOp(index + 1);
					index++;
					continue;
				}

				replacement = GetOptimizedRegisterMutationOpcode(next.OpCode);
				if (replacement != null &&
				    index + 2 < _bytecode.Length &&
				    _bytecode[index + 2].OpCode == Opcode.OP_INDEX_DEC)
				{
					op.OpCode = replacement.Value;
					SetNoOp(index + 1);
					SetNoOp(index + 2);
					index += 2;
					continue;
				}
			}

			if (op.OpCode == Opcode.OP_UNKNOWN_47 && next.OpCode == Opcode.OP_UNKNOWN_45)
			{
				op.OpCode = Opcode.OP_UNKNOWN_242;
				op.Value = next.Value;
				SetNoOp(index + 1);
				index++;
				continue;
			}

			var assignmentReplacement = next.OpCode == Opcode.OP_ASSIGN
				? GetOptimizedStackAssignOpcode(op.OpCode)
				: null;

			if (assignmentReplacement == null)
				continue;

			op.OpCode = assignmentReplacement.Value;
			SetNoOp(index + 1);
			index++;
		}
	}

	private void SetNoOp(int index)
	{
		_bytecode[index].OpCode = Opcode.OP_NONE;
		_bytecode[index].Value = 0.0d;
		_bytecode[index].VariableName = null;
	}

	private static Opcode? GetOptimizedImmediateOpcode(Opcode opcode) =>
		opcode switch
		{
			Opcode.OP_ADD => Opcode.OP_UNKNOWN_200,
			Opcode.OP_SUB => Opcode.OP_UNKNOWN_201,
			Opcode.OP_MUL => Opcode.OP_UNKNOWN_202,
			Opcode.OP_DIV => Opcode.OP_UNKNOWN_203,
			Opcode.OP_MOD => Opcode.OP_UNKNOWN_204,
			Opcode.OP_POW => Opcode.OP_UNKNOWN_205,
			Opcode.OP_UNKNOWN_66 => Opcode.OP_UNKNOWN_206,
			Opcode.OP_UNKNOWN_67 => Opcode.OP_UNKNOWN_207,
			Opcode.OP_LT => Opcode.OP_UNKNOWN_224,
			Opcode.OP_GT => Opcode.OP_UNKNOWN_225,
			Opcode.OP_LTE => Opcode.OP_UNKNOWN_226,
			Opcode.OP_GTE => Opcode.OP_UNKNOWN_227,
			_ => null,
		};

	private static Opcode? GetOptimizedStackAssignOpcode(Opcode opcode) =>
		opcode switch
		{
			Opcode.OP_ADD => Opcode.OP_UNKNOWN_208,
			Opcode.OP_SUB => Opcode.OP_UNKNOWN_209,
			Opcode.OP_MUL => Opcode.OP_UNKNOWN_210,
			Opcode.OP_DIV => Opcode.OP_UNKNOWN_211,
			Opcode.OP_MOD => Opcode.OP_UNKNOWN_212,
			Opcode.OP_POW => Opcode.OP_UNKNOWN_213,
			Opcode.OP_UNKNOWN_66 => Opcode.OP_UNKNOWN_214,
			Opcode.OP_UNKNOWN_67 => Opcode.OP_UNKNOWN_215,
			_ => null,
		};

	private static Opcode? GetOptimizedImmediateAssignOpcode(Opcode opcode) =>
		opcode switch
		{
			Opcode.OP_ADD => Opcode.OP_UNKNOWN_216,
			Opcode.OP_SUB => Opcode.OP_UNKNOWN_217,
			Opcode.OP_MUL => Opcode.OP_UNKNOWN_218,
			Opcode.OP_DIV => Opcode.OP_UNKNOWN_219,
			Opcode.OP_MOD => Opcode.OP_UNKNOWN_220,
			Opcode.OP_POW => Opcode.OP_UNKNOWN_221,
			Opcode.OP_UNKNOWN_66 => Opcode.OP_UNKNOWN_222,
			Opcode.OP_UNKNOWN_67 => Opcode.OP_UNKNOWN_223,
			_ => null,
		};

	private static Opcode? GetOptimizedRegisterOpcode(Opcode opcode) =>
		opcode switch
		{
			Opcode.OP_COPY_LAST_OP => Opcode.OP_UNKNOWN_233,
			Opcode.OP_CONV_TO_FLOAT => Opcode.OP_UNKNOWN_228,
			Opcode.OP_CONV_TO_STRING => Opcode.OP_UNKNOWN_229,
			Opcode.OP_CONV_TO_OBJECT => Opcode.OP_UNKNOWN_230,
			_ => null,
		};

	private static Opcode? GetOptimizedRegisterMutationOpcode(Opcode opcode) =>
		opcode switch
		{
			Opcode.OP_INC => Opcode.OP_UNKNOWN_231,
			Opcode.OP_DEC => Opcode.OP_UNKNOWN_232,
			_ => null,
		};

	private async Task<IStackEntry> Execute(string functionName, Stack<IStackEntry>? parameters = null, ScriptVariable? receiverOverride = null, bool inheritTempFrame = false)
	{
		try
		{
			return await Machine.Execute(functionName, parameters, receiverOverride, inheritTempFrame).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			Tools.DebugLine(e.Message);
			throw;
			//return 0.ToStackEntry();
		}
	}

	internal Task<IStackEntry> CallEntries(string eventName, IEnumerable<IStackEntry>? args, ScriptVariable? receiverOverride = null) =>
		Execute(eventName, BuildCallStack(args), receiverOverride);

	private Task<IStackEntry> CallEntries(string eventName, IEnumerable<IStackEntry>? args, ScriptVariable? receiverOverride, bool inheritTempFrame) =>
		Execute(eventName, BuildCallStack(args), receiverOverride, inheritTempFrame);

	internal void InstallObjectEventCatchers(string objectName, Script sourceScript)
	{
		if (string.IsNullOrWhiteSpace(objectName)) return;

		var prefix = $"{objectName}.";
		var normalizedPrefix = prefix.ToLowerInvariant();
		lock (_eventCatchers)
		{
			foreach (var eventName in _eventCatchers.Keys.Where(key => key.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)).ToArray())
			{
				var catchers = _eventCatchers[eventName];
				catchers.Remove(sourceScript);
				if (catchers.Count == 0)
					_eventCatchers.Remove(eventName);
			}

			foreach (var functionName in sourceScript.GetObjectEventFunctionNames(prefix))
			{
				if (!_eventCatchers.TryGetValue(functionName, out var catchers))
				{
					catchers = new();
					_eventCatchers[functionName] = catchers;
				}

				catchers[sourceScript] = functionName;
			}
		}
	}

	private IEnumerable<string> GetObjectEventFunctionNames(string objectPrefix)
	{
		string[] functionNames;
		lock (_functionsLock)
			functionNames = Functions.Keys.ToArray();

		return functionNames
		       .Where(functionName =>
		       {
			       if (!functionName.StartsWith(objectPrefix, StringComparison.OrdinalIgnoreCase)) return false;
			       var eventName = functionName[objectPrefix.Length..];
			       return eventName.StartsWith("on", StringComparison.OrdinalIgnoreCase);
		       })
		       .ToArray();
	}

	private bool TryGetEventCatchers(string eventName, out KeyValuePair<Script, string>[] catchers)
	{
		lock (_eventCatchers)
		{
			if (_eventCatchers.TryGetValue(eventName.ToLowerInvariant(), out var eventCatchers))
			{
				catchers = eventCatchers.ToArray();
				return catchers.Length > 0;
			}
		}

		catchers = [];
		return false;
	}

	private static Stack<IStackEntry> BuildCallStack(IEnumerable<IStackEntry>? args)
	{
		var callStack = new Stack<IStackEntry>();
		if (args == null) return callStack;

		foreach (var variable in args.Reverse())
			callStack.Push(variable);

		return callStack;
	}

	private static IStackEntry? ToCallStackEntry(object variable) =>
		variable switch
		{
			IStackEntry entry => entry,
			string s => s.ToStackEntry(),
			int i => i.ToStackEntry(),
			double d => d.ToStackEntry(),
			float f => f.ToStackEntry(),
			decimal dc => dc.ToStackEntry(),
			string[] sa => sa.ToStackEntry(),
			int[] ia => ia.ToStackEntry(),
			bool bo => bo.ToStackEntry(),
			VariableCollection p => p.ToStackEntry(),
			IEnumerable enumerable => enumerable.Cast<object?>().ToStackEntry(),
			_ => null
		};

	private static bool IsObjectEventName(string eventName)
	{
		var dotIndex = eventName.LastIndexOf('.');
		return dotIndex >= 0 &&
		       dotIndex + 2 < eventName.Length &&
		       eventName.AsSpan(dotIndex + 1).StartsWith("on", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryGetGs1Event(string eventName, out string canonicalName, out int eventIndex)
	{
		var normalizedName = eventName.ToLowerInvariant();
		if (normalizedName.StartsWith("on", StringComparison.Ordinal))
			normalizedName = normalizedName[2..];

		canonicalName = Gs1EventAliases.GetValueOrDefault(normalizedName, normalizedName);
		var eventNameToFind = canonicalName;
		eventIndex = Array.FindIndex(Gs1EventNames, name => name == eventNameToFind);
		return eventIndex >= 0;
	}

	private Script? GetJoinedClass(string className)
	{
		if (!ScriptManager.GlobalVariables.TryGetVariable(className, out var entry)) return null;

		object? value = entry?.GetValue();
		while (value is IStackEntry stackEntry)
			value = stackEntry.GetValue();

		return value as Script;
	}

	private bool HasEventFunction(string functionName, HashSet<string>? visitedClasses = null)
	{
		lock (_functionsLock)
			if (Functions.ContainsKey(functionName))
				return true;

		visitedClasses ??= new(StringComparer.OrdinalIgnoreCase);
		foreach (var className in JoinedClassNames)
		{
			if (!visitedClasses.Add(className)) continue;
			var classScript = GetJoinedClass(className);
			if (classScript != null && classScript.HasEventFunction(functionName, visitedClasses)) return true;
		}

		return false;
	}

	private bool HasGs1EventFlag(int eventIndex)
	{
		var eventFlag = 1 << (eventIndex & 0x1f);
		if ((Gs1Flags & eventFlag) != 0) return true;

		foreach (var className in JoinedClassNames)
		{
			var classScript = GetJoinedClass(className);
			if (classScript != null && (classScript.Gs1Flags & eventFlag) != 0) return true;
		}

		return false;
	}

	private bool EventFunctionStartsAtScriptRoot(string functionName)
	{
		lock (_functionsLock)
			return Functions.TryGetValue(functionName, out var function) && function.BytecodePosition == 0;
	}

	private async Task<IStackEntry> CallScriptEvent(
		string eventName,
		int? gs1EventIndex,
		IReadOnlyCollection<IStackEntry>? entries
	)
	{
		var functionName = $"on{eventName}";
		var hasEventFunction = HasEventFunction(functionName);
		var needsWholeScript = gs1EventIndex == null || HasGs1EventFlag(gs1EventIndex.Value);
		if (!needsWholeScript && !hasEventFunction) return 0.ToStackEntry();

		IStackEntry result = 0.ToStackEntry();
		var executedWholeScript = !HasOnlyFunctions;
		if (executedWholeScript)
			result = await Machine.ExecuteScript(eventName, BuildCallStack(entries)).ConfigureAwait(false);

		if (hasEventFunction && (!executedWholeScript || !EventFunctionStartsAtScriptRoot(functionName)))
			result = await Execute(functionName, BuildCallStack(entries)).ConfigureAwait(false);

		return result;
	}

	/// <summary>
	///     Function -> Call Event for Object
	/// </summary>
	public async Task<IStackEntry> Call(string eventName, params object[]? args)
	{
		try
		{
			var entries = args?.Select(ToCallStackEntry).Where(entry => entry != null).Cast<IStackEntry>().ToArray();
			if (TryGetGs1Event(eventName, out var gs1EventName, out var gs1EventIndex))
				return await CallScriptEvent(gs1EventName, gs1EventIndex, entries).ConfigureAwait(false);

			if (eventName.Equals("created", StringComparison.OrdinalIgnoreCase) ||
			    eventName.Equals("oncreated", StringComparison.OrdinalIgnoreCase))
				return await CallScriptEvent("created", null, entries).ConfigureAwait(false);

			var inheritTempFrame = IsObjectEventName(eventName);
			bool hasFunction;
			lock (_functionsLock)
				hasFunction = Functions.ContainsKey(eventName.ToLowerInvariant());

			if (hasFunction)
				return await Execute(eventName, BuildCallStack(entries), inheritTempFrame: inheritTempFrame).ConfigureAwait(false);

			if (TryGetEventCatchers(eventName, out var catchers))
			{
				IStackEntry result = 0.ToStackEntry();
				foreach (var catcher in catchers)
					result = await catcher.Key.CallEntries(
						catcher.Value,
						entries,
						null,
						IsObjectEventName(catcher.Value)
					).ConfigureAwait(false);

				return result;
			}

			if (args == null) return await Execute(eventName).ConfigureAwait(false);

			var callStack = BuildCallStack(entries);

			return await Execute(eventName, callStack, inheritTempFrame: inheritTempFrame).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			Tools.DebugLine($"Error calling {Name}.{eventName}: {e}");
		}

		return 0.ToStackEntry();
	}

	public async Task<IStackEntry> TriggerEvent(string eventName)
	{
		switch (eventName.ToLowerInvariant())
		{
			case "ontimeout":
			{
				break;
			}
			default:
				return await Execute(eventName).ConfigureAwait(false);
		}

		return 0.ToStackEntry();
	}

	public void SetTimer(double value)
	{
		Timer = DateTime.UtcNow.AddSeconds(value);
		/*try
		{
			if (!ThreadPool.QueueUserWorkItem(
				    delegate
				    {
					    DelayedMethodCall(
						    value,
						    () => OnTriggerEvent("onTimeout").ConfigureAwait(false).GetAwaiter().GetResult()
					    );
				    }
			    ))
			{
				Console.WriteLine("Timer function not queued");
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"{e.Message}: {e}");
		}*/
	}

	private static void DelayedMethodCall(double seconds, Action methodToCall)
	{
		Thread.Sleep((int)(seconds * 1500));  // Convert seconds to milliseconds
		methodToCall();
	}

	public async Task OnTriggerEvent(string eventName)
	{
		Timer = null;
		await Execute(eventName).ConfigureAwait(false);
	}

	public void AddObjectReference(string objectType, ScriptVariable obj)
	{
		ScriptManager.RegisterGlobalObject(objectType, obj);
	}

	public IScriptManager ScriptManager { get; }
}
