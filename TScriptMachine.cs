using GS2Engine.GS2.ByteCode;
using Newtonsoft.Json;
using static GS2Engine.TStackEntryType;

namespace GS2Engine
{
	public class TScriptMachine
	{
		private static          TScript?                         _script;
		private static readonly Dictionary<string, TStackEntry?> TempVariables = new();
		private static          bool                             _useTemp;


		private readonly Dictionary<string, Command?> _functions = new()
		{
			{
				"echo", delegate(TStackEntry[] args)
				{
					Console.WriteLine(
						args[0].Type == Variable ? _script?.variables[args[0].Value.ToString()].Value : args[0].Value
					);
					return new() { Type = Number, Value = 0 };
				}
			}
		};

		private bool _firstRun = true;

		private int _indexPos;
		private int _scriptStackSize;

		public TScriptMachine(TScript? script) => _script = script;

		private void Debug(string? text)
		{
#if DEBUG
			Console.Write(text);
#endif
		}

		private void DebugLine(string? text)
		{
#if DEBUG
			Console.WriteLine(text);
#endif
		}

		public async Task<TStackEntry?> execute(string? functionName, Stack<TStackEntry>? callStack = null)
		{
			if (!_script.functions.TryGetValue(functionName?.ToLower(), out FunctionParams value)) return null;

			Stack<TStackEntry?> stack = new();

			int desiredStart = value.BytecodePosition;
			int index = _firstRun ? 0 : value.BytecodePosition;
			if (_firstRun)
				_firstRun = false;

			int iVar18 = 0;
			const int maxLoopCount = 10000;

			DebugLine($"Starting to execute function \"{functionName}\"");
			while (index < _script.bytecode.Length)
			{
				LAB_003381c0:
				int curIndex = index;
				index = curIndex + 1;
				iVar18 = curIndex * 0x20;
				_indexPos = index;


				TScriptCom bytecodeop = _script.bytecode[curIndex];

				Debug($"OP: {(Opcode)bytecodeop.BytecodeByte}");
				if (bytecodeop.VariableName != null)
					Debug($" - var: {bytecodeop.VariableName}");
				if (bytecodeop.Value != 0)
					Debug($" - val: {bytecodeop.Value}");
				DebugLine("");

				switch ((Opcode)bytecodeop.BytecodeByte)
				{
					default:
					case Opcode.OP_NONE:
						break;
					case Opcode.OP_SET_INDEX:
						if (bytecodeop.Value == _script.bytecode.Length)
						{
							index = desiredStart;
							desiredStart = (int)bytecodeop.Value;
						}
						else
						{
							index = (int)bytecodeop.Value;
						}

						_indexPos = index;
						break;
					case Opcode.OP_SET_INDEX_TRUE:
						curIndex = _scriptStackSize;
						if (curIndex < 0) return new() { Type = Number, Value = 1 };
						object? sitCompVar = getEntry(stack.Pop()).Value;
						
						bool sitCompare = sitCompVar.GetType() == typeof(bool)?(bool)sitCompVar:sitCompVar is double && sitCompVar.Equals((double)1);
						if (!sitCompare)
						{
							_scriptStackSize = curIndex + -1;
						}
						else
						{
							index = (int)bytecodeop.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						goto LAB_003381c0;
						break;
					case Opcode.OP_OR:
						curIndex = _scriptStackSize;
						if (curIndex < 0) return new() { Type = Number, Value = 1 };

						object? orCompVar = getEntry(stack.Pop()).Value;
						
						bool orCompare = orCompVar.GetType() == typeof(bool)?(bool)orCompVar:orCompVar is double && orCompVar.Equals((double)1);

						if (!orCompare)
						{
							index = (int)bytecodeop.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_IF:
						curIndex = _scriptStackSize;
						if (curIndex < 0) return new() { Type = Number, Value = 1 };

						object? ifCompVar = getEntry(stack.Pop()).Value;
						
						bool ifCompare = ifCompVar.GetType() == typeof(bool)?(bool)ifCompVar:ifCompVar is double && ifCompVar.Equals((double)1);

						if (!ifCompare)
						{
							index = (int)bytecodeop.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_AND:
						if (_scriptStackSize < 0) return new() { Type = Number, Value = 1 };

						object? andCompVar = getEntry(stack.Pop()).Value;
						
						bool andCompare = andCompVar.GetType() == typeof(bool)?(bool)andCompVar:andCompVar is double && andCompVar.Equals((double)1);

						if (!andCompare)
						{
							index = (int)bytecodeop.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_CALL:
						Stack<TStackEntry?> parameters = new();
						TStackEntry? cmd = stack.Pop();
						parameters = stack.Clone();
						stack.Clear();
						if (_script.functions.ContainsKey(cmd?.Value.ToString()))
							stack.Push(await execute(cmd?.Value.ToString()?.ToLower(), parameters));
						else if (_functions.TryGetValue(cmd?.Value.ToString()?.ToLower(), out Command? command))
							stack.Push(command?.Invoke(parameters.ToArray()));
						else stack.Push(new() { Type = Number, Value = 0 });

						break;
					case Opcode.OP_RET:
						TStackEntry? ret = new() { Type = Number, Value = 0 };
						if (stack.Count > 0)
							ret = stack.Pop();
						DebugLine(JsonConvert.SerializeObject(_script.variables, Formatting.Indented));
						return ret;
					case Opcode.OP_SLEEP:
						double sleep = (double)(getEntry(stack.Pop())?.Value ?? 0);
						sleep *= 1000;
						await Task.Delay((int)sleep);
						break;
					case Opcode.OP_CMD_CALL:
						index = _script.field17_0xc0;
						if ((int)bytecodeop.Value == index)
						{
							if (maxLoopCount <= bytecodeop.LoopCount) throw new ScriptException("Loop limit exceeded");

							bytecodeop.LoopCount += 1;
							index = _indexPos;
						}
						else
						{
							bytecodeop.Value = index;
							bytecodeop.VariableName = null;
							index = _indexPos;
						}

						break;
					case Opcode.OP_JMP:

					{
						//index = indexPos;
					}

						break;
					case Opcode.OP_TYPE_NUMBER:
						stack.Push(new() { Type = Number, Value = bytecodeop.Value });
						break;
					case Opcode.OP_TYPE_STRING:
						stack.Push(new() { Type = TStackEntryType.String, Value = bytecodeop.VariableName });
						break;
					case Opcode.OP_TYPE_VAR:
						stack.Push(new() { Type = Variable, Value = bytecodeop.VariableName });
						break;
					case Opcode.OP_TYPE_ARRAY:
						index = _indexPos;

						goto LAB_003381c0;

					case Opcode.OP_TYPE_TRUE:
						break;
					case Opcode.OP_TYPE_FALSE:
						break;
					case Opcode.OP_TYPE_NULL:
						break;
					case Opcode.OP_PI:
						break;
					case Opcode.OP_COPY_LAST_OP:
						break;
					case Opcode.OP_SWAP_LAST_OPS:
						break;
					case Opcode.OP_INDEX_DEC:
						break;
					case Opcode.OP_CONV_TO_FLOAT:
						object? test = getEntry(stack.Pop())?.Value;

						double convToFloatVal = 0;
						if (test?.GetType() == typeof(TString))
						{
							convToFloatVal = double.Parse((TString)test);
						}
						else if (test?.GetType() == typeof(bool))
						{
							convToFloatVal = (bool)test ? 1 : 0;
						}
						else
						{
							convToFloatVal = (double)test;
						}
						

						stack.Push(new() { Type = Number, Value = convToFloatVal });
						break;
					case Opcode.OP_CONV_TO_STRING:
						break;
					case Opcode.OP_MEMBER_ACCESS:
						break;
					case Opcode.OP_CONV_TO_OBJECT:
						break;
					case Opcode.OP_ARRAY_END:
						break;
					case Opcode.OP_ARRAY_NEW:
						break;
					case Opcode.OP_SETARRAY:
						break;
					case Opcode.OP_INLINE_NEW:
						break;
					case Opcode.OP_MAKEVAR:
						break;
					case Opcode.OP_NEW_OBJECT:
						break;
					case Opcode.OP_INLINE_CONDITIONAL:
						break;
					case Opcode.OP_ASSIGN:
						TStackEntry? val = stack.Pop();
						TStackEntry? vari = getEntry(stack.Pop());
						if (vari.Type != Variable)
						{
							vari.Value = val.Value;
						}
						else if (!_useTemp)
						{
							_script.variables.Add(vari.Value.ToString()?.ToLower(), val);
						}
						else
						{
							_useTemp = false;
							TempVariables.Add(vari.Value.ToString()?.ToLower(), val);
						}

						break;
					case Opcode.OP_FUNC_PARAMS_END:
						while (stack.Count > 0)
						{
							TStackEntry? funcParam = stack.Pop();
							TStackEntry? funcParamVal = null;
							try
							{
								funcParamVal = callStack?.Pop();
							}
							catch (Exception)
							{
								// ignored
							}

							TempVariables.Add(
								funcParam.Value.ToString()?.ToLower(),
								funcParamVal ?? new() { Type = Number, Value = 0 }
							);
						}

						index = _indexPos;
						break;
					case Opcode.OP_INC:
						break;
					case Opcode.OP_DEC:
						break;
					case Opcode.OP_ADD:
						double addA = (double)getEntry(stack.Pop()).Value;
						double addB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = Number, Value = addB + addA });
						break;
					case Opcode.OP_SUB:
						double subA = (double)getEntry(stack.Pop()).Value;
						double subB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = Number, Value = subB - subA });
						break;
					case Opcode.OP_MUL:
						double mulA = (double)getEntry(stack.Pop()).Value;
						double mulB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = Number, Value = mulB * mulA });
						break;
					case Opcode.OP_DIV:
						double divA = (double)getEntry(stack.Pop()).Value;
						double divB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = Number, Value = divB / divA });
						break;
					case Opcode.OP_MOD:
						double modA = (double)getEntry(stack.Pop()).Value;
						double modB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = Number, Value = modB % modA });
						break;
					case Opcode.OP_POW:
						double powA = (double)getEntry(stack.Pop()).Value;
						double powB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = Number, Value = Math.Pow(powB, powA) });
						break;
					case Opcode.OP_NOT:
						break;
					case Opcode.OP_UNARYSUB:
						break;
					case Opcode.OP_EQ:
						object? eq1 = getEntry(stack.Pop()).Value;
						object? eq2 = getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = TStackEntryType.Boolean, Value = eq1.Equals(eq2) });
						break;
					case Opcode.OP_NEQ:
						object? neq1 = getEntry(stack.Pop()).Value;
						object? neq2 = getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = TStackEntryType.Boolean, Value = !neq1.Equals(neq2) });
						break;
					case Opcode.OP_LT:
						double ltA = (double)getEntry(stack.Pop()).Value;
						double ltB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = TStackEntryType.Boolean, Value = ltB < ltA });
						break;
					case Opcode.OP_GT:
						double gtA = (double)getEntry(stack.Pop()).Value;
						double gtB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = TStackEntryType.Boolean, Value = gtB > gtA });
						break;
					case Opcode.OP_LTE:
						double lteA = (double)getEntry(stack.Pop()).Value;
						double lteB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = TStackEntryType.Boolean, Value = lteB <= lteA });
						break;
					case Opcode.OP_GTE:
						double gteA = (double)getEntry(stack.Pop()).Value;
						double gteB = (double)getEntry(stack.Pop()).Value;
						stack.Push(new() { Type = TStackEntryType.Boolean, Value = gteB >= gteA });
						break;
					case Opcode.OP_BWO:
						break;
					case Opcode.OP_BWA:
						break;
					case Opcode.OP_IN_RANGE:
						break;
					case Opcode.OP_IN_OBJ:
						break;
					case Opcode.OP_OBJ_INDEX:
						break;
					case Opcode.OP_OBJ_TYPE:
						break;
					case Opcode.OP_FORMAT:
						break;
					case Opcode.OP_INT:
						break;
					case Opcode.OP_ABS:
						break;
					case Opcode.OP_RANDOM:
						break;
					case Opcode.OP_SIN:
						break;
					case Opcode.OP_COS:
						break;
					case Opcode.OP_ARCTAN:
						break;
					case Opcode.OP_EXP:
						break;
					case Opcode.OP_LOG:
						break;
					case Opcode.OP_MIN:
						break;
					case Opcode.OP_MAX:
						break;
					case Opcode.OP_GETANGLE:
						break;
					case Opcode.OP_GETDIR:
						break;
					case Opcode.OP_VECX:
						break;
					case Opcode.OP_VECY:
						break;
					case Opcode.OP_OBJ_INDICES:
						break;
					case Opcode.OP_OBJ_LINK:
						break;
					case Opcode.OP_CHAR:
						break;
					case Opcode.OP_OBJ_TRIM:
						break;
					case Opcode.OP_OBJ_LENGTH:
						break;
					case Opcode.OP_OBJ_POS:
						break;
					case Opcode.OP_JOIN:
						break;
					case Opcode.OP_OBJ_CHARAT:
						break;
					case Opcode.OP_OBJ_SUBSTR:
						break;
					case Opcode.OP_OBJ_STARTS:
						object? obj = getEntry(stack.Pop())?.Value;
						object? startsWith = getEntry(stack.Pop())?.Value;
						stack.Push(new(){Type = TStackEntryType.Boolean, Value = obj.ToString().StartsWith(startsWith.ToString(), StringComparison.CurrentCultureIgnoreCase) });
						break;
					case Opcode.OP_OBJ_ENDS:
						break;
					case Opcode.OP_OBJ_TOKENIZE:
						break;
					case Opcode.OP_TRANSLATE:
						break;
					case Opcode.OP_OBJ_POSITIONS:
						break;
					case Opcode.OP_OBJ_SIZE:
						break;
					case Opcode.OP_ARRAY:
						break;
					case Opcode.OP_ARRAY_ASSIGN:
						break;
					case Opcode.OP_ARRAY_MULTIDIM:
						break;
					case Opcode.OP_ARRAY_MULTIDIM_ASSIGN:
						break;
					case Opcode.OP_OBJ_SUBARRAY:
						break;
					case Opcode.OP_OBJ_ADDSTRING:
						break;
					case Opcode.OP_OBJ_DELETESTRING:
						break;
					case Opcode.OP_OBJ_REMOVESTRING:
						break;
					case Opcode.OP_OBJ_REPLACESTRING:
						break;
					case Opcode.OP_OBJ_INSERTSTRING:
						break;
					case Opcode.OP_OBJ_CLEAR:
						break;
					case Opcode.OP_ARRAY_NEW_MULTIDIM:
						break;
					case Opcode.OP_WITH:
						break;
					case Opcode.OP_WITHEND:
						break;
					case Opcode.OP_FOREACH:
						break;
					case Opcode.OP_THIS:
						break;
					case Opcode.OP_THISO:
						break;
					case Opcode.OP_PLAYER:
						break;
					case Opcode.OP_PLAYERO:
						break;
					case Opcode.OP_LEVEL:
						break;
					case Opcode.OP_TEMP:
						_useTemp = true;
						break;
					case Opcode.OP_PARAMS:
						break;
					case Opcode.OP_NUM_OPS:
						break;
				}
			}

			return null;
		}

		private static TStackEntry? getEntry(TStackEntry? stackEntry)
		{
			switch (stackEntry?.Type)
			{
				case Variable when TempVariables.ContainsKey(stackEntry?.Value?.ToString()?.ToLower()):
					_useTemp = false;
					return TempVariables[stackEntry?.Value?.ToString()?.ToLower()];
				case Variable when _script.variables.ContainsKey(stackEntry?.Value?.ToString()?.ToLower()):
					return _script.variables[stackEntry?.Value?.ToString()?.ToLower()];
				default:
					return stackEntry;
			}
		}

		private delegate TStackEntry Command(TStackEntry?[] args);
	}
}