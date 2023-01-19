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
		private readonly List<TString>                          _strings  = new();
		public readonly  Dictionary<string, FunctionParams>     Functions = new();
		public readonly  Dictionary<string, VariableCollection> Objects   = new();
		public readonly  VariableCollection                     Variables = new();

		private ScriptCom[] _bytecode = Array.Empty<ScriptCom>();

		public Script(TString bytecodeFile, IDictionary<string, VariableCollection>? objects, VariableCollection? variables, Dictionary<string, Command>? functions)
		{
			Name = Path.GetFileNameWithoutExtension(bytecodeFile);
			File = bytecodeFile;
			Machine = new(this);
			setStream(ReadAllBytes(bytecodeFile));

			Init(objects, variables, functions);
		}
		
		public void UpdateFromFile(string scriptFile, IDictionary<string, VariableCollection>? objects, VariableCollection? variables, Dictionary<string, Command>? functions)
		{
			Name = Path.GetFileNameWithoutExtension(scriptFile);
			File = scriptFile;
			setStream(ReadAllBytes(scriptFile));
			
			Init(objects, variables, functions);
		}

		public void UpdateFromByteCode(byte[] byteCode,IDictionary<string, VariableCollection>? objects, VariableCollection? variables, Dictionary<string, Command>? functions)
		{
			setStream(byteCode);

			Init(objects, variables, functions);
		}

		private void Init(IDictionary<string, VariableCollection>? objects, VariableCollection? variables, Dictionary<string, Command>? functions)
		{
			if (objects != null)
				foreach (KeyValuePair<string, VariableCollection> obj in objects)
					Objects.Add(obj.Key, obj.Value);

			Variables.AddOrUpdate(variables);

			if (functions != null)
				foreach (KeyValuePair<string, Command> obj in functions)
					ExternalFunctions.Add(obj.Key, obj.Value);

			ExternalFunctions.Add("settimer", delegate(ScriptMachine machine, IStackEntry[]? args)
			{
				if (args?.Length > 0)
					SetTimer((double)(machine.GetEntry(args[0]).GetValue() ?? 0));
				return 0.ToStackEntry();
			});

			Execute("onCreated").ConfigureAwait(false).GetAwaiter().GetResult();
		}
		public delegate IStackEntry Command(ScriptMachine machine, IStackEntry[]? args);
		private void Reset()
		{
			Machine?.Reset();
			Variables.Clear();
			Functions.Clear();
			Objects.Clear();
			ExternalFunctions.Clear();
			_bytecode = Array.Empty<ScriptCom>();
		}

		private int BytecodeLength => _bytecode.Length;

		public  TString       Name     { get; set; }
		public  TString       File     { get; set; }
		private int           Gs1Flags { get; set; }
		public  ScriptMachine Machine  { get; set; }
		private DateTime?     Timer    { get; set; }

		public ScriptCom[]                 Bytecode          => _bytecode;
		public Dictionary<string, Command> ExternalFunctions { get; } = new();


		private void setStream(TString bytecodeParam)
		{
			int oIndex = 0;


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
						ScriptCom op = null;
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
									byte varIndex = segmentSection.readChar();
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
				}
			}

			Array.Resize(ref _bytecode, oIndex);

			onScriptUpdated();
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
			/*
			TString str;
			uint hashcode;
			TScriptProperty* pTVar1;
			int oIndex;
			void* __fn;
			void* in_R8;
			TProperties* properties;
			TScriptCom* bytecodeByte1;
			TScriptCom* bytecodeByte2;
			int length;
			unsigned char opCode;
			unsigned char opCode2;

			oIndex = 0;
			length = this->bytecodeLength;
			if (1 < length)
				do
				{
					properties = TScriptUniverse_properties;
					bytecodeByte1 = &this->bytecode[oIndex];
					bytecodeByte2 = &this->bytecode[oIndex + 1];
					opCode = bytecodeByte1->byte;
					switch (opCode)
					{
						case '\x14':
							opCode2 = bytecodeByte2->byte;
							if (opCode2 < 0x4c)
							{
								if (opCode2 < 0x48)
								{
									if ((unsigned char)opCode2 - 0x3c < 8) {
										if (oIndex + 2 < length && (this->bytecode[(long)oIndex + 2].byte == '2')) {
											bytecodeByte1->byte = opCode2 + 0x9c;
											oIndex = oIndex + 2;
										}
										else {
											bytecodeByte1->byte = opCode2 + 0x8c;
											oIndex = oIndex + 1;
										}
									}
								}
								else
								{
									oIndex = oIndex + 1;
									bytecodeByte1->byte = opCode2 + 0x98;
								}
							}
							else if (opCode2 == 0x83)
							{
								bytecodeByte1->byte = -0x10;
								oIndex = oIndex + 1;
							}

							break;
						case '\x16':
							switch (bytecodeByte2->

							byte) {
							case '\x06':
							case '-':
							case '/':
							str = bytecodeByte1->variableName;
							hashcode = THashList::getHashcode(str);
							__fn = (void*)hashcode;
							/*
									pTVar1 = (TScriptProperty *)
										THashList::getObjectEncoded(&properties->hashList,hashcode,str);
									if ((pTVar1 != (TScriptProperty *)0x0) && (pTVar1->isFunction != false)) {
										str.clear();
										bytecodeByte1->byte = -0xf;
										if ((this->graalVar).protected_object < pTVar1->functionLevel) {
											length = TServerList::getServerPrivileges();
											if (length < (int)(uint)pTVar1->functionLevel) {
												pTVar1 = TScriptProperty::clone
													(pTVar1,__fn,(void *)(ulong)pTVar1->functionLevel,0x408390,in_R8)
													;
												bytecodeByte1->scriptProperty = pTVar1;
												pTVar1->functionLevel = 10;
												break;
											}
										}
										bytecodeByte1->scriptProperty = pTVar1;
									}
									*
							break;
							case '#':
							if (oIndex < length + -2)
							{
								opCode2 = this->bytecode[(long)oIndex + 2].byte;
								if (opCode2 == 0x22)
								{
									bytecodeByte1->byte = -0x13;
									oIndex = oIndex + 2;
								}
								else if (opCode2 < 0x23)
								{
									if (opCode2 != 0x21) goto LAB_0032d73b;
									bytecodeByte1->byte = -0x14;
									oIndex = oIndex + 2;
								}
								else if (opCode2 == 0x24)
								{
									bytecodeByte1->byte = -0x12;
									oIndex = oIndex + 2;
								}
								else
								{
									if (opCode2 != 0x2f ||
									    ((oIndex + 3 < length && (this->bytecode[(long)oIndex + 3].byte == '-'))))
									goto LAB_0032d73b;
									bytecodeByte1->byte = -0x11;
									oIndex = oIndex + 2;
								}
							}
							else
							{
								bytecodeByte1->byte = -0x16;
								oIndex = oIndex + 1;
							}

							break;
							case '$':
							bytecodeByte1->byte = -0x15;
							oIndex = oIndex + 1;
						}
							break;
						case '.':
							opCode2 = bytecodeByte2->byte - 0x1e;
							if (opCode2 < 0x18)
							{
								/*
								/* WARNING: Could not recover jumptable at 0x0032d702. Too many branches */
			/* WARNING: Treating indirect jump as call *
			(*(code *)(&DAT_00408390 + *(int *)(&DAT_00408390 + (ulong)opCode2 * 4)))
				(length,opCode,&DAT_00408390 + *(int *)(&DAT_00408390 + (ulong)opCode2 * 4));
			return;
			*
		}

		break;
	case '/':
		if (bytecodeByte2->byte == '-') {
		bytecodeByte1->byte = -0xe;
		bytecodeByte1->value = bytecodeByte2->value;
		oIndex = oIndex + 1;
	}
		break;
	case '<':
	case '=':
	case '>':
	case '?':
	case '@':
	case 'A':
	case 'B':
	case 'C':
		if (bytecodeByte2->byte == '2') {
		bytecodeByte1->byte = opCode + -0x6c;
		oIndex = oIndex + 1;
	}
}

length = this->bytecodeLength;
oIndex = oIndex + 1;
} while (oIndex < length + -1);
*/
		}

		private async Task<IStackEntry> Execute(string functionName, Stack<IStackEntry>? parameters = null) =>
			await Machine.Execute(functionName, parameters);

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

		public void SetTimer(double value) => Timer = DateTime.UtcNow.AddSeconds(value);
		

		public async Task<IStackEntry> RunEvents()
		{
			if (Timer <= DateTime.UtcNow)
			{
				Timer = null;
				return await Execute("onTimeout");
			}

			return 0.ToStackEntry();
		}

		public void AddObjectReference(string objectType, VariableCollection obj)
		{
			if (Objects.ContainsKey(objectType))
				Objects[objectType] = obj;
			else
				Objects.Add(objectType, obj);
		}
	}
}