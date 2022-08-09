namespace archie.io;

public class Streams : IStreams
{
    public Streams(TextReader stdin, TextWriter stdout, TextWriter stderror)
    {   
        Stdin = stdin;
        Stdout = stdout;
        Stderror = stderror;
    }

    public TextReader Stdin {get;private set;}

    public TextWriter Stdout {get;private set;}

    public TextWriter Stderror {get;private set;}
}