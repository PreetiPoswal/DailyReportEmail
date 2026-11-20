using System;
using System.Linq;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.Load("QuestPDF");
        var methods = asm.GetTypes()
            .Where(t => t.IsSealed && t.IsAbstract && t.IsPublic) // static classes
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name.Contains("Svg") || m.Name.Contains("Canvas") || m.Name.Contains("Image") || m.Name.Contains("Text") || m.Name.Contains("Row") || m.Name.Contains("Column") || m.Name.Contains("Page") || m.Name.Contains("Table") || m.Name.Contains("Pie") || m.Name.Contains("Chart") || m.Name.Contains("Path"))
                .Select(m => new { Declaring = t.FullName, m.Name, Params = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)), Return = m.ReturnType.Name }))
            .OrderBy(x => x.Declaring).ThenBy(x => x.Name);
        foreach (var method in methods) Console.WriteLine($"{method.Declaring}.{method.Name}({method.Params}) => {method.Return}");
    }
}
