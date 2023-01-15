// See https://aka.ms/new-console-template for more information

using GS2Engine;

TScript script = new(File.ReadAllBytes("test.gs2bc"));

TScriptMachine machine = new(script);

Console.WriteLine((await machine.execute("onCreated"))?.Value);