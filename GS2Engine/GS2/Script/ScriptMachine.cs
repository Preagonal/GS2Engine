using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GS2Engine.Enums;
using GS2Engine.Exceptions;
using GS2Engine.Extensions;
using GS2Engine.GS2.ByteCode;
using GS2Engine.Models;
using Newtonsoft.Json;
using static GS2Engine.Enums.StackEntryType;

namespace GS2Engine.GS2.Script
{
	public class ScriptMachine
	{
		private readonly Script             _script;
		private readonly VariableCollection _tempVariables = new();

		private bool _firstRun = true;
		private int  _indexPos;
		private int  _scriptStackSize;
		private bool _useTemp;

		public ScriptMachine(Script script) => _script = script;

		private Dictionary<string, Script.Command> Functions => _script.ExternalFunctions;

		public async Task<IStackEntry> Execute(string functionName, Stack<IStackEntry>? callStack = null)
		{
			if (!_script.Functions.TryGetValue(functionName.ToLower(), out FunctionParams value))
				return 0.ToStackEntry();

			Stack<IStackEntry> stack = new();

			int desiredStart = value.BytecodePosition;
			int index = _firstRun ? 0 : value.BytecodePosition;
			if (_firstRun)
				_firstRun = false;

			const int maxLoopCount = 10000;

			IStackEntry? opCopy = null;
			Stack<IStackEntry> opWith = new();

			Tools.DebugLine($"Starting to execute function \"{functionName}\"");
			while (index < _script.Bytecode.Length)
			{
				int curIndex = index;
				index = curIndex + 1;
				_indexPos = index;

				ScriptCom op = _script.Bytecode[curIndex];

				Tools.Debug($"OP: {op.OpCode}");
				if (op.VariableName != null)
					Tools.Debug($" - var: {op.VariableName}");
				if (op.Value != 0)
					Tools.Debug($" - val: {op.Value}");
				Tools.DebugLine("");

				switch (op.OpCode)
				{
					default:
					case Opcode.OP_NONE:
						break;
					case Opcode.OP_SET_INDEX:
						// ReSharper disable once CompareOfFloatsByEqualityOperator
						if (op.Value == _script.Bytecode.Length)
						{
							index = desiredStart;
							desiredStart = (int)op.Value;
						}
						else
						{
							index = (int)op.Value;
						}

						_indexPos = index;
						break;
					case Opcode.OP_SET_INDEX_TRUE:
						curIndex = _scriptStackSize;
						if (curIndex < 0) return 1.ToStackEntry();
						object? sitCompVar = GetEntry(stack.Pop()).GetValue();

						bool sitCompare = sitCompVar is bool var
							? var
							: sitCompVar is double && sitCompVar.Equals((double)1);
						if (!sitCompare)
						{
							_scriptStackSize = curIndex + -1;
						}
						else
						{
							index = (int)op.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_OR:
						curIndex = _scriptStackSize;
						if (curIndex < 0) return 1.ToStackEntry();

						object? orCompVar = GetEntry(stack.Pop()).GetValue();

						bool orCompare = orCompVar is bool compVar
							? compVar
							: orCompVar is double && orCompVar.Equals((double)1);

						if (!orCompare)
						{
							index = (int)op.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_IF:
						curIndex = stack.Count;
						if (curIndex < 0) return 1.ToStackEntry();

						object? ifCompVar = GetEntry(stack.Pop()).GetValue();

						bool ifCompare = ifCompVar is bool b ? b : ifCompVar is double && ifCompVar.Equals((double)1);

						if (!ifCompare)
						{
							index = (int)op.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_AND:
						if (_scriptStackSize < 0) return 1.ToStackEntry();

						object? andCompVar = GetEntry(stack.Pop()).GetValue();

						bool andCompare = andCompVar is bool var1
							? var1
							: andCompVar is double && andCompVar.Equals((double)1);

						if (!andCompare)
						{
							index = (int)op.Value;
							_scriptStackSize = curIndex + -1;
							_indexPos = index;
						}

						break;
					case Opcode.OP_CALL:
						IStackEntry? callEntry = GetEntry(stack.Pop());
						object? cmd = getEntryValue<object>(callEntry);

						Stack<IStackEntry> parameters = stack.Clone();
						while (stack.Peek()?.Type != ArrayStart) stack.Pop();
						stack.Pop();
						bool opWithHasFunc = false;
						if (opWith?.Any() ?? false)
							opWithHasFunc =
								opWith?.Peek()
								      ?.GetValue<VariableCollection>()
								      ?.ContainsVariable(cmd?.ToString()?.ToLower() ?? string.Empty) ??
								false;
						switch (callEntry.Type)
						{
							case StackEntryType.String or Variable when opWithHasFunc:
								IStackEntry funcRet =
									opWith?.Peek()
									      ?.GetValue<VariableCollection>()
									      ?.GetVariable(cmd?.ToString()?.ToLower() ?? string.Empty)
									      ?.GetValue<Script.Command>()
									      ?.Invoke(this, parameters.ToArray()) ??
									0.ToStackEntry();
								if (funcRet.GetValue() is not (double)0)
									stack.Push(funcRet);
								break;
							case StackEntryType.String or Variable
								when _script.Functions.ContainsKey(cmd?.ToString().ToLower() ?? string.Empty):
								stack.Push(await Execute(cmd?.ToString().ToLower() ?? string.Empty, parameters));
								break;
							case StackEntryType.String or Variable when Functions.TryGetValue(
								cmd?.ToString().ToLower() ?? string.Empty,
								out Script.Command command
							):
								stack.Push(command.Invoke(this, parameters.ToArray()));
								break;
							case StackEntryType.String or Variable:
								stack.Push(0.ToStackEntry());
								break;
							case Function:
								stack.Push(
									(cmd as Script.Command)?.Invoke(this, parameters.ToArray()) ?? 0.ToStackEntry()
								);
								break;
							default:
								stack.Push(0.ToStackEntry());
								break;
						}

						break;
					case Opcode.OP_RET:
						IStackEntry ret = 0.ToStackEntry();
						if (stack.Count > 0)
							ret = stack.Pop();
						Tools.DebugLine(
							JsonConvert.SerializeObject(Script.GlobalVariables.GetDictionary(), Formatting.Indented)
						);
						return ret;
					case Opcode.OP_SLEEP:
						double sleep = getEntryValue<double>(stack.Pop());
						sleep *= 1000;
						await Task.Delay((int)sleep);
						break;
					case Opcode.OP_CMD_CALL:
						//index = _script.Field170Xc0;
						if ((int)op.Value == index)
						{
							if (maxLoopCount <= op.LoopCount &&
							    !functionName.Equals("onTimeout", StringComparison.CurrentCultureIgnoreCase))
								throw new ScriptException("Loop limit exceeded");

							op.LoopCount += 1;
							index = _indexPos;
						}
						else
						{
							op.Value = index;
							op.VariableName = null;
							index = _indexPos;
						}

						break;
					case Opcode.OP_JMP:

					{
						//index = indexPos;
					}

						break;
					case Opcode.OP_TYPE_NUMBER:
						stack.Push(op.Value.ToStackEntry());
						break;
					case Opcode.OP_TYPE_STRING:
						stack.Push((op.VariableName ?? "").ToStackEntry());
						break;
					case Opcode.OP_TYPE_VAR:
						stack.Push((op.VariableName ?? "").ToStackEntry(true));
						break;
					case Opcode.OP_TYPE_ARRAY:
						stack.Push(new StackEntry(ArrayStart, null));
						break;

					case Opcode.OP_TYPE_TRUE:
						stack.Push(true.ToStackEntry());
						break;
					case Opcode.OP_TYPE_FALSE:
						stack.Push(false.ToStackEntry());
						break;
					case Opcode.OP_TYPE_NULL:
						stack.Push(0.ToStackEntry());
						break;
					case Opcode.OP_PI:
						stack.Push(Math.PI.ToStackEntry());
						break;
					case Opcode.OP_COPY_LAST_OP:
						stack.Push(stack.Peek());
						break;
					case Opcode.OP_SWAP_LAST_OPS:
						IStackEntry? stackSwap1 = stack.Pop();
						IStackEntry? stackSwap2 = stack.Pop();
						stack.Push(stackSwap1);
						stack.Push(stackSwap2);
						break;
					case Opcode.OP_INDEX_DEC:
						break;
					case Opcode.OP_CONV_TO_FLOAT:
						object? test = getEntryValue<object>(stack.Pop());

						double convToFloatVal;
						if (test?.GetType() == typeof(TString) &&
						    double.TryParse((TString)test, out double convToFloatParse))
							convToFloatVal = convToFloatParse;
						else if (test?.GetType() == typeof(bool))
							convToFloatVal = (bool)test ? 1 : 0;
						else if (test?.GetType() == typeof(double))
							convToFloatVal = (double)test;
						else
							convToFloatVal = 0;


						stack.Push(convToFloatVal.ToStackEntry());
						break;
					case Opcode.OP_CONV_TO_STRING:
						stack.Push(getEntryValue<object>(stack.Pop())?.ToString().ToStackEntry() ?? "".ToStackEntry());
						break;
					case Opcode.OP_MEMBER_ACCESS:
						IStackEntry? stackVal = stack.Pop();
						TString? memberAccessParam = getEntryValue<TString>(stackVal, StackEntryType.String);

						try
						{
							VariableCollection? memberAccessObject = getEntryValue<VariableCollection>(stack.Pop());
							IStackEntry? member = memberAccessObject?.GetVariable(memberAccessParam ?? "");
							stack.Push(member ?? 0.ToStackEntry());
						}
						catch (Exception e)
						{
							Tools.DebugLine(e.Message);
							stack.Push(0.ToStackEntry());
						}

						break;
					case Opcode.OP_CONV_TO_OBJECT:
						IStackEntry convEntry = GetEntry(stack.Pop());
						if (convEntry.Type is StackEntryType.String or Variable)
							stack.Push(
								(opWith.Any()
									? opWith?.Peek()
									        ?.GetValue<VariableCollection>()
									        ?.GetVariable(getEntryValue<TString>(convEntry)?.ToLower() ?? string.Empty)
									: GetEntry(convEntry, Variable))!
							);
						else
							stack.Push(convEntry);

						break;
					case Opcode.OP_ARRAY_END:

						List<object> stackArr = new();
						//keep popping the stack till we hit an array start
						while (stack.Count > 0 && stack.Peek().Type != ArrayStart)
							stackArr.Add(GetEntry(stack.Pop())?.GetValue() ?? 0.ToStackEntry());
						stack.Pop(); //pop array start marker off

						stack.Push(new StackEntry(StackEntryType.Array, stackArr)); //push new array onto stack
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
						IStackEntry? newObject = stack.Pop();
						IStackEntry? newObjectParam = stack.Pop();
						try
						{
							VariableCollection? newObjectRet = (VariableCollection)GetInstance(
								getEntryValue<TString>(newObject) ?? string.Empty,
								getEntryValue<object>(newObjectParam)?.ToString()!
							)!;
							stack.Push(newObjectRet?.ToStackEntry() ?? 0.ToStackEntry());
						}
						catch (Exception e)
						{
							Tools.DebugLine(e.Message);
							stack.Push(0.ToStackEntry());
						}

						break;
					case Opcode.OP_INLINE_CONDITIONAL:
						break;
					case Opcode.OP_ASSIGN:
						IStackEntry val = stack.Pop();

						IStackEntry variable = (stack.Count == 0 ? opCopy : GetEntry(stack.Pop())) ?? 0.ToStackEntry();
						if (variable.Type != Variable /*StackEntryType.String or Number val.Type*/)
						{
							variable.SetValue(val.GetValue());
						}
						else if (opWith.Any())
						{
							try
							{
								opWith?.Peek()
								      ?.GetValue<VariableCollection>()
								      ?.AddOrUpdate((variable.GetValue() ?? "").ToString().ToLower(), val);
							}
							catch (Exception e)
							{
								Tools.DebugLine(e.Message);
							}
						}
						else if (!_useTemp)
						{
							Script.GlobalVariables.AddOrUpdate((variable.GetValue() ?? "").ToString().ToLower(), val);
						}
						else
						{
							_useTemp = false;
							_tempVariables.AddOrUpdate((variable.GetValue() ?? "").ToString().ToLower(), val);
						}

						break;
					case Opcode.OP_FUNC_PARAMS_END:
						while (stack.Count > 0)
						{
							IStackEntry funcParam = stack.Pop();
							try
							{
								IStackEntry? funcParamVal = callStack?.Pop();
								_tempVariables.AddOrUpdate(
									(funcParam.GetValue() ?? "").ToString().ToLower(),
									funcParamVal ?? 0.ToStackEntry()
								);
							}
							catch (Exception e)
							{
								// ignored
								Tools.DebugLine(e.Message);
							}
						}

						index = _indexPos;
						break;
					case Opcode.OP_INC:
						try
						{
							IStackEntry incVar = GetEntry(stack.Pop());

							double incVal = stack.Any()
								? getEntryValue<double>(stack.Pop())
								: getEntryValue<double>(incVar);
							if (incVar.Type == Number) incVar.SetValue(incVal + 1);

							stack.Push(((double?)incVar.GetValue())?.ToStackEntry() ?? 0.ToStackEntry());
						}
						catch (Exception e)
						{
							Tools.DebugLine(e.Message);
							stack.Push(0.ToStackEntry());
						}

						break;
					case Opcode.OP_DEC:
						IStackEntry decVar = GetEntry(stack.Pop());
						double decVal =
							stack.Any() ? getEntryValue<double>(stack.Pop()) : getEntryValue<double>(decVar);
						if (decVar.Type == Number) decVar.SetValue(decVal - 1);

						stack.Push(((double?)decVar.GetValue())?.ToStackEntry() ?? 0.ToStackEntry());
						break;
					case Opcode.OP_ADD:
						double addA = getEntryValue<double>(stack.Pop());
						double addB = getEntryValue<double>(stack.Pop());
						stack.Push((addB + addA).ToStackEntry());
						break;
					case Opcode.OP_SUB:
						double subA = getEntryValue<double>(stack.Pop());
						double subB = getEntryValue<double>(stack.Pop());
						stack.Push((subB - subA).ToStackEntry());
						break;
					case Opcode.OP_MUL:
						double mulA = getEntryValue<double>(stack.Pop());
						double mulB = getEntryValue<double>(stack.Pop());
						stack.Push((mulB * mulA).ToStackEntry());
						break;
					case Opcode.OP_DIV:
						double divA = getEntryValue<double>(stack.Pop());
						double divB = getEntryValue<double>(stack.Pop());
						stack.Push((divB / divA).ToStackEntry());
						break;
					case Opcode.OP_MOD:
						double modA = getEntryValue<double>(stack.Pop());
						double modB = getEntryValue<double>(stack.Pop());
						stack.Push((modB % modA).ToStackEntry());
						break;
					case Opcode.OP_POW:
						double powA = getEntryValue<double>(stack.Pop());
						double powB = getEntryValue<double>(stack.Pop());
						stack.Push(Math.Pow(powB, powA).ToStackEntry());
						break;
					case Opcode.OP_NOT:
						break;
					case Opcode.OP_UNARYSUB:
						break;
					case Opcode.OP_EQ:
						object? eq1 = getEntryValue<object?>(stack.Pop());
						object? eq2 = getEntryValue<object?>(stack.Pop());
						stack.Push((eq1 ?? false).Equals(eq2).ToStackEntry());
						break;
					case Opcode.OP_NEQ:
						object? neq1 = getEntryValue<object?>(stack.Pop());
						object? neq2 = getEntryValue<object?>(stack.Pop());
						stack.Push((!(neq1 ?? false).Equals(neq2)).ToStackEntry());
						break;
					case Opcode.OP_LT:
						double ltA = getEntryValue<double>(stack.Pop());
						double ltB = getEntryValue<double>(stack.Pop());
						stack.Push((ltB < ltA).ToStackEntry());
						break;
					case Opcode.OP_GT:
						double gtA = getEntryValue<double>(stack.Pop());
						double gtB = getEntryValue<double>(stack.Pop());
						stack.Push((gtB > gtA).ToStackEntry());
						break;
					case Opcode.OP_LTE:
						double lteA = getEntryValue<double>(stack.Pop());
						double lteB = getEntryValue<double>(stack.Pop());
						stack.Push((lteB <= lteA).ToStackEntry());
						break;
					case Opcode.OP_GTE:
						double gteA = getEntryValue<double>(stack.Pop());
						double gteB = getEntryValue<double>(stack.Pop());
						stack.Push((gteB >= gteA).ToStackEntry());
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
						IStackEntry format = stack.Pop();
						object?[] objects = stack.Select(x => getEntryValue<object>(x)).ToArray();
						stack.Clear();
						string formatted = Tools.Format(getEntryValue<TString>(format) ?? "", objects);
						stack.Push(formatted.ToStackEntry());
						break;
					case Opcode.OP_INT:
						break;
					case Opcode.OP_ABS:
						stack.Push(Math.Abs(getEntryValue<double>(stack.Pop())).ToStackEntry());
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
						TString? joinA = getEntryValue<TString>(stack.Pop());
						TString? joinB = getEntryValue<TString>(stack.Pop());
						stack.Push($"{joinB}{joinA}".ToStackEntry());
						break;
					case Opcode.OP_OBJ_CHARAT:
						break;
					case Opcode.OP_OBJ_SUBSTR:
						break;
					case Opcode.OP_OBJ_STARTS:
						TString obj = getEntryValue<TString>(stack.Pop()) ?? "";
						TString startsWith = getEntryValue<TString>(stack.Pop()) ?? "";
						stack.Push(
							obj.StartsWith(startsWith, StringComparison.CurrentCultureIgnoreCase).ToStackEntry()
						);
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
						object? objSizeVar = GetEntry(stack.Pop()).GetValue();

						if (objSizeVar is VariableCollection vc)
							stack.Push(vc.GetDictionary().Count.ToStackEntry());
						else if (objSizeVar is List<string> ls)
							stack.Push(ls.Count.ToStackEntry());
						else if (objSizeVar is List<object> lo)
							stack.Push(lo.Count.ToStackEntry());
						else
							stack.Push(0.ToStackEntry());

						break;
					case Opcode.OP_ARRAY:
						double arrayIndex = getEntryValue<double>(stack.Pop());
						object? array = getEntryValue<object>(stack.Pop());

						if (array?.GetType() == typeof(List<string>))
							stack.Push(
								((List<string>?)array)?[(int)arrayIndex].ToStackEntry() ?? new object().ToStackEntry()
							);
						else if (array?.GetType() == typeof(List<int>))
							stack.Push(
								((List<int>?)array)?[(int)arrayIndex].ToStackEntry() ?? new object().ToStackEntry()
							);
						else
							stack.Push(
								((List<object>?)array)?[(int)arrayIndex].ToStackEntry() ?? new object().ToStackEntry()
							);

						break;
					case Opcode.OP_ARRAY_ASSIGN:
						try
						{
							IStackEntry arrAssVal = GetEntry(stack.Pop());
							double arrAssIndex = GetEntry(stack.Pop()).GetValue<double>();
							VariableCollection? arrAssObj = GetEntry(stack.Pop()).GetValue<VariableCollection>();
							arrAssObj?.AddOrUpdate(((int)arrAssIndex).ToString(), arrAssVal);
						}
						catch (Exception e)
						{
							Tools.DebugLine(e.Message);
						}

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
						opWith?.Push(stack.Pop());
						break;
					case Opcode.OP_WITHEND:
						opWith?.Pop(); //stack.Push();
						break;
					case Opcode.OP_FOREACH:
						break;
					case Opcode.OP_THIS:
						stack.Push(new StackEntry(StackEntryType.Array, Script.GlobalVariables));
						break;
					case Opcode.OP_THISO:
						break;
					case Opcode.OP_PLAYER:
						//_script.Objects[p]
						stack.Push(
							_script.GlobalObjects.ContainsKey("player")
								? new(Player, _script.GlobalObjects["player"])
								: 0.ToStackEntry()
						);
						break;
					case Opcode.OP_PLAYERO:
						break;
					case Opcode.OP_LEVEL:
						break;
					case Opcode.OP_TEMP:
						stack.Push(new StackEntry(StackEntryType.Array, _tempVariables));
						_useTemp = true;
						break;
					case Opcode.OP_PARAMS:
						break;
					case Opcode.OP_NUM_OPS:
						break;
				}
			}

			return 0.ToStackEntry();
		}

		private object? GetInstance(string className, string arg)
		{
			if (string.IsNullOrEmpty(className)) return null;
			if (className.EndsWith("Profile", StringComparison.CurrentCultureIgnoreCase))
				return new GuiControl(arg, _script);

			Type? type = Type.GetType(className);
			if (type != null)
				return Activator.CreateInstance(type, arg, _script);
			foreach (Assembly? asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = asm.GetTypes()
				          .FirstOrDefault(x => x.Name.Equals(className, StringComparison.CurrentCultureIgnoreCase)) ??
				       null;
				if (type != null)
					return Activator.CreateInstance(type, arg, _script);
			}

			throw new ScriptException($"Missing Class: {className}");
		}

		public IStackEntry GetEntry(IStackEntry stackEntry, StackEntryType? overrideStackType = null)
		{
			StackEntryType? type = overrideStackType ?? stackEntry.Type;
			switch (type)
			{
				case Variable
					when _tempVariables.ContainsVariable(stackEntry.GetValue()?.ToString()?.ToLower() ?? string.Empty):
					_useTemp = false;
					return _tempVariables.GetVariable(stackEntry.GetValue()?.ToString()?.ToLower() ?? string.Empty);
				case Variable
					when Script.GlobalVariables.ContainsVariable(
						stackEntry.GetValue()?.ToString()?.ToLower() ?? string.Empty
					):
					return Script.GlobalVariables[stackEntry.GetValue()?.ToString()?.ToLower() ?? string.Empty];
				case Variable when _script != null &&
				                   _script.GlobalObjects.ContainsKey(
					                   stackEntry.GetValue()?.ToString()?.ToLower() ?? string.Empty
				                   ):
					return _script.GlobalObjects[stackEntry.GetValue()?.ToString()?.ToLower() ?? string.Empty]
					              .ToStackEntry();
				default:
					return stackEntry;
			}
		}

		private T? getEntryValue<T>(IStackEntry stackEntry, StackEntryType? overrideStackType = null) =>
			(T?)GetEntry(stackEntry, overrideStackType).GetValue();

		public void Reset()
		{
			_firstRun = true;
			_tempVariables.Clear();
		}
	}
}