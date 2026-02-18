using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

// =========================
// ENTRY POINT
// =========================
internal static class Program
{
    private static void Main()
    {
        var gws = GameWindowSettings.Default;

        var nws = new NativeWindowSettings
        {
            Size = new Vector2i(900, 650),
            Title = "Cubo 3D (Shader + Mesh)",
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible
        };

        using var game = new Game(gws, nws);
        game.Run();
    }
}

// =========================
// GAME WINDOW
// =========================
public sealed class Game : GameWindow
{
    // =========================
    // GEOMETRÍA
    // =========================
    private readonly float[] _vertices =
    {
        // 8 vértices del cubo:
        // X,  Y,  Z,    R,  G,  B
        -1f, -1f,  1f,   1f, 0f, 0f,  // 0
         1f, -1f,  1f,   0f, 1f, 0f,  // 1
         1f,  1f,  1f,   0f, 0f, 1f,  // 2
        -1f,  1f,  1f,   1f, 1f, 0f,  // 3

        -1f, -1f, -1f,   1f, 0f, 1f,  // 4
         1f, -1f, -1f,   0f, 1f, 1f,  // 5
         1f,  1f, -1f,   1f, 1f, 1f,  // 6
        -1f,  1f, -1f,   0f, 0f, 0f,  // 7
    };

    private readonly uint[] _indices =
    {
        // Frente
        0, 1, 2,  2, 3, 0,
        // Derecha
        1, 5, 6,  6, 2, 1,
        // Atrás
        5, 4, 7,  7, 6, 5,
        // Izquierda
        4, 0, 3,  3, 7, 4,
        // Arriba
        3, 2, 6,  6, 7, 3,
        // Abajo
        4, 5, 1,  1, 0, 4
    };

    // =========================
    // OBJETOS "ALTOS"
    // =========================
    private Mesh _cube;
    private Shader _shader;

    // =========================
    // CÁMARA / ANIM
    // =========================
    private float _angleDeg;
    private Matrix4 _projection;

    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    // =========================
    // ONLOAD
    // =========================
    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.08f, 0.09f, 0.12f, 1f);
        GL.Enable(EnableCap.DepthTest);

        // Por si acaso (además OnResize)
        GL.Viewport(0, 0, Size.X, Size.Y);

        // Crea Mesh (layout: pos+color => 6 floats)
        _cube = new Mesh(_vertices, _indices, strideBytes: 6 * sizeof(float));

        // Crea Shader
        _shader = new Shader(
    "Shaders/basic.vert",
    "Shaders/basic.frag"
);

        // Proyección inicial
        _projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            Size.X / (float)Size.Y,
            0.1f,
            100f
        );
    }

    // =========================
    // ONRESIZE
    // =========================
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);

        _projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            Size.X / (float)Size.Y,
            0.1f,
            100f
        );
    }

    // =========================
    // UPDATE
    // =========================
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        _angleDeg += 60f * (float)e.Time;

        if (IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
            Close();
    }

    // =========================
    // RENDER
    // =========================
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Model
        var model =
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_angleDeg)) *
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_angleDeg * 0.6f));

        // View
        var view = Matrix4.CreateTranslation(0f, 0f, -5f);

        // Mantén tu convención actual (porque dices que así te funciona)
        var mvp = model * view * _projection;

        _shader.Use();
        _shader.SetMatrix4("uMVP", mvp, transpose: false);

        _cube.Draw();

        SwapBuffers();
    }

    // =========================
    // UNLOAD
    // =========================
    protected override void OnUnload()
    {
        base.OnUnload();

        _cube?.Dispose();
        _shader?.Dispose();
    }
}
