using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using archie.io;
using Xunit.Abstractions;

namespace units;

public class InputOutputWrapper : IStreams
{

    public InputOutputWrapper(ITestOutputHelper helper)
    {
        VirtualStdout = new VirtualStream(helper, false, false, true);
        VirtualStderr = new VirtualStream(helper, false, false, true);
    }

    public class VirtualStream : Stream
    {
        private long _length;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly ITestOutputHelper _helper;
        private readonly bool _canSeek;

        public List<string> OutBuffer { get; private set; }
        public List<string> InBuffer { get; private set; }

        public VirtualStream(ITestOutputHelper helper, bool canRead, bool canSeek, bool canWrite)
        {
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
            _helper = helper!;
            OutBuffer = new List<string>();
            InBuffer = new List<string>();
        }
        public override bool CanRead => _canRead;

        public override bool CanSeek => _canSeek;

        public override bool CanWrite => _canWrite;

        public override long Length => _length;

        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = offset;
            return Position;
        }

        public override void SetLength(long value)
        {
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var line = Encoding.Default.GetString(buffer, offset, count);
            OutBuffer.Add(line);
            _helper.WriteLine(line);
        }
    }


    public TextReader Stdin => null;

    public TextWriter Stdout => new StreamWriter(VirtualStdout);

    public TextWriter Stderror => new StreamWriter(VirtualStderr);

    public VirtualStream VirtualStdout { get; private set; }
    public VirtualStream VirtualStderr { get; private set; }
}