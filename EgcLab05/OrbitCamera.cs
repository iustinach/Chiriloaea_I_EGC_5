
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class OrbitCamera : Camera
{
    // Parametrii camerei orbită.
    public Vector3 Target = Vector3.Zero;
    public float Distance = 5.0f;
    public float Azimuth = 0.0f;
    public float Elevation = 20.0f;

    private const float MinDistance = 1.0f;
    private const float MaxDistance = 20.0f;
    private const float MaxElevation = 89.0f;

    private const float MouseSensitivity = 0.2f;
    private const float ZoomSensitivity = 0.5f;
    private const float RotationSpeed = 90.0f;

    // Valorile inițiale pentru Reset().
    private readonly Vector3 _initialTarget = Vector3.Zero;
    private const float InitialDistance = 5.0f;
    private const float InitialAzimuth = 0.0f;
    private const float InitialElevation = 20.0f;

    public override string Name => "Orbit";

    // Poziția camerei pe sfera din jurul țintei.
    public override Vector3 Position
    {
        get
        {
            float az = MathHelper.DegreesToRadians(Azimuth);
            float el = MathHelper.DegreesToRadians(Elevation);

            Vector3 offset = new Vector3(
                MathF.Cos(el) * MathF.Cos(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Sin(az)
            );

            return Target + Distance * offset;
        }
    }

    // Direcția camerei, întotdeauna către țintă.
    public override Vector3 Front =>
        (Target - Position).Normalized();

    // Matricea view.
    public override Matrix4 ViewMatrix =>
        Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public override void Update(
        KeyboardState keyboard,
        MouseState mouse,
        Vector2 mouseDelta,
        float dt)
    {
        // Rotație cu mouse-ul: doar când butonul stâng este apăsat.
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation -= mouseDelta.Y * MouseSensitivity;
        }

        // Rotație cu săgețile.
        if (keyboard.IsKeyDown(Keys.Left))
            Azimuth -= RotationSpeed * dt;

        if (keyboard.IsKeyDown(Keys.Right))
            Azimuth += RotationSpeed * dt;

        if (keyboard.IsKeyDown(Keys.Up))
            Elevation += RotationSpeed * dt;

        if (keyboard.IsKeyDown(Keys.Down))
            Elevation -= RotationSpeed * dt;

        // Limitarea elevației pentru a evita răsturnarea camerei.
        Elevation = MathHelper.Clamp(
            Elevation,
            -MaxElevation,
            MaxElevation
        );

        // Zoom cu rotița mouse-ului.
        Distance -= mouse.ScrollDelta.Y * ZoomSensitivity;

        Distance = MathHelper.Clamp(
            Distance,
            MinDistance,
            MaxDistance
        );
    }

    public override void Reset()
    {
        Target = _initialTarget;
        Distance = InitialDistance;
        Azimuth = InitialAzimuth;
        Elevation = InitialElevation;
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Țintă: {Target}";
        yield return $"Distanță: {Distance:F2}";
        yield return $"Azimut: {Azimuth:F2}°";
        yield return $"Elevație: {Elevation:F2}°";
        yield return $"Poziție: {Position}";
    }
}
