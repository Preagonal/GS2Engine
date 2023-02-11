using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GS2Engine.Extensions;
using GS2Engine.GS2.ByteCode;
using GS2Engine.Models;
using static System.IO.File;

namespace GS2Engine.GS2.Script
{
	public class Script
	{
		public delegate IStackEntry Command(ScriptMachine machine, IStackEntry[]? args);

		public static VariableCollection GlobalVariables = new();
		private readonly List<TString> _strings = new();
		public readonly Dictionary<string, FunctionParams> Functions = new();
		private ScriptCom[] _bytecode = Array.Empty<ScriptCom>();
		public IDictionary<string, VariableCollection> GlobalObjects = new Dictionary<string, VariableCollection>();

		public Script(
			TString bytecodeFile,
			IDictionary<string, VariableCollection>? objects,
			VariableCollection? variables,
			Dictionary<string, Command>? functions
		)
		{
			Name = Path.GetFileNameWithoutExtension(bytecodeFile);
			File = bytecodeFile;
			Machine = new(this);
			SetStream(ReadAllBytes(bytecodeFile));

			Init(objects, variables, functions);
		}
		
		public Script(
			TString name,
			byte[] bytecode,
			IDictionary<string, VariableCollection>? objects,
			VariableCollection? variables,
			Dictionary<string, Command>? functions
		)
		{
			Name = name;
			File = "";
			Machine = new(this);
			SetStream(bytecode);

			Init(objects, variables, functions);
		}

		private int BytecodeLength => _bytecode.Length;

		public  TString       Name     { get; set; }
		public  TString       File     { get; set; }
		private int           Gs1Flags { get; set; }
		public  ScriptMachine Machine  { get; }
		private DateTime?     Timer    { get; set; }

		public ScriptCom[]                 Bytecode          => _bytecode;
		public Dictionary<string, Command> ExternalFunctions { get; } = new();

		public void UpdateFromFile(
			string scriptFile,
			IDictionary<string, VariableCollection>? objects,
			VariableCollection? variables,
			Dictionary<string, Command>? functions
		)
		{
			Name = Path.GetFileNameWithoutExtension(scriptFile);
			File = scriptFile;
			SetStream(ReadAllBytes(scriptFile));

			Init(objects, variables, functions);
		}

		public void UpdateFromByteCode(
			TString name,
			byte[] byteCode,
			IDictionary<string, VariableCollection>? objects,
			VariableCollection? variables,
			Dictionary<string, Command>? functions
		)
		{
			Name = name;
			File = "";
			SetStream(byteCode);

			Init(objects, variables, functions);
		}

		private void Init(
			IDictionary<string, VariableCollection>? objects,
			VariableCollection? variables,
			Dictionary<string, Command>? functions
		)
		{
			if (objects != null)
				GlobalObjects = objects;

			if (variables != null)
				GlobalVariables = variables;

			if (functions != null)
				foreach (KeyValuePair<string, Command> obj in functions)
					ExternalFunctions?.Add(obj.Key, obj.Value);

			ExternalFunctions?.Add(
				"settimer",
				delegate(ScriptMachine machine, IStackEntry[]? args)
				{
					if (args?.Length > 0)
						SetTimer((double)(machine.GetEntry(args[0]).GetValue() ?? 0));
					return 0.ToStackEntry();
				}
			);

			Execute("onCreated").ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private void Reset()
		{
			Machine.Reset();
			Functions.Clear();
			ExternalFunctions?.Clear();
			_bytecode = Array.Empty<ScriptCom>();
		}


		private void SetStream(TString bytecodeParam)
		{
			int oIndex = 0;

			CheckHeader(bytecodeParam);

			Reset();
			bytecodeParam.setRead(0);

			while (bytecodeParam.bytesLeft() > 0)
			{
				Tools.DebugLine($"Bytes left: {bytecodeParam.bytesLeft()}");

				if (bytecodeParam.bytesLeft() == 1)
					if (bytecodeParam.readChar() == '\n')
						break;

				BytecodeSegment segmentType = (BytecodeSegment)bytecodeParam.readInt();

				if (segmentType is < BytecodeSegment.Gs1EventFlags or > BytecodeSegment.Bytecode)
				{
					Tools.Debug($"Segment: Unknown ({segmentType})\n");
					break;
				}

				Tools.Debug($"Segment: {segmentType.BytecodeSegmentToString()}\n");

				int segmentLength = bytecodeParam.readInt();

				TString segmentSection = bytecodeParam.readChars(segmentLength);

				switch (segmentType)
				{
					case BytecodeSegment.Gs1EventFlags:
					{
						int flags = 0;
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
								int pos = segmentSection.readInt();

								while (true)
								{
									byte ch = segmentSection.readChar();
									if (ch == '\0') break;
									functionName.writeChar(ch);
								}

								bool isPublic = functionName.starts("public.");
								if (isPublic) functionName.removeStart(7);
								addFunction(functionName, pos, isPublic);

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
									byte ch = segmentSection.readChar();
									if (ch == '\0') break;
									stringName.writeChar(ch);
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
							byte bytecodeByte = segmentSection.readChar();
							if (bytecodeByte is >= 0xF0 and <= 0xF6)
								Tools.Debug($" (Opcode: 0x{bytecodeByte}) ");

							switch (bytecodeByte)
							{
								case 0xF0:
								{
									byte varIndex = segmentSection.readChar();

									op.VariableName = _strings[varIndex];

									Tools.Debug($" - variable[{varIndex}]({op.VariableName}) (byte)\n");
									break;
								}
								case 0xF1:
								{
									short varIndex = segmentSection.readShort();

									op.VariableName = _strings[varIndex];

									Tools.Debug($" - string({op.VariableName}) (word)\n");
									break;
								}
								case 0xF2:
								{
									int varIndex = segmentSection.readInt();

									op.VariableName = _strings[varIndex];

									Tools.Debug($" - string({op.VariableName}) (dword)\n");
									break;
								}
								case 0xF3:
								{
									sbyte varIndex = (sbyte)segmentSection.readChar();
									op.Value = varIndex;
									Tools.Debug($" - double({op.Value}) (byte)\n");
									break;
								}
								case 0xF4:
								{
									short varIndex = segmentSection.readShort();
									op.Value = varIndex;
									Tools.Debug($" - double({op.Value}) (word)\n");
									break;
								}
								case 0xF5:
								{
									int varIndex = segmentSection.readInt();
									op.Value = varIndex;
									Tools.Debug($" - double({op.Value}) (dword)\n");
									break;
								}
								case 0xF6:
								{
									TString doubleString = new();
									while (true)
									{
										byte ch = segmentSection.readChar();
										if (ch == '\0') break;
										doubleString.writeChar(ch);
									}

									doubleString = doubleString.ToString().Replace("--", "");
									op.Value = double.Parse(doubleString.ToString(), CultureInfo.InvariantCulture);
									Tools.Debug($" - double({op.Value}) (string)\n");
									break;
								}

								default:
								{
									if (oIndex >= BytecodeLength) Array.Resize(ref _bytecode, oIndex + 0x100);
									//BytecodeLength = oIndex + 0x100;
									op = Bytecode[oIndex] = new();
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

			onScriptUpdated();
		}

		private static void CheckHeader(TString bytecodeParam)
		{
			byte isPacket = bytecodeParam.readChar();
			if (isPacket != 0xAC) return;

			Tools.DebugLine("GServer packet header included");
			ushort infoSectionLength = (ushort)bytecodeParam.readShort();
			Tools.DebugLine("Length of information section: $infoSectionLength");

			TString infoSection = bytecodeParam.readChars(infoSectionLength);

			string[] data = infoSection.ToString().Split(',');

			string target = data[0];
			string name = data[1];

			Tools.DebugLine($"Code target: {target}");
			Tools.DebugLine($"Target name: {name}");

			int.TryParse(data[2], out int saveScriptToFileInt);
			string saveScriptToFile = saveScriptToFileInt == 1 ? "Yes" : "No";

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

		private void addFunction(TString functionName, int pos, bool isPublic) =>
			Functions.Add(functionName.ToString().ToLower(), new() { BytecodePosition = pos, IsPublic = isPublic });


		private static void onScriptUpdated()
		{
			//fixBadByteCode();
			//checkOnlyFunctions();
			//optimizeByteCode();
		}

		private static void optimizeByteCode()
		{
		}

		private async Task<IStackEntry> Execute(string functionName, Stack<IStackEntry>? parameters = null)
		{
			try
			{
				return await Machine.Execute(functionName, parameters);
			}
			catch (Exception e)
			{
				Tools.DebugLine(e.Message);
				return 0.ToStackEntry();
			}
		}

		/// <summary>
		///     Function -> Call Event for Object
		/// </summary>
		public async Task Call(string eventName, object[]? args)
		{
			try
			{
				Stack<IStackEntry> callStack = new();
				if (args != null)
					foreach (object variable in args.Reverse())
						switch (variable)
						{
							case string s:
								callStack.Push(s.ToStackEntry());
								break;
							case int i:
								callStack.Push(i.ToStackEntry());
								break;
							case double d:
								callStack.Push(d.ToStackEntry());
								break;
							case float f:
								callStack.Push(f.ToStackEntry());
								break;
							case decimal dc:
								callStack.Push(dc.ToStackEntry());
								break;
							case string[] sa:
								callStack.Push(sa.ToStackEntry());
								break;
							case int[] ia:
								callStack.Push(ia.ToStackEntry());
								break;
							case bool bo:
								callStack.Push(bo.ToStackEntry());
								break;
						}

				await Execute(eventName, callStack);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public async Task<IStackEntry> TriggerEvent(string eventName)
		{
			switch (eventName.ToLower())
			{
				case "ontimeout":
				{
					break;
				}
				default:
					return await Execute(eventName);
			}

			return 0.ToStackEntry();
		}

		private void SetTimer(double value) => Timer = DateTime.UtcNow.AddSeconds(value);


		public async Task<IStackEntry> RunEvents()
		{
			if (Timer <= DateTime.UtcNow)
			{
				Timer = null;
				return await Execute("onTimeout").ConfigureAwait(false);
			}

			return 0.ToStackEntry();
		}

		public void AddObjectReference(string objectType, VariableCollection obj)
		{
			if (GlobalObjects.ContainsKey(objectType))
				GlobalObjects[objectType] = obj;
			else
				GlobalObjects.Add(objectType, obj);
		}
	}
}