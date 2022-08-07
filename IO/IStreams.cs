namespace archie.io;

public interface IStreams {
    TextReader Stdin {get;}
    TextWriter Stdout {get;}
    TextWriter Stderror {get;}
}