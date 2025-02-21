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

    private bool _drawFlag;

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
                        IncrementPC();
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
            case 0x1000:
                _pc = (ushort)(opcode & 0x0FFF);
                break;
            case 0x2000:
                // We need to do a temporary jump, thus we store the current 
                // address of the pc.
                _stack[_sp] = _pc; 
                _sp += 1;
                _pc = (ushort)(opcode & 0xFFF);
                break;
            case 0x3000:
                if (_v[opcode & 0x0F00] == (opcode & 0x00FF))
                    IncrementPC();
                break;
            case 0x4000:
                if (_v[opcode & 0x0F00] != (opcode & 0x00FF))
                    IncrementPC();
                break;
            case 0x5000:
                if (_v[opcode & 0x0F00] == _v[opcode & 0x00F0])
                    IncrementPC();
                break;
            case 0x6000:
                _v[opcode & 0x0F00] = (byte)(opcode & 0x00FF);
                IncrementPC();
                break;
            case 0x7000:
                _v[opcode & 0x0F00] += (byte)(opcode & 0x00FF);
                IncrementPC();
                break;
            case 0x8000:
            {
                var (x, y, _) = GetXyzFromOpcode(opcode);
                switch (opcode & 0x0FFF)
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
                    case 0x0005:
                        _v[0xF] = (_v[x] >= _v[y]) ? (byte)1 : (byte)0;
                        _v[x] = (byte)(_v[x] - _v[y]);
                        IncrementPC();
                        break;
                    case 0x0006:
                        _v[0xF] = (byte)(_v[x] & 0x1);
                        _v[x] >>= 1;
                        IncrementPC();
                        break;
                    case 0x0007:
                        int subSum = _v[x] - _v[y];
                        unchecked
                        {
                            _v[x] = (byte)subSum;
                        }

                        _v[0xF] = (subSum < 0) ? (byte)0 : (byte)1;
                        IncrementPC();
                        break;
                    case 0x000E:
                        // 0x80 is the most significant bit
                        // we bitshift it by 7 to make it the least significant, and therefore store just the bit in v[F]
                        _v[0xF] = (byte)((_v[x] & 0x80) >> 7);
                        _v[x] <<= 1;
                        IncrementPC();
                        break;
                    default:
                        throw new ArgumentException($"Opcode doesn't exist. Opcode: {opcode:X}");
                }

                break;
            }
            case 0xA000:
                _i = (ushort)(opcode & 0x0FFF);
                _pc += 2;
                break;
            case 0xB000:
                _pc = (ushort)(_v[0x0] + (opcode & 0x0FFF));
                break;
            case 0xC000:
                Random rng = new Random();
                _v[opcode & 0x0F00] = (byte)(rng.Next(0, 256) & (opcode & 0x00FF));
                IncrementPC();
                break;
            case 0xD000:
            {
                var (x, y, height) = GetXyzFromOpcode(opcode);

                _v[0xF] = 0;
                for (var yline = 0; yline < height; yline++)
                {
                    ushort pixel = _memory.MemoryArray[_i + yline];
                    // the width is hardcoded to be 8.
                    for (var xline = 0; xline < 8; xline++)
                    {
                        if ((pixel & (0x80 >> xline)) != 0)
                        {
                            if (_gfx[(x + xline + ((y + yline) * 64))] == 1)
                                _v[0xF] = 1;
                            _gfx[x + xline + ((y + yline) * 64)] ^= 1;
                        }
                    }
                }
                _drawFlag = true;
                IncrementPC();
                break;

            }
            case 0xE000:
            {
                var (x, _, _) = GetXyzFromOpcode(opcode);
                switch (opcode & 0x0FFF)
                {
                    case 0x009E:
                        throw new NotImplementedException();
                    case 0x00A1:
                        throw new NotImplementedException();
                    case 0x0007:
                        _v[x] = _delay_timer;
                        IncrementPC();
                        break;
                    case 0x000A:
                        throw new NotImplementedException();
                    case 0x0015:
                        _delay_timer = _v[x];
                        IncrementPC();
                        break;
                    case 0x0018:
                        _sound_timer = _v[x];
                        IncrementPC();
                        break;
                    case 0x001E:
                        _i += _v[x];
                        IncrementPC();
                        break;
                    case 0x0029:
                        throw new NotImplementedException();
                    case 0x0033:
                        _memory.MemoryArray[_i] = (byte)(_v[x] / 100);
                        _memory.MemoryArray[_i+1] = (byte)((_v[x] / 10) % 10);
                        _memory.MemoryArray[_i+2] = (byte)((_v[x] % 100) % 10);
                        IncrementPC();
                        break;
                    case 0x0055:
                        for (int y = 0; y <= x; y++)
                        {
                            _memory.MemoryArray[_i+y] = _v[y];
                        }
                        IncrementPC();
                        break;
                    case 0x0065:
                        for (int y = 0; y <= x; y++)
                        {
                            _v[y] = _memory.MemoryArray[_i+y];
                        }
                        IncrementPC();
                        break;
                }

                break;
            }
            default:
                throw new NotImplementedException($"The opcode {opcode:X} has not yet been implemented!");
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

    private (int, int, int) GetXyzFromOpcode(ushort opcode)
    {
        var x = (opcode & 0x0F00) >> 8;
        var y = (opcode & 0x00F0) >> 4;
        var z = (opcode & 0x000F);

        return (x, y, z);
    }

    private void IncrementPC(ushort amount = 2)
    {
        _pc += amount;
    }
}