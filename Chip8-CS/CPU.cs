using System.Data;

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


    /// <summary>
    /// Emulates one cycle based on memory. Uses the fetch-decode-execute cycle,
    /// as well as updating the timers.
    /// </summary>
    public void EmulateCycle()
    {
        var opcode = _memory.GetOpcode(_pc);

        switch (opcode & 0xF000) // Get the first symbol of the code
        {
            case 0x0000:
                switch (opcode & 0x000F)
                {
                    case 0x00E0:
                        Array.Clear(_gfx, 0, _gfx.Length);
                        break;
                    case 0x00EE:
                        if (_sp > 0)
                        {
                            _sp -= 1;
                            _pc = _stack[_sp];
                        }
                        else
                        {
                            throw new StackOverflowException("Stack underflow error: No return address.");
                        }

                        break; 
                    default:
                        throw new ArgumentException($"Unknown opcode {opcode}");
                }

                break;
            case 0xA000:
                _i = (ushort)(opcode & 0x0FFF);
                _pc += 2;
                break;
            case 0x2000:
                // We need to do a temporary jump, thus we store the current 
                // address of the pc.
                _stack[_sp] = _pc; 
                _sp += 1;
                _pc = (ushort)(opcode & 0xFFF);
                break;

            case 0x8000:
                var (x, y) = GetXyFromOpcode(opcode);
                switch (opcode & 0x8000)
                {   
                    case 0x0000:
                        _v[x] = _v[y];
                        IncrementPC();
                        break;
                    case 0x0001:
                        _v[x] = (byte)(_v[x] | _v[y]);
                        IncrementPC();
                        break;
                    case 0x0002:
                        _v[x] = (byte)(_v[x] & _v[y]);
                        IncrementPC();
                        break;
                    case 0x0003:
                        _v[x] = (byte)(_v[x] ^ _v[y]);
                        IncrementPC();
                        break;
                    case 0x0004:
                        int sum = _v[x] + _v[y];
                        unchecked
                        {
                            _v[x] = (byte)sum;
                        }
                        _v[0xF] = (sum > 255) ? (byte)1 : (byte)0;
                        IncrementPC();
                        break;
                }

                break;
            default:
                throw new NotImplementedException($"The opcode {opcode} has not yet been implemented!");
        }
        
        if (_delay_timer > 0)
            _delay_timer -= 1;

        if (_sound_timer > 0)
        {
            if (_sound_timer == 1)
                Console.WriteLine("BEEP!");
            _sound_timer -= 1;
        }
    }

    private (int, int) GetXyFromOpcode(ushort opcode)
    {
        var x = (opcode & 0x0F00) >> 8;
        var y = (opcode & 0x00F0) >> 4;

        return (x, y);
    }

    private void IncrementPC()
    {
        _pc += 2;
    }
}