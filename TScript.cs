using System.Globalization;
using GS2Engine.GS2.ByteCode;

namespace GS2Engine
{
	public class TScript
	{
		public readonly  Dictionary<string?, FunctionParams> functions = new();
		private readonly List<TString>                       strings   = new();
		public           TScriptCom[]                        bytecode  = { };
		private          int                                 bytecodeLength;
		public           int                                 field17_0xc0;
		private          int                                 gs1flags;
		public           Dictionary<string?, TStackEntry?>   variables = new()
		{
			{"servername", new(){Type = TStackEntryType.String, Value = "Login"}}
		};

		public TScript(TString bytecodeParam) => setStream(bytecodeParam);

		private void setStream(TString bytecodeParam)
		{
			int oIndex = 0;


			reset();
			bytecodeParam.setRead(0);
			//bytecode = null;

			while (bytecodeParam.bytesLeft() > 0)
			{
				Console.WriteLine($"Bytes left: {bytecodeParam.bytesLeft()}");

				if (bytecodeParam.bytesLeft() == 1)
					if (bytecodeParam.readChar() == '\n')
						break;

				BytecodeSegment segmentType = (BytecodeSegment)bytecodeParam.readInt();

				if (segmentType is < BytecodeSegment.GS1EventFlags or > BytecodeSegment.Bytecode)
				{
					Console.Write("Segment: Unknown ({0})\n", segmentType);
					break;
				}

				Console.Write("Segment: {0}\n", segmentType.BytecodeSegmentToString());

				int segmentLength = bytecodeParam.readInt();

				TString segmentSection = bytecodeParam.readChars(segmentLength);

				switch (segmentType)
				{
					case BytecodeSegment.GS1EventFlags:
					{
						int flags = 0;
						if (3 < segmentSection.length())
							flags = segmentSection.readInt();
						gs1flags = flags;
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

								Console.Write("Function[{0}]: {1}\n", pos, functionName);
								//functionName.clear();
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

								strings.Add(stringName);
								Console.Write("String: {0}\n", stringName);
							}

						break;
					}

					case BytecodeSegment.Bytecode:
					{
						//bytecode = {} ;//(TScriptCom*)malloc(0x100 * sizeof(TScriptCom));
						TScriptCom? scriptCom = null;
						while (segmentSection.bytesLeft() > 0)
						{
							byte bytecodeByte = segmentSection.readChar();
							if (bytecodeByte is >= 0xF0 and <= 0xF6)
								Console.Write(" (Opcode: 0x{0}) ", bytecodeByte);

							switch (bytecodeByte)
							{
								case 0xF0:
								{
									byte varIndex = segmentSection.readChar();

									scriptCom.VariableName = strings[varIndex];


									Console.Write(" - variable[{0}]({1}) (byte)\n", varIndex, scriptCom.VariableName);
									break;
								}
								case 0xF1:
								{
									short varIndex = segmentSection.readShort();

									scriptCom.VariableName = strings[varIndex];

									Console.Write(" - string({0}) (word)\n", scriptCom.VariableName);
									break;
								}
								case 0xF2:
								{
									int varIndex = segmentSection.readInt();

									scriptCom.VariableName = strings[varIndex];

									Console.Write(" - string({0}) (dword)\n", scriptCom.VariableName);
									break;
								}
								case 0xF3:
								{
									byte varIndex = segmentSection.readChar();
									scriptCom.Value = varIndex;
									Console.Write(" - double({0}) (byte)\n", scriptCom.Value);
									break;
								}
								case 0xF4:
								{
									short varIndex = segmentSection.readShort();
									scriptCom.Value = varIndex;
									Console.Write(" - double({0}) (word)\n", scriptCom.Value);
									break;
								}
								case 0xF5:
								{
									int varIndex = segmentSection.readInt();
									scriptCom.Value = varIndex;
									Console.Write(" - double({0}) (dword)\n", scriptCom.Value);
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

									//byte stringLength = segmentSection.readChar();
									//var test = segmentSection.readChars(stringLength);
									scriptCom.Value = double.Parse(
										doubleString.ToString(),
										CultureInfo.InvariantCulture
									);
									Console.Write(" - double({0}) (string)\n", scriptCom.Value);
									break;
								}

								default:
								{
									if (oIndex >= bytecodeLength)
									{
										Array.Resize(ref bytecode, oIndex + 0x100);
										/*
										bytecode = (TScriptCom*)realloc(
											this->bytecode,
											(oIndex + 0x100) * sizeof(TScriptCom)
										);

										size_t oldSize = this->bytecodeLength * sizeof(TScriptCom);
										size_t newSize = (oIndex + 0x100) * sizeof(TScriptCom);

										size_t diff = newSize - oldSize;
										void* pStart = (char*)this->bytecode + oldSize;
										memset(pStart, 0, diff)
											*/

										bytecodeLength = oIndex + 0x100;
									}

									scriptCom = bytecode[oIndex] = new();
									scriptCom.BytecodeByte = bytecodeByte;
									++oIndex;
									break;
								}
							}
						}

						Console.WriteLine("Bytecode done");
						break;
					}
				}
			}


			bytecodeLength = oIndex;
			Array.Resize(ref bytecode, oIndex);

			onScriptUpdated();
		}

		private void addFunction(TString functionName, int pos, bool isPublic) =>
			functions.Add(functionName.ToString().ToLower(), new() { BytecodePosition = pos, isPublic = isPublic });

		private void reset()
		{
		}

		private void onScriptUpdated()
		{
			//fixBadByteCode();
			//checkOnlyFunctions();
			//optimizeByteCode();
		}

		private void optimizeByteCode()
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
	}
}