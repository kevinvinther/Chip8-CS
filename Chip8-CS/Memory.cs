namespace Chip8_CS;

public class Memory
{
    public byte[] MemoryArray { get; } = new byte[4096];

    private byte[] _chip8_fontset = new byte[80]
    { 
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };
    

    public Memory()
    {
        Array.Clear(MemoryArray, 0, MemoryArray.Length);
    }

    public void LoadFontset()
    {
        for (int i = 0; i < 80; ++i)
        {
            MemoryArray[i] = _chip8_fontset[i];
        }
    }

    public ushort GetOpcode(ushort pc)
    {
        return (ushort)(MemoryArray[pc] << 8 | MemoryArray[pc + 1]);
    }
    
    /// <summary>
    /// Loads the machine code into memory.
    /// </summary>
    /// <param name="path">The path to the machine code.</param>
    public void LoadGame(String path)
    {
        try
        {
            byte[] program = File.ReadAllBytes(path);

            if (program.Length > (MemoryArray.Length - 0x200)) // recall that game memory starts at 0x200
            {
                throw new ArgumentException("Error: Program too large to fit in memory.");
                return;
            }

            Array.Copy(program, 0, MemoryArray, 0x200, program.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {path}: {ex.Message}");
        }
    }
}