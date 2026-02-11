using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public sealed class Shader : IDisposable
{
    public int Handle { get; private set; }
    private readonly Dictionary<string, int> _uniformLocations = new();

    public Shader(string vertexPath, string fragmentPath)
    {
        // ✅ Resuelve rutas aunque el programa corra desde bin/Debug/...
        vertexPath = ResolvePath(vertexPath);
        fragmentPath = ResolvePath(fragmentPath);

        string vertexSource = File.ReadAllText(vertexPath);
        string fragmentSource = File.ReadAllText(fragmentPath);

        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
            throw new Exception("Shader link error:\n" + GL.GetProgramInfoLog(Handle));

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // Cache de uniforms
        GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
        for (int i = 0; i < numberOfUniforms; i++)
        {
            string key = GL.GetActiveUniform(Handle, i, out _, out _);
            int location = GL.GetUniformLocation(Handle, key);
            _uniformLocations[key] = location;
        }
    }

    public void Use() => GL.UseProgram(Handle);

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        if (!_uniformLocations.TryGetValue(name, out int location) || location == -1)
            location = GL.GetUniformLocation(Handle, name);

        // ✅ transpose false (OpenTK / column-major típico)
        GL.UniformMatrix4(location, transpose: false, ref matrix);
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
            throw new Exception($"Error compiling {type}:\n{GL.GetShaderInfoLog(shader)}");

        return shader;
    }

    // ✅ Busca el archivo en varios lugares típicos
    private static string ResolvePath(string relativeOrAbsolute)
    {
        if (Path.IsPathRooted(relativeOrAbsolute) && File.Exists(relativeOrAbsolute))
            return relativeOrAbsolute;

        // 1) CurrentDirectory
        string p1 = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativeOrAbsolute));
        if (File.Exists(p1)) return p1;

        // 2) BaseDirectory (bin/Debug/netX.Y/)
        string baseDir = AppContext.BaseDirectory;
        string p2 = Path.GetFullPath(Path.Combine(baseDir, relativeOrAbsolute));
        if (File.Exists(p2)) return p2;

        // 3) Sube 3 niveles desde bin/Debug/netX.Y/ hacia el root del proyecto
        string projRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        string p3 = Path.GetFullPath(Path.Combine(projRoot, relativeOrAbsolute));
        if (File.Exists(p3)) return p3;

        throw new FileNotFoundException($"No se encontró el shader: {relativeOrAbsolute}\nProbé:\n{p1}\n{p2}\n{p3}");
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
        Handle = 0;
    }
}
