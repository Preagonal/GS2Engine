// See https://aka.ms/new-console-template for more information

using System.Reflection;
using GS2Engine;
using GS2Engine.GS2.Script;

HashSet<Script> scripts = new();
foreach (string file in Directory.GetFiles(
	         $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}scripts"
         ))
{
	Console.WriteLine($"File: {file}");
	scripts.Add(new(file, null, null, null));
}


while (true)
{
	foreach (Script script in scripts) await script.TriggerEvent("onTimeout");

	Thread.Sleep(10);
}

//await scripts.FirstOrDefault(x => x.Name.Equals("shared")).execute("encrypt").ConfigureAwait(false);

/*
Script script = new Script(File.ReadAllBytes("shared.gs2bc"));

Console.WriteLine(JsonConvert.SerializeObject(script.Functions, Formatting.Indented));
Console.WriteLine((await script.execute("onCreated"))?.Value);
Stack<StackEntry> test = new();
test.Push(new() {Type = StackEntryType.String, Value = "hello"});
Console.WriteLine((await script.execute("encrypt", test))?.Value);
*/