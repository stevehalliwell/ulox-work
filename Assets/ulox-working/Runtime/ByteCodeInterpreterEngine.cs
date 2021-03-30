//todo class vars
//todo system based bouncing ball scriptworking under bytecode interp
//todo native interop
//todo collections
//todo separate the treewalk interpreter innards from the bytecode innards

namespace ULox
{
    public class ByteCodeInterpreterEngine
    {
        private Scanner _scanner;
        private Compiler _compiler;
        private VM _vm;
        private Disasembler _disasembler;

        public ByteCodeInterpreterEngine()
        {
            _scanner = new Scanner();
            _compiler = new Compiler();
            _disasembler = new Disasembler();
            _vm = new VM();
        }

        public string StackDump => _vm.GenerateStackDump();
        public string Disassembly => _disasembler.GetString();
        public VM VM => _vm;

        public virtual void Run(string testString)
        {
            _scanner.Reset();
            _compiler.Reset();

            var tokens = _scanner.Scan(testString);
            var chunk = _compiler.Compile(tokens);
            _disasembler.DoChunk(chunk);
            _vm.Interpret(chunk);
        }
    }
}
