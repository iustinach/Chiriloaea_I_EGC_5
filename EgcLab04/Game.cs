using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;
    // Pivotul în jurul căruia se rotește obiectul selectat.
    private Vector2 _pivot;
    private bool _hasPivot = false;
    // T5: triunghiul satelit și unghiul curent al orbitei.
    private Mesh _satelliteMesh;
    private float _satelliteAngle = 0.0f;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
        // T5: satelitul folosește un triunghi mic și nu poate fi selectat separat.
        _satelliteMesh = Shapes.Triangle();
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;
        SceneObject sel = _scene[_selected];
        float viteza = KeyboardState.IsKeyDown(Keys.LeftShift) ? 3.0f : 1.0f;
        // LeftShift accelerează mișcarea, rotația și scalarea de 3 ori.

        const float moveSpeed = 2.0f;   // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.Left))  sel.Transform.Position.X -= moveSpeed * dt * viteza;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveSpeed * dt * viteza ;
        if (KeyboardState.IsKeyDown(Keys.Down))  sel.Transform.Position.Y -= moveSpeed * dt * viteza;
        if (KeyboardState.IsKeyDown(Keys.Up))    sel.Transform.Position.Y += moveSpeed * dt * viteza;

        // TODO (T2): Q / E rotesc obiectul, Z / X îl scalează


        // Rotația se calculează în grade pe secundă.
        const float vitezaRotatie = 90.0f;
        if (KeyboardState.IsKeyDown(Keys.Q))
            sel.Transform.Rotation += vitezaRotatie * dt * viteza;
        if (KeyboardState.IsKeyDown(Keys.E))
            sel.Transform.Rotation -= vitezaRotatie * dt * viteza;

        // Scalarea este multiplicativă, pentru ca modificarea să fie proporțională cu dimensiunea actuală.
        float factorScalare = MathF.Pow(1.5f, dt * viteza);
        if (KeyboardState.IsKeyDown(Keys.Z))
        {
            sel.Transform.Scale /= factorScalare;
        }
        if (KeyboardState.IsKeyDown(Keys.X))
        {
            sel.Transform.Scale *= factorScalare;
        }

        sel.Transform.Scale.X = Math.Clamp(sel.Transform.Scale.X, 0.1f, 10.0f);
        sel.Transform.Scale.Y = Math.Clamp(sel.Transform.Scale.Y, 0.1f, 10.0f);
        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O))   _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F))   _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R))   ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // TODO (T3): la clic stânga, obiectul selectat se mută în punctul din lume aflat sub cursor
        //            (folosiți ScreenToWorld; Depth nu se schimbă)
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            Vector2 mousePixels = MouseState.Position;
            Vector2 worldPosition = ScreenToWorld(mousePixels);

            // Păstrăm adâncimea obiectului și schimbăm doar poziția X și Y.
            sel.Transform.Position = new Vector2(worldPosition.X, worldPosition.Y);
        }

        // TODO (T4, facultativ): clic dreapta = pivot; tasta P ținută = rotație în jurul pivotului
        // Clic dreapta stabilește pivotul în punctul din lume aflat sub cursor.
        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivot = ScreenToWorld(MouseState.Position);
            _hasPivot = true;
        }
        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }

        if (_hasPivot && KeyboardState.IsKeyDown(Keys.P))
        {
            // Rotația este de 90 de grade pe secundă.
            float unghi = MathHelper.DegreesToRadians(90.0f * dt);

            // Calculăm poziția obiectului față de pivot.
            Vector2 diferenta = sel.Transform.Position - _pivot;

            float cos = MathF.Cos(unghi);
            float sin = MathF.Sin(unghi);

            // Rotim poziția obiectului în jurul pivotului.
            sel.Transform.Position = new Vector2(
                _pivot.X + diferenta.X * cos - diferenta.Y * sin,
                _pivot.Y + diferenta.X * sin + diferenta.Y * cos
            );

            // Rotim și obiectul propriu-zis cu 90 de grade pe secundă.
            sel.Transform.Rotation += 90.0f * dt;
        }
        // T5: o rotație completă a orbitei durează 4 secunde.
        _satelliteAngle += MathHelper.TwoPi * dt / 4.0f;

        // Păstrăm unghiul într-un interval de la 0 la 2π.
        if (_satelliteAngle >= MathHelper.TwoPi)
            _satelliteAngle -= MathHelper.TwoPi;

    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // TODO (T5, facultativ): satelitul obiectului selectat
        SceneObject parinte = _scene[_selected];

        Matrix4 modelSatelit =
            Matrix4.CreateScale(0.3f) *
            Matrix4.CreateTranslation(1.2f, 0.0f, 0.0f) *
            Matrix4.CreateRotationZ(_satelliteAngle) *
            parinte.Transform.ModelMatrix;

        _shader.SetMatrix4("uModel", modelSatelit);
        _shader.SetColor("uTint", new Color4(1.0f, 0.85f, 0.2f, 1.0f));
        _satelliteMesh.Draw();
        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    /// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // TODO (T3): pasul 1 (pixel → NDC), apoi pasul 2 (NDC → lume, cu inversa lui view * projection)
        // Pasul 1: transformăm coordonatele pixelilor în coordonate NDC,
        // unde centrul ecranului este (0, 0), iar intervalul este [-1, 1].
        float x = 2.0f * mousePixels.X / ClientSize.X - 1.0f;
        float y = 1.0f - 2.0f * mousePixels.Y / ClientSize.Y;


        // Coordonata Z = -1 reprezintă planul apropiat (near plane) în NDC,
        // iar Z = 1 reprezintă planul îndepărtat (far plane). Folosim ambele
        // valori pentru a construi raza care trece prin poziția cursorului.
        // Inversăm axa Y deoarece coordonatele mouse-ului cresc în jos,
        // în timp ce coordonatele lumii cresc în sus.
        Vector4 ndc = new Vector4(x, y, -1.0f, 1.0f);
        // Pasul 2: transformăm din NDC în coordonate ale lumii
        // folosind inversa matricei View * Projection.
        Matrix4 viewProjection = _camera.ProjectionMatrix * _camera.ViewMatrix;
        Matrix4 inverseViewProjection = viewProjection.Inverted();

        Vector4 world = Vector4.TransformRow(ndc, inverseViewProjection);
        if (MathF.Abs(world.W) > 0.00001f)
        {
            world.X /= world.W;
            world.Y /= world.W;
        }

        return new Vector2(world.X, world.Y);
    }

    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
