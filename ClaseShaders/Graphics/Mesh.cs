using System;
using OpenTK.Graphics.OpenGL4;

public sealed class Mesh : IDisposable
{
    private readonly int _vao;
    private readonly int _vbo;
    private readonly int _ebo;
    private readonly int _indexCount;

    public Mesh(float[] vertices, uint[] indices, int strideBytes)
    {
        _indexCount = indices.Length;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        // Layout fijo para tu caso actual:
        // location 0: vec3 pos (offset 0)
        // location 1: vec3 color (offset 12 bytes)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, strideBytes, 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, strideBytes, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_ebo);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GC.SuppressFinalize(this);
    }
}
