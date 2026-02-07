using OpenTK.Mathematics;

public sealed class Renderer
{
    public Matrix4 View = Matrix4.Identity;
    public Matrix4 Projection = Matrix4.Identity;

    public void Draw(Mesh mesh, Shader shader)
    {
        shader.Use();

        Matrix4 mvp = Projection * View * Matrix4.Identity;
        shader.SetMatrix4("uMVP", mvp);

        mesh.Draw();
    }
}
