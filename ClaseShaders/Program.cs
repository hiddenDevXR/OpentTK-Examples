using System;                           // Tipos base de C# (Exception, etc.)
using OpenTK.Mathematics;               // Matrices, vectores, MathHelper
using OpenTK.Graphics.OpenGL4;          // Llamadas OpenGL (GL.*)
using OpenTK.Windowing.Common;          // Tipos de eventos (FrameEventArgs, ResizeEventArgs)
using OpenTK.Windowing.Desktop;         // GameWindow y configuración de ventana

// Clase estática con el punto de entrada del programa (Main)
internal static class Program
{
    private static void Main()
    {
        // Configuración del "game loop" (frecuencias por defecto, etc.)
        var gws = GameWindowSettings.Default;

        // Configuración de la ventana y del contexto OpenGL
        var nws = new NativeWindowSettings
        {
            Size = new Vector2i(900, 650),         // Tamaño de ventana en píxeles
            Title = "Cubo 3D con Perspectiva",     // Título de la ventana

            // Pide un contexto OpenGL 3.3 "Core Profile" (pipeline moderno)
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,

            // ForwardCompatible: deshabilita funciones antiguas (fixed pipeline)
            Flags = ContextFlags.ForwardCompatible
        };

        // Crea la ventana (Game) y arranca el loop
        using var game = new Game(gws, nws);
        game.Run(); // Llama internamente a: OnLoad -> Update/Render en bucle -> OnUnload
    }
}

// Nuestra ventana/juego hereda de GameWindow (OpenTK maneja el loop por nosotros)
public sealed class Game : GameWindow
{
    // =========================
    // GEOMETRÍA: VÉRTICES
    // =========================
    // Cada vértice tiene 6 floats:
    //  - Posición: X, Y, Z  (3 floats)
    //  - Color:    R, G, B  (3 floats)
    private readonly float[] _vertices =
    {
        // 8 vértices del cubo:
        // X,  Y,  Z,    R,  G,  B
        -1f, -1f,  1f,   1f, 0f, 0f,  // 0: frente-abajo-izquierda (rojo)
         1f, -1f,  1f,   0f, 1f, 0f,  // 1: frente-abajo-derecha   (verde)
         1f,  1f,  1f,   0f, 0f, 1f,  // 2: frente-arriba-derecha (azul)
        -1f,  1f,  1f,   1f, 1f, 0f,  // 3: frente-arriba-izq     (amarillo)

        -1f, -1f, -1f,   1f, 0f, 1f,  // 4: atrás-abajo-izq       (magenta)
         1f, -1f, -1f,   0f, 1f, 1f,  // 5: atrás-abajo-der       (cian)
         1f,  1f, -1f,   1f, 1f, 1f,  // 6: atrás-arriba-der      (blanco)
        -1f,  1f, -1f,   0f, 0f, 0f,  // 7: atrás-arriba-izq      (negro)
    };

    // =========================
    // GEOMETRÍA: ÍNDICES
    // =========================
    // En vez de repetir vértices, usamos un arreglo de índices.
    // Cada grupo de 3 índices = 1 triángulo.
    // Un cubo: 6 caras * 2 triángulos/cara = 12 triángulos = 36 índices
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

    // IDs de objetos OpenGL
    private int _vao; // Vertex Array Object: guarda el "layout" de atributos (cómo leer el VBO)
    private int _vbo; // Vertex Buffer Object: guarda los vértices en GPU
    private int _ebo; // Element Buffer Object: guarda los índices en GPU

    // Shader program y ubicación del uniform
    private int _shaderProgram; // Programa linkeado (vertex + fragment)
    private int _uMvpLocation;  // Ubicación en GPU del uniform "uMVP"

    // Animación / Cámara
    private float _angleDeg;      // Ángulo acumulado de rotación (en grados)
    private Matrix4 _projection;  // Matriz de proyección (perspectiva)

    // Constructor: llama al constructor base (GameWindow) con settings
    public Game(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

    // =========================
    // ONLOAD: corre UNA VEZ
    // =========================
    protected override void OnLoad()
    {
        base.OnLoad();

        // Color con el que se limpia el buffer de color (fondo)
        GL.ClearColor(0.08f, 0.09f, 0.12f, 1f);

        // Habilita prueba de profundidad (Z-buffer)
        // Sin esto, los triángulos "del fondo" se pueden dibujar encima.
        GL.Enable(EnableCap.DepthTest);

        // Genera (crea) los objetos de OpenGL y devuelve sus IDs
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        // "Bind" del VAO: a partir de aquí, la configuración de atributos queda guardada en este VAO
        GL.BindVertexArray(_vao);

        // -------------------------
        // VBO: subir vértices a GPU
        // -------------------------
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        // Copia _vertices (CPU) -> memoria en GPU (VBO)
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            _vertices.Length * sizeof(float),
            _vertices,
            BufferUsageHint.StaticDraw
        );

        // -------------------------
        // EBO: subir índices a GPU
        // -------------------------
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

        // Copia _indices (CPU) -> memoria en GPU (EBO)
        GL.BufferData(
            BufferTarget.ElementArrayBuffer,
            _indices.Length * sizeof(uint),
            _indices,
            BufferUsageHint.StaticDraw
        );

        // -------------------------
        // Layout de atributos
        // -------------------------
        // Cada vértice tiene 6 floats => 6 * 4 bytes = 24 bytes por vértice
        int stride = 6 * sizeof(float);

        // Atributo 0 (location = 0 en el shader): vec3 aPos
        // Lee 3 floats desde offset 0
        GL.VertexAttribPointer(
            index: 0,
            size: 3,
            type: VertexAttribPointerType.Float,
            normalized: false,
            stride: stride,
            offset: 0
        );
        GL.EnableVertexAttribArray(0); // Habilita el atributo 0

        // Atributo 1 (location = 1 en el shader): vec3 aColor
        // Lee 3 floats desde offset = 3 floats (después de la posición)
        GL.VertexAttribPointer(
            index: 1,
            size: 3,
            type: VertexAttribPointerType.Float,
            normalized: false,
            stride: stride,
            offset: 3 * sizeof(float)
        );
        GL.EnableVertexAttribArray(1); // Habilita el atributo 1

        // Des-bindeamos el VAO para no modificarlo accidentalmente después
        GL.BindVertexArray(0);

        // =========================
        // SHADERS: compilar + linkear
        // =========================
        _shaderProgram = CreateShaderProgram(VertexShaderSource, FragmentShaderSource);

        // Obtiene la ubicación del uniform "uMVP" dentro del shader program
        _uMvpLocation = GL.GetUniformLocation(_shaderProgram, "uMVP");

        // =========================
        // PROYECCIÓN: perspectiva
        // =========================
        _projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),   // Campo de visión vertical (FOV)
            Size.X / (float)Size.Y,             // Aspect ratio (ancho/alto)
            0.1f,                                // Plano cercano (near)
            100f                                 // Plano lejano (far)
        );
    }

    // =========================
    // ONRESIZE: cuando cambia tamaño
    // =========================
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // Ajusta el viewport para que OpenGL dibuje en toda la ventana
        GL.Viewport(0, 0, Size.X, Size.Y);

        // Recalcula la proyección con el nuevo aspect ratio
        _projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            Size.X / (float)Size.Y,
            0.1f,
            100f
        );
    }

    // =========================
    // ONUPDATEFRAME: lógica (cada frame)
    // =========================
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        // Aumenta el ángulo según el tiempo real transcurrido entre frames
        // Esto hace la rotación independiente del FPS.
        _angleDeg += 60f * (float)e.Time;

        // Cierra con ESC
        if (IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
            Close();
    }

    // =========================
    // ONRENDERFRAME: dibujado (cada frame)
    // =========================
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        // Limpia el color buffer (pantalla) y depth buffer (Z-buffer)
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // =========================
        // MATRICES
        // =========================

        // MODEL: transforma el objeto (rotaciones en X e Y)
        var model =
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_angleDeg)) *
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_angleDeg * 0.6f));

        // VIEW: simula cámara alejándose 5 unidades en Z
        // (Mover la cámara atrás equivale a mover la escena hacia -Z)
        var view = Matrix4.CreateTranslation(0f, 0f, -5f);

        // MVP: combina Model, View, Projection en una sola matriz
        // IMPORTANTE: El orden correcto típico con gl_Position = uMVP * vec4(pos,1)
        // suele ser: projection * view * model
        // Tú aquí tienes: model * view * projection (en algunos setups sale raro).
        var mvp = model * view * _projection;

        // Activa el programa de shaders para dibujar
        GL.UseProgram(_shaderProgram);

        // Sube la matriz MVP al uniform uMVP del shader
        // transpose:false => OpenTK/GLSL usual (column-major)
        GL.UniformMatrix4(_uMvpLocation, transpose: false, ref mvp);

        // Bind del VAO (recuerda: el VAO sabe cómo leer el VBO y qué EBO usar)
        GL.BindVertexArray(_vao);

        // Dibuja usando índices (EBO) en modo triángulos
        GL.DrawElements(
            PrimitiveType.Triangles,
            _indices.Length,
            DrawElementsType.UnsignedInt,
            0
        );

        // Des-bindea por limpieza
        GL.BindVertexArray(0);

        // Intercambia buffers: lo dibujado pasa a pantalla
        SwapBuffers();
    }

    // =========================
    // ONUNLOAD: limpieza al cerrar
    // =========================
    protected override void OnUnload()
    {
        base.OnUnload();

        // Libera recursos en GPU
        GL.DeleteBuffer(_ebo);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);

        GL.DeleteProgram(_shaderProgram);
    }

    // =========================
    // CREA PROGRAMA DE SHADERS (compile + link)
    // =========================
    private static int CreateShaderProgram(string vertexSrc, string fragmentSrc)
    {
        // Compila cada shader (vertex y fragment)
        int vs = CompileShader(ShaderType.VertexShader, vertexSrc);
        int fs = CompileShader(ShaderType.FragmentShader, fragmentSrc);

        // Crea un programa y adjunta shaders
        int program = GL.CreateProgram();
        GL.AttachShader(program, vs);
        GL.AttachShader(program, fs);

        // Link: combina ambos shaders en un único programa ejecutable por GPU
        GL.LinkProgram(program);

        // Verifica si link fue exitoso
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
        {
            // Si falla, imprime el log (muy útil para errores de GLSL)
            string log = GL.GetProgramInfoLog(program);
            throw new Exception($"Error linkeando shader program:\n{log}");
        }

        // Ya linkeado: podemos separar y borrar los shaders individuales
        GL.DetachShader(program, vs);
        GL.DetachShader(program, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        return program;
    }

    // =========================
    // COMPILA UN SHADER (vertex o fragment)
    // =========================
    private static int CompileShader(ShaderType type, string src)
    {
        // Crea objeto shader (aún vacío)
        int shader = GL.CreateShader(type);

        // Pasa el código GLSL al shader
        GL.ShaderSource(shader, src);

        // Compila el GLSL
        GL.CompileShader(shader);

        // Revisa si compiló bien
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            // Si falla, muestra el error de compilación de GLSL
            string log = GL.GetShaderInfoLog(shader);
            throw new Exception($"Error compilando {type}:\n{log}");
        }

        return shader;
    }

    // =========================
    // VERTEX SHADER (GLSL)
    // =========================
    private const string VertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPos;     
layout(location = 1) in vec3 aColor;   

uniform mat4 uMVP;                     

out vec3 vColor;                       

void main()
{
    vColor = aColor;                   
    gl_Position = uMVP * vec4(aPos, 1.0);
}
";

    // =========================
    // FRAGMENT SHADER (GLSL)
    // =========================
    private const string FragmentShaderSource = @"
#version 330 core
in vec3 vColor;                        
out vec4 FragColor;                    

void main()
{
    FragColor = vec4(vColor, 1.0);     
}
";
}
