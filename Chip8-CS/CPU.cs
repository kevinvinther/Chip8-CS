namespace Chip8_CS;

public class Cpu
{
    private ushort _opcode;
    // _v defines the general purpose registers named V0, V1, ..., VE.
    private byte[] _v = new byte[16];
    // index register
    private ushort _i;
    // program counter
    private ushort _pc;
    // graphics, if 0 the pixel is off, if 1 the pixel is on.
    private byte[] _gfx = new byte[64 * 32];

    // interrupts and hardware registers. counts at 60 hz. when above 0, counts down to zero.
    private byte _delay_timer;
    private byte _sound_timer;

    // the stack is used to remember the current location before a jump is performed.
    // thus you store the program counter before jumping. 
    private ushort[] _stack = new ushort[16];
    // stack pointer, holds the current level of stack (out of 16)
    private ushort _sp;

    // hex-based keypad (0x0-0xF). stores the current key state.
    private byte[] _key = new byte[16];

    private Memory _memory;

    /// <summary>
    /// Initializes the registers and memory.
    /// </summary>
    public void Initialize()
    {
        _pc = 0x200; // Program counter starts at 0x200
        _opcode = 0; // Reset the opcode
        _i = 0; // Reset index
        _sp = 0; // Reset stack pointer
        
        _memory.InitializeMemory();
        _memory.LoadFontset();
        // Clear stack
        Array.Clear(_stack, 0, _stack.Length);
        // Clear registers
        Array.Clear(_v, 0, _v.Length);
        
        // Reset Timers
        _delay_timer = 0;
        _sound_timer = 0;
    }
}