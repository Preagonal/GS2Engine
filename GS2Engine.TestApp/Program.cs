using System.Collections.Concurrent;
using System.Reflection;
using GS2Engine.Extensions;
using GS2Engine.GS2.Script;
using GS2Engine.Models;
using GS2Engine.TestApp.Objects;
using static GS2Engine.GS2.Script.Script;

namespace GS2Engine.TestApp;

internal static class Program
{
	private static async Task Main(string[] args)
	{
		Tools.DEBUG_ON = true;
		/*
	HashSet<Script> scripts = new();
	foreach (string file in Directory.GetFiles($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}scripts"))
	{
		Console.WriteLine($"File: {file}");
		scripts.Add(new Script(file, null, null, null));
	}

	while (true)
	{
		foreach (Script script in scripts) await script.TriggerEvent("onTimeout");

		Thread.Sleep(10);
	}
	*/
		var path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}scripts/test.gs2bc";
		Command echoCommand = delegate (ScriptMachine machine, IStackEntry[]? args)
		{
			if (args?.Length > 0)
				Console.WriteLine(machine.GetEntry(args[0]).GetValue() ?? "");
			return 0.ToStackEntry();
		};
		GlobalFunctions.AddOrUpdate(
			"echo",
			echoCommand,
			(_, _) => echoCommand
		);

		ConcurrentDictionary<int, Drawing?> Drawings = new();

		Command showimgCommand = delegate(ScriptMachine machine, IStackEntry[]? args)
		{
			if (args?.Length > 3)
				try
				{
					var     index = (int)machine.GetEntry(args[0]).GetValue<double>();
					string? image = machine.GetEntry(args[1]).GetValue<TString>() ?? string.Empty;

					var x = (int)machine.GetEntry(args[2]).GetValue<double>();
					var y = (int)machine.GetEntry(args[3]).GetValue<double>();
					if (Drawings.TryGetValue(index, out var value))
					{
						value?.ShowImg(image, x, y);
					}
					else
					{
						value = new(image, x, y);
						Drawings.AddOrUpdate(index, value, (_, _) => value);
					}
				}
				catch (Exception e)
				{
					//_logger.LogDebug(e.Message);
				}

			return 0.ToStackEntry();
		};

		Command findimgCommand = delegate(ScriptMachine machine, IStackEntry[]? args)
		{
			if (args?.Length > 0)
				try
				{
					var index = (int)machine.GetEntry(args[0]).GetValue<double>();

					if (Drawings.TryGetValue(index, out var value))
					{
						return value!.ToStackEntry();
					}
				}
				catch (Exception e)
				{
					//_logger.LogDebug(e.Message);
				}

			return 0.ToStackEntry();
		};


		GlobalFunctions.AddOrUpdate(
			"showimg",
			showimgCommand,
			(_, _) => showimgCommand
		);

		GlobalFunctions.AddOrUpdate(
			"findimg",
			findimgCommand,
			(_, _) => findimgCommand
		);

		var scriptText = """

		                 //#CLIENTSIDE
		                 function onCreated() {

		                 }
		                 		
		                 """;
		var response = Gs2Compiler.CompileCode(
			scriptText,
			"weapon",
			"testScript"
		);

		if (response.Success)
		{
			var script = new Script("testScript", response.ByteCode);

			await script.Call("onCreated");
			await script.Call("myFunction", "test", 1, true);
			await script.Call("myFunction", "test", 1, false);
			await script.Call(
				"myFunction2",
				"test",
				-0xFF,
				false,
				1.345,
				true
			);
		}
	}
}