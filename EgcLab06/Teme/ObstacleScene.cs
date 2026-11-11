
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Varianta B: joc de evitare a obstacolelor.
/// Jucătorul se deplasează pe trei benzi, sare peste obstacole joase
/// și evită obstacolele înalte.
/// </summary>
public sealed class ObstacleScene : Scene
{
    private const string RestartAction = "Restart";

    private const float LaneSpacing = 2.2f;
    private const float PlayerZ = 4.0f;
    private const float PlayerHeight = 1.4f;
    private const float JumpSpeed = 9.0f;
    private const float Gravity = 22.0f;
    private const float LaneChangeDuration = 0.16f;

    private static float _bestScore;

    private readonly Random _random = new();

    private readonly List<SceneObject> _roadMarks = new();
    private readonly List<Obstacle> _obstacles = new();

    private Mesh? _cubeMesh;

    private SceneObject? _playerPivot;
    private SceneObject? _playerBody;

    private Tween? _laneTween;

    private int _lane = 1;
    private float _verticalSpeed;
    private float _score;
    private float _worldSpeed = 9.0f;
    private float _spawnTimer = 1.0f;
    private bool _gameOver;

    private sealed class Obstacle
    {
        public SceneObject Object { get; }
        public float Height { get; }

        public Obstacle(SceneObject obj, float height)
        {
            Object = obj;
            Height = height;
        }
    }

    public override string Name => "Evitare obstacole";

    public override string Status =>
        _gameOver
            ? $"JOC TERMINAT | Scor: {(int)_score} | R = restart"
            : $"Scor: {(int)_score} | Record: {(int)_bestScore}";

    public override IEnumerable<string> ActionNames
    {
        get
        {
            yield return $"{Actions.Left} (A / săgeată stânga)";
            yield return $"{Actions.Right} (D / săgeată dreapta)";
            yield return $"{Actions.Jump} (Space)";
            yield return "Restart (R)";
        }
    }

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 8.0f, 14.0f);
        CameraTarget = new Vector3(0.0f, 0.5f, -9.0f);

        input.Bind(Actions.Left, Keys.Left, Keys.A);
        input.Bind(Actions.Right, Keys.Right, Keys.D);
        input.Bind(Actions.Jump, Keys.Space);
        input.Bind(RestartAction, Keys.R);

        _cubeMesh = AddMesh(Shapes.ShadedCube());

        // Șoseaua.
        SceneObject road = AddObject("Șosea", _cubeMesh);
        road.Transform.Position = new Vector3(0.0f, -0.2f, -10.0f);
        road.Transform.Scale = new Vector3(8.0f, 0.3f, 55.0f);
        road.Tint = new Color4(0.16f, 0.17f, 0.20f, 1.0f);

        // Marginile șoselei.
        AddRoadEdge(-4.0f);
        AddRoadEdge(4.0f);

        // Marcajele dintre benzi.
        for (int laneLine = -1; laneLine <= 1; laneLine += 2)
        {
            for (int i = 0; i < 12; i++)
            {
                SceneObject mark = AddObject("Marcaj șosea", _cubeMesh);
                mark.Transform.Position = new Vector3(
                    laneLine * LaneSpacing * 0.5f,
                    -0.025f,
                    -28.0f + i * 4.5f);

                mark.Transform.Scale = new Vector3(0.07f, 0.025f, 2.0f);
                mark.Tint = new Color4(0.9f, 0.9f, 0.75f, 1.0f);
                _roadMarks.Add(mark);
            }
        }

        // Pivotul jucătorului și corpul său copil.
        // Mișcarea pivotului deplasează automat și corpul.
        _playerPivot = AddObject("Pivot jucător", null);
        _playerPivot.Transform.Position = new Vector3(0.0f, 0.0f, PlayerZ);

        _playerBody = AddObject("Jucător", _cubeMesh, _playerPivot);
        _playerBody.Transform.Position = new Vector3(0.0f, PlayerHeight * 0.5f, 0.0f);
        _playerBody.Transform.Scale = new Vector3(0.85f, PlayerHeight, 0.8f);
        _playerBody.Tint = new Color4(0.15f, 0.65f, 1.0f, 1.0f);

        ResetGame();
    }

    private void AddRoadEdge(float x)
    {
        SceneObject edge = AddObject("Margine șosea", _cubeMesh);
        edge.Transform.Position = new Vector3(x, 0.0f, -10.0f);
        edge.Transform.Scale = new Vector3(0.12f, 0.12f, 55.0f);
        edge.Tint = new Color4(0.9f, 0.75f, 0.2f, 1.0f);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        if (input.WasPressed(RestartAction))
        {
            ResetGame();
            return;
        }

        if (_gameOver || dt <= 0.0f)
            return;

        UpdateLaneMovement(dt, input);
        UpdateJump(dt, input);
        UpdateDifficulty(dt);
        UpdateRoadMarks(dt);
        UpdateObstacles(dt);
        CheckCollisions();

        _score += dt * 10.0f;

        if (_score > _bestScore)
            _bestScore = _score;
    }

    private void UpdateLaneMovement(float dt, Input input)
    {
        if (_playerPivot == null)
            return;

        if (input.WasPressed(Actions.Left) && _lane > 0)
        {
            _lane--;
            StartLaneTween();
        }
        else if (input.WasPressed(Actions.Right) && _lane < 2)
        {
            _lane++;
            StartLaneTween();
        }

        if (_laneTween != null)
        {
            _laneTween.Update(dt);

            Vector3 position = _playerPivot.Transform.Position;
            position.X = _laneTween.Value.X;
            _playerPivot.Transform.Position = position;

            if (_laneTween.IsFinished)
                _laneTween = null;
        }
    }

    private void StartLaneTween()
    {
        if (_playerPivot == null)
            return;

        float currentX = _playerPivot.Transform.Position.X;
        float targetX = (_lane - 1) * LaneSpacing;

        _laneTween = new Tween(
            new Vector3(currentX, 0.0f, 0.0f),
            new Vector3(targetX, 0.0f, 0.0f),
            LaneChangeDuration,
            Easing.EaseOut);
    }

    private void UpdateJump(float dt, Input input)
    {
        if (_playerPivot == null)
            return;

        if (input.WasPressed(Actions.Jump) &&
            _playerPivot.Transform.Position.Y <= 0.001f)
        {
            _verticalSpeed = JumpSpeed;
        }

        if (_verticalSpeed != 0.0f ||
            _playerPivot.Transform.Position.Y > 0.0f)
        {
            Vector3 position = _playerPivot.Transform.Position;

            _verticalSpeed -= Gravity * dt;
            position.Y += _verticalSpeed * dt;

            if (position.Y <= 0.0f)
            {
                position.Y = 0.0f;
                _verticalSpeed = 0.0f;
            }

            _playerPivot.Transform.Position = position;
        }
    }

    private void UpdateDifficulty(float dt)
    {
        _worldSpeed = MathF.Min(9.0f + _score * 0.025f, 20.0f);

        _spawnTimer -= dt;

        if (_spawnTimer <= 0.0f)
        {
            SpawnObstacle();

            float interval = MathF.Max(0.55f, 1.2f - _score * 0.0015f);
            _spawnTimer = interval + (float)_random.NextDouble() * 0.3f;
        }
    }

    private void UpdateRoadMarks(float dt)
    {
        foreach (SceneObject mark in _roadMarks)
        {
            Vector3 position = mark.Transform.Position;
            position.Z += _worldSpeed * dt;

            if (position.Z > 9.0f)
                position.Z -= 54.0f;

            mark.Transform.Position = position;
        }
    }

    private void SpawnObstacle()
    {
        if (_cubeMesh == null)
            return;

        int lane = _random.Next(0, 3);

        // Obstacolele pot fi joase sau înalte.
        bool lowObstacle = _random.NextDouble() < 0.55;
        float height = lowObstacle ? 0.55f : 2.3f;

        SceneObject obj = AddObject("Obstacol", _cubeMesh);

        obj.Transform.Position = new Vector3(
            (lane - 1) * LaneSpacing,
            height * 0.5f,
            -32.0f);

        obj.Transform.Scale = new Vector3(1.15f, height, 0.9f);

        obj.Tint = lowObstacle
            ? new Color4(0.95f, 0.65f, 0.1f, 1.0f)
            : new Color4(0.9f, 0.15f, 0.15f, 1.0f);

        _obstacles.Add(new Obstacle(obj, height));
    }

    private void UpdateObstacles(float dt)
    {
        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            Obstacle obstacle = _obstacles[i];

            Vector3 position = obstacle.Object.Transform.Position;
            position.Z += _worldSpeed * dt;
            obstacle.Object.Transform.Position = position;

            // Eliminăm obstacolele care au trecut de jucător.
            if (position.Z > 11.0f)
            {
                Objects.Remove(obstacle.Object);
                _obstacles.RemoveAt(i);
            }
        }
    }

    private void CheckCollisions()
    {
        if (_playerPivot == null)
            return;

        float playerX = _playerPivot.Transform.Position.X;
        float playerY = _playerPivot.Transform.Position.Y;

        foreach (Obstacle obstacle in _obstacles)
        {
            Vector3 obstaclePosition = obstacle.Object.Transform.Position;

            bool overlapsX =
                MathF.Abs(playerX - obstaclePosition.X) < 1.0f;

            bool overlapsZ =
                MathF.Abs(PlayerZ - obstaclePosition.Z) < 0.85f;

            // Pentru obstacolele joase, săritura poate evita coliziunea.
            bool overlapsY =
                playerY < obstacle.Height - 0.05f;

            if (overlapsX && overlapsZ && overlapsY)
            {
                _gameOver = true;
                _bestScore = MathF.Max(_bestScore, _score);
                break;
            }
        }
    }

    private void ResetGame()
    {
        if (_playerPivot == null)
            return;

        foreach (Obstacle obstacle in _obstacles)
            Objects.Remove(obstacle.Object);

        _obstacles.Clear();

        _lane = 1;
        _laneTween = null;
        _verticalSpeed = 0.0f;
        _score = 0.0f;
        _worldSpeed = 9.0f;
        _spawnTimer = 1.0f;
        _gameOver = false;

        _playerPivot.Transform.Position = new Vector3(0.0f, 0.0f, PlayerZ);

        if (_playerBody != null)
        {
            _playerBody.Transform.Position =
                new Vector3(0.0f, PlayerHeight * 0.5f, 0.0f);
        }

        foreach (SceneObject mark in _roadMarks)
        {
            Vector3 position = mark.Transform.Position;

            // Reașezăm marcajele în pozițiile inițiale.
            // Coordonatele X și Y rămân neschimbate.
            int index = _roadMarks.IndexOf(mark);
            position.Z = -28.0f + (index / 2) * 4.5f;

            mark.Transform.Position = position;
        }
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe())
            yield return line;

        yield return "Varianta B - Evitare obstacole";
        yield return $"Scor: {(int)_score}";
        yield return $"Record: {(int)_bestScore}";
        yield return $"Viteză: {_worldSpeed:F1}";
        yield return $"Bandă: {_lane + 1} din 3";
        yield return _gameOver ? "Stare: joc terminat" : "Stare: în desfășurare";
        yield return "A/D sau săgeți: schimbă banda";
        yield return "Space: salt";
        yield return "R: restart";
    }
}
