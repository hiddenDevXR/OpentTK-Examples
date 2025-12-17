using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;

class Game : GameWindow
{
    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine("OnLoad OK. OpenGL: " + GL.GetString(StringName.Version));

        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        SwapBuffers();
    }
}

class Program
{
    static void Main()
    {
        try
        {
            var gws = GameWindowSettings.Default;

            var nws = new NativeWindowSettings
            {
                Title = "Clase Shaders - OpenTK",
                Size = new Vector2i(800, 600),

                // Esto evita muchos cierres instantáneos por contexto incompatible:
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible
            };

            using var game = new Game(gws, nws);
            game.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH:");
            Console.WriteLine(ex);
            Console.WriteLine("\nPresiona ENTER para cerrar...");
            Console.ReadLine();
        }
    }
}
