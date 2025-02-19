using OpenTK.Windowing.Desktop;

namespace Chip8_CS;

public class Graphics
{
    public void SetPixels(byte[] gfx) 
    {
        if (gfx.Length != 32 * 64)
        {
            throw new ArgumentException("You must provide a 64 * 32 graphics array!");
        }     
    } 
    
    public void SetupGraphics(String title)
    {
        var windowSettings = new NativeWindowSettings()
        {
            Size = new OpenTK.Mathematics.Vector2i(64, 32),
            Title = title,
        };

        var window = new GameWindow(GameWindowSettings.Default, windowSettings);
        window.Run();
    }
}