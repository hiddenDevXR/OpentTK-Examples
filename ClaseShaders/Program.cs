using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;

class Game : GameWindow
{
    private int _vao, _vbo, _ebo;

    private Shader _shader;
    private Texture _tex;

    private float _time; // acumulador para animación

    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.08f, 0.08f, 0.10f, 1f);

        // Quad: Pos(x,y) + UV(u,v)
        float[] vertices =
        {
            // x,     y,     u,    v
            -0.5f,  0.5f,  0f,  1f, // v0 top-left
             0.5f,  0.5f,  1f,  1f, // v1 top-right
            -0.5f, -0.5f,  0f,  0f, // v2 bottom-left
             0.5f, -0.5f,  1f,  0f  // v3 bottom-right
        };

        // 2 triángulos
        uint[] indices =
        {
            0, 2, 1,
            2, 3, 1
        };

        // Crear buffers
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        // VBO: datos
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // EBO: índices (queda asociado al VAO)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // Atributos:
        // layout(0) -> aPos (vec2)
        // layout(1) -> aUV  (vec2)
        int stride = 4 * sizeof(float);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Shaders (vertex con MVP)
        _shader = new Shader("Shaders/textured_mvp.vert", "Shaders/textured.frag");

        // Textura
        _tex = new Texture("Textures/Lenna.png");

        // Conectar sampler con Texture Unit 0
        _shader.Use();
        _shader.SetInt("uTex", 0);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        // Tiempo acumulado (segundos)
        _time += (float)e.Time;
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 1) Construir matrices

        // Model: rotación (2D) + un poquito de escala opcional
        var model =
            Matrix4.CreateRotationZ(_time) *
            Matrix4.CreateScale(0.9f);

        // View: identidad (no cámara todavía)
        var view = Matrix4.Identity;

        // Projection: Ortho para 2D (encaja bien con quad en [-1,1])
        // left, right, bottom, top, zNear, zFar
        var proj = Matrix4.CreateOrthographicOffCenter(-1f, 1f, -1f, 1f, -1f, 1f);

        // Orden típico para OpenGL: MVP = model * view * proj o proj * view * model según convención.
        // Con esta configuración (y el shader uMVP * vec4), suele ir bien con:
        var mvp = model * view * proj;

        // 2) Enviar uniform al shader
        _shader.Use();
        _shader.SetMatrix4("uMVP", mvp);

        // 3) Bind textura en Texture0 y dibujar
        _tex.Use(TextureUnit.Texture0);

        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        // GPU cleanup
        GL.DeleteBuffer(_ebo);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);

        _tex.Dispose();
        _shader.Dispose();
    }
}

class Program
{
    static void Main()
    {
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings
        {
            Title = "E04 - Textured Quad + MVP",
            Size = new Vector2i(800, 600),
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible
        };

        using var game = new Game(gws, nws);
        game.Run();
    }
}
