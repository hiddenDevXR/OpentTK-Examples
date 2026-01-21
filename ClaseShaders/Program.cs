using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;

class Game : GameWindow
{

    // ID used by OpenGL.
    private int _vao; // Vertex array object -> Cómo se leen los vértices.
    private int _vbo; // Vertex buffer object -> Datos crudos de los vértices.
    private int _ebo; // Element buffer object -> índices y el orden de cada vértice.
    private Shader _shader;
    private Texture _texture;

    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);

        // 4 flotantes por vertice (x,y) & (u,v)
        float[] vertices =
        {
            -0.5f,  0.5f,  0f, 1f,  // v0: Izq Arriba
             0.5f,  0.5f,  1f, 1f,  // v1: Der Arriba
            -0.5f, -0.5f,  0f, 0f,  // v2: Izq Abajo
             0.5f, -0.5f,  1f, 0f,  // v3: Der Abajo 
        };

        // Determinamos el order para usar los vértices.
        uint[] indices =
        {
            0, 2, 1, // T01
            2, 3, 1  // T02
        };


        // Creamos los objetos de OpenGL
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        // Activamos el VAO.
        GL.BindVertexArray(_vao);

        // Para el VBO, hacemos los "Bind" / enlaces de datos con la GOU y los shaders.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // EBO
        // Contiene los índices de cómo leer los vértices. El EBO se guarda en el VAO.
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // VAO sabe cómo leer el VBO
        // Atributo 1: Posiciones
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Atributo 2: color
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);


        _texture = new Texture("Textures/Lenna.png");
        _shader = new Shader("Shaders/textured.vert", "Shaders/textured.frag");


    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        _shader.Use();
        _shader.SetInt("uTex", 0);
        _texture.Use(TextureUnit.Texture0);

        // VAO ya trae el VBO + EBO = formato
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        GL.DeleteBuffer(_ebo);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
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
            Title = "E02 - Triangle",
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
