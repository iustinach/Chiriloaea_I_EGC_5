
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public sealed class CarScene : Scene
{
    private const string MoveLeft = "Mașina la stânga";
    private const string MoveRight = "Mașina la dreapta";

    private SceneObject? _car;
    private SceneObject? _ball;
    private Tween? _ballRotation;

    private float _ballAngle;
    private readonly float _carSpeed = 5.0f;

    public override string Name => "Mașina și mingea";

    public override IEnumerable<string> ActionNames
    {
        get
        {
            yield return "A / săgeată stânga: mașina la stânga";
            yield return "D / săgeată dreapta: mașina la dreapta";
        }
    }

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 8.0f, 13.0f);
        CameraTarget = new Vector3(0.0f, 0.5f, 0.0f);

        input.Bind(MoveLeft, Keys.A, Keys.Left);
        input.Bind(MoveRight, Keys.D, Keys.Right);

        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh sphere = AddMesh(Shapes.ShadedSphere(0.5f, 24, 16));
        Mesh cylinder = AddMesh(Shapes.ShadedCylinder(0.5f, 0.25f, 24));

        // Podeaua.
        SceneObject floor = AddObject("Podea", cube);
        floor.Transform.Position = new Vector3(0.0f, -0.25f, 0.0f);
        floor.Transform.Scale = new Vector3(14.0f, 0.3f, 10.0f);
        floor.Tint = new Color4(0.25f, 0.3f, 0.25f, 1.0f);

        // Obiectul părinte al mașinii.
        _car = AddObject("Mașină", null);
        _car.Transform.Position = Vector3.Zero;

        // Caroseria.
        AddCarPart(cube, "Caroserie",
            new Vector3(0.0f, 0.75f, 0.0f),
            new Vector3(2.2f, 0.55f, 1.2f),
            new Color4(0.85f, 0.1f, 0.1f, 1.0f));

        // Cabina.
        AddCarPart(cube, "Cabină",
            new Vector3(0.0f, 1.2f, -0.1f),
            new Vector3(1.1f, 0.5f, 0.95f),
            new Color4(0.08f, 0.22f, 0.32f, 1.0f));

        // Capota.
        AddCarPart(cube, "Capotă",
            new Vector3(0.0f, 0.91f, 0.48f),
            new Vector3(1.9f, 0.18f, 0.55f),
            new Color4(0.95f, 0.08f, 0.06f, 1.0f));

        // Portbagaj.
        AddCarPart(cube, "Portbagaj",
            new Vector3(0.0f, 0.91f, -0.48f),
            new Vector3(1.9f, 0.18f, 0.45f),
            new Color4(0.8f, 0.04f, 0.04f, 1.0f));

        // Parbriz și lunetă.
        AddCarPart(cube, "Parbriz",
            new Vector3(0.0f, 1.28f, 0.28f),
            new Vector3(0.95f, 0.32f, 0.08f),
            new Color4(0.15f, 0.65f, 0.9f, 1.0f));

        AddCarPart(cube, "Lunetă",
            new Vector3(0.0f, 1.28f, -0.43f),
            new Vector3(0.95f, 0.32f, 0.08f),
            new Color4(0.15f, 0.65f, 0.9f, 1.0f));

        // Faruri.
        AddCarPart(cube, "Far stânga",
            new Vector3(-0.72f, 0.83f, 0.79f),
            new Vector3(0.35f, 0.16f, 0.06f),
            new Color4(1.0f, 1.0f, 0.75f, 1.0f));

        AddCarPart(cube, "Far dreapta",
            new Vector3(0.72f, 0.83f, 0.79f),
            new Vector3(0.35f, 0.16f, 0.06f),
            new Color4(1.0f, 1.0f, 0.75f, 1.0f));

        // Grila frontală.
        AddCarPart(cube, "Grilă",
            new Vector3(0.0f, 0.68f, 0.79f),
            new Vector3(0.65f, 0.16f, 0.05f),
            new Color4(0.04f, 0.04f, 0.04f, 1.0f));

        // Stopuri.
        AddCarPart(cube, "Stop stânga",
            new Vector3(-0.72f, 0.82f, -0.77f),
            new Vector3(0.3f, 0.16f, 0.06f),
            new Color4(1.0f, 0.0f, 0.0f, 1.0f));

        AddCarPart(cube, "Stop dreapta",
            new Vector3(0.72f, 0.82f, -0.77f),
            new Vector3(0.3f, 0.16f, 0.06f),
            new Color4(1.0f, 0.0f, 0.0f, 1.0f));

        // Spoiler.
        AddCarPart(cube, "Suport spoiler",
            new Vector3(0.0f, 1.2f, -0.78f),
            new Vector3(0.12f, 0.25f, 0.12f),
            new Color4(0.08f, 0.08f, 0.08f, 1.0f));

        AddCarPart(cube, "Spoiler",
            new Vector3(0.0f, 1.34f, -0.78f),
            new Vector3(2.0f, 0.1f, 0.35f),
            new Color4(0.08f, 0.08f, 0.08f, 1.0f));

        // Praguri laterale.
        AddCarPart(cube, "Prag stânga",
            new Vector3(-1.05f, 0.55f, 0.0f),
            new Vector3(0.12f, 0.12f, 0.9f),
            new Color4(0.08f, 0.08f, 0.08f, 1.0f));

        AddCarPart(cube, "Prag dreapta",
            new Vector3(1.05f, 0.55f, 0.0f),
            new Vector3(0.12f, 0.12f, 0.9f),
            new Color4(0.08f, 0.08f, 0.08f, 1.0f));

        // Cele patru roți.
        AddWheel(cylinder, -0.85f, 0.45f, 0.65f);
        AddWheel(cylinder, 0.85f, 0.45f, 0.65f);
        AddWheel(cylinder, -0.85f, 0.45f, -0.65f);
        AddWheel(cylinder, 0.85f, 0.45f, -0.65f);

        // Mingea rămâne copil al mașinii.
        _ball = AddObject("Minge", sphere, _car);
        _ball.Transform.Position = new Vector3(2.0f, 1.2f, 0.0f);
        _ball.Transform.Scale = new Vector3(0.55f);
        _ball.Tint = new Color4(1.0f, 0.8f, 0.05f, 1.0f);

        _ballRotation = new Tween(
            Vector3.Zero,
            new Vector3(0.0f, 360.0f, 0.0f),
            3.0f,
            Easing.Linear,
            true);
    }

    // Adaugă o componentă ca obiect copil al mașinii.
    private void AddCarPart(
        Mesh mesh,
        string name,
        Vector3 position,
        Vector3 scale,
        Color4 color)
    {
        SceneObject part = AddObject(name, mesh, _car);
        part.Transform.Position = position;
        part.Transform.Scale = scale;
        part.Tint = color;
    }

    private void AddWheel(Mesh cylinder, float x, float y, float z)
    {
        // Anvelopă.
        SceneObject wheel = AddObject("Roată", cylinder, _car);
        wheel.Transform.Position = new Vector3(x, y, z);
        wheel.Transform.Scale = new Vector3(0.55f, 0.25f, 0.55f);
        wheel.Transform.Rotation = new Vector3(0.0f, 0.0f, 90.0f);
        wheel.Tint = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        if (_car == null || _ball == null || _ballRotation == null)
            return;

        // Mișcarea mașinii.
        float direction = input.Axis(MoveLeft, MoveRight);

        Vector3 carPosition = _car.Transform.Position;
        carPosition.X += direction * _carSpeed * dt;
        carPosition.X = MathHelper.Clamp(carPosition.X, -4.5f, 4.5f);
        _car.Transform.Position = carPosition;

        // Animația mingii.
        _ballRotation.Update(dt);
        _ballAngle = _ballRotation.Value.Y;

        float angleRadians = MathHelper.DegreesToRadians(_ballAngle);
        float radius = 2.0f;

        _ball.Transform.Position = new Vector3(
            MathF.Cos(angleRadians) * radius,
            1.2f,
            MathF.Sin(angleRadians) * radius);

        _ball.Transform.Rotation = new Vector3(
            _ballAngle * 2.0f,
            _ballAngle,
            _ballAngle * 0.5f);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe())
            yield return line;

        yield return "Scena: Mașina și mingea";
        yield return "A / săgeată stânga: deplasare stânga";
        yield return "D / săgeată dreapta: deplasare dreapta";
        yield return "Mingea se rotește în jurul mașinii";
        yield return $"Poziția mașinii: {_car?.Transform.Position.X:F2}";
    }
}
