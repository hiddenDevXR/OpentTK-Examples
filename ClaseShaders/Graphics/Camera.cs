using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Camera
{
    private float _yaw = -90f;
    private float _pitch = 15f;
    private float _distance = 5f;

    private Vector2 _lastMouse;
    private bool _dragging;

    public Vector3 Position { get; private set; }
    public Matrix4 View { get; private set; }

    public void Update(MouseState mouse)
    {
        // Zoom
        _distance -= mouse.ScrollDelta.Y * 0.6f;
        _distance = MathHelper.Clamp(_distance, 2f, 25f);

        // Rotación con click izquierdo
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            if (!_dragging)
            {
                _dragging = true;
                _lastMouse = mouse.Position;
            }
            else
            {
                var delta = mouse.Position - _lastMouse;
                _lastMouse = mouse.Position;

                float sens = 0.25f;

                _yaw += delta.X * sens;
                _pitch += delta.Y * sens;

                _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
            }
        }
        else
        {
            _dragging = false;
        }

        UpdateVectors();
    }

    private void UpdateVectors()
    {
        float yawR = MathHelper.DegreesToRadians(_yaw);
        float pitchR = MathHelper.DegreesToRadians(_pitch);

        Position = new Vector3(
            _distance * MathF.Cos(pitchR) * MathF.Cos(yawR),
            _distance * MathF.Sin(pitchR),
            _distance * MathF.Cos(pitchR) * MathF.Sin(yawR)
        );

        View = Matrix4.LookAt(Position, Vector3.Zero, Vector3.UnitY);
    }
}