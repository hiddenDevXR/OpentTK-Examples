using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;

class Game : GameWindow
{
    // =========================
    // 1) Recursos compartidos
    // =========================
    // La idea “motorcito” es:
    // - Mesh: geometría (quad) que se reutiliza
    // - Shader: programa GPU que se reutiliza
    // - Renderer: contiene View/Projection y sabe dibujar Renderables
    private Mesh _quadMesh;
    private Shader _shader;
    private Renderer _renderer;

    // =========================
    // 2) Recursos por material
    // =========================
    // Cada material = (shader + textura) en el caso simple
    private Texture _texA, _texB, _texC;
    private Material _matA, _matB, _matC;

    // =========================
    // 3) Lista de instancias (objetos en escena)
    // =========================
    // Cada Renderable tiene:
    // - Mesh (compartido)
    // - Material (puede ser distinto)
    // - Transform (Position/Rotation/Scale)
    private readonly List<Renderable> _objects = new();

    private float _time;

    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.08f, 0.08f, 0.10f, 1f);

        // ============================================================
        // A) Crear la geometría UNA SOLA VEZ (Mesh compartido)
        // ============================================================
        // Quad con layout: (x, y, u, v) => 4 floats por vértice
        float[] vertices =
        {
            // x,     y,     u,   v
            -0.5f,  0.5f,  0f,  1f, // v0 (top-left)
             0.5f,  0.5f,  1f,  1f, // v1 (top-right)
            -0.5f, -0.5f,  0f,  0f, // v2 (bottom-left)
             0.5f, -0.5f,  1f,  0f  // v3 (bottom-right)
        };

        // 2 triángulos -> 6 índices
        uint[] indices =
        {
            0, 2, 1,
            2, 3, 1
        };

        int stride = 4 * sizeof(float);

        // Creamos el Mesh, y le decimos cómo configurar el VAO (atributos)
        // OJO: aquí es donde conectas el VBO con el shader:
        // location 0 -> aPos (vec2)
        // location 1 -> aUV  (vec2)
        _quadMesh = new Mesh(vertices, indices, stride, setupAttribs: () =>
        {
            // location 0 -> aPos (vec2)
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            // location 1 -> aUV (vec2)
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        });

        // ============================================================
        // B) Shader compartido (uno para todos los objetos)
        // ============================================================
        _shader = new Shader("Shaders/textured_mvp.vert", "Shaders/textured.frag");

        // ============================================================
        // C) Texturas (una por “material”)
        // ============================================================
        // Cambia los nombres según tus archivos reales en Textures/
        _texA = new Texture("Textures/Lenna.png");
        _texB = new Texture("Textures/Lenna.png");
        _texC = new Texture("Textures/Lenna.png");

        // ============================================================
        // D) Material = Shader + Texture
        // ============================================================
        // Importante: el shader se comparte, lo que cambia es la textura
        _matA = new Material(_shader, _texA);
        _matB = new Material(_shader, _texB);
        _matC = new Material(_shader, _texC);

        // ============================================================
        // E) Renderer: define “cámara” (View) y Projection
        // ============================================================
        _renderer = new Renderer
        {
            View = Matrix4.Identity,
            Projection = Matrix4.CreateOrthographicOffCenter(-1f, 1f, -1f, 1f, -1f, 1f)
        };

        // ============================================================
        // F) Crear instancias (Renderables)
        // ============================================================
        // MISMO mesh, diferente material (textura) y diferente transform
        _objects.Add(new Renderable(_quadMesh, _matA)
        {
            Position = new Vector3(-0.7f, 0.0f, 0f),
            Scale = new Vector3(0.5f, 0.5f, 1f)
        });

        _objects.Add(new Renderable(_quadMesh, _matB)
        {
            Position = new Vector3(0.0f, 0.0f, 0f),
            Scale = new Vector3(0.5f, 0.5f, 1f)
        });

        _objects.Add(new Renderable(_quadMesh, _matC)
        {
            Position = new Vector3(0.7f, 0.0f, 0f),
            Scale = new Vector3(0.5f, 0.5f, 1f)
        });

        // Nota didáctica:
        // - Mesh = geometría reutilizable
        // - Material = apariencia (textura)
        // - Renderable = “objeto en escena” (transform + mesh + material)
        // - Renderer = “dibujador” que aplica MVP + draw
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        _time += (float)e.Time;

        // Animación simple: cada objeto rota distinto
        if (_objects.Count >= 3)
        {
            _objects[0].RotationZ = _time;
            _objects[1].RotationZ = -_time * 0.7f;
            _objects[2].RotationZ = _time * 1.3f;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // Dibujar todos los objetos:
        // Cada llamada hace:
        // - Bind material (shader + textura)
        // - Set uMVP (por objeto)
        // - Mesh.Draw()
        foreach (var obj in _objects)
            _renderer.Draw(obj);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        // Limpieza:
        // - Mesh (VAO/VBO/EBO)
        // - Texturas
        // - Shader program
        _quadMesh.Dispose();

        _texA.Dispose();
        _texB.Dispose();
        _texC.Dispose();

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
            Title = "Instancias: mismo mesh, diferente textura",
            Size = new Vector2i(900, 600),
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible
        };

        using var game = new Game(gws, nws);
        game.Run();
    }
}
