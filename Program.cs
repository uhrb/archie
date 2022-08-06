using System.CommandLine;

namespace archie;

public static class Programm {
    public static async Task<int> MainAsync(string[] args) {
        var rootCommand = new RootCommand();
        return await rootCommand.InvokeAsync(args);
    }
}