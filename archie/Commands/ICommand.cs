using System.CommandLine;

namespace archie.Commands;

public interface ICommand {
    void Register(RootCommand command);
}