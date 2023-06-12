using System.Reflection;
using GS2Engine;
using GS2Engine.Extensions;
using GS2Engine.GS2.Script;
using GS2Engine.Models;
using static GS2Engine.GS2.Script.Script;

internal class Program
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
		string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}scripts/test.gs2bc";
		Dictionary<string, Command> commands = new() {
			{
				"echo",
				delegate (ScriptMachine machine, IStackEntry[]? args)
				{
					if (args?.Length > 0)
						Console.WriteLine(machine.GetEntry(args[0]).GetValue() ?? "");
					return 0.ToStackEntry();
				}
			}
		};
		Script script = new(path, null, null, commands);
		
		await script.Call("myFunction", new object[] { "test", 1, true });
		await script.Call("myFunction", new object[] { "test", 1, false });
		await script.Call("myFunction2", new object[] { "test", -0xFF, false, 1.345, true });
		
	}
}