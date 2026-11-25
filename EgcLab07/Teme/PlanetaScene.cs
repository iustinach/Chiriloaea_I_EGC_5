using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class PlanetaScene : Scene
{
    private const string NewPlanet = "planeta: generează alta";
    private const string ToggleClouds = "planeta: nori";

    private Material _planet = null!;
    private Tween _seedTween = null!;
    private readonly Random _random = new();

    private bool _cloudsVisible = true;

    public override string Name => "planeta";

    public override string Status =>
        $"nori: {(_cloudsVisible ? "activați" : "opriți")}";

    public override IEnumerable<string> ActionNames =>
        new[] { NewPlanet, ToggleClouds };

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 0.0f, 4.0f);
        CameraTarget = Vector3.Zero;

        input.Bind(NewPlanet, Keys.N);
        input.Bind(ToggleClouds, Keys.Space);

        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 96, 48));

        _planet = AddMaterial("planetă", "procedural.vert", "planet.frag")
            .Param("uSeaLevel", 0.40f, 0.65f, 0.50f)
            .Param("uCloudScale", 2.0f, 12.0f, 6.0f)
            .Param("uCloudOpacity", 0.0f, 1.0f, 0.75f)
            .Param("uIceLatitude", 0.65f, 0.95f, 0.82f);

        Vector3 initialSeed = new Vector3(2.3f, 5.7f, 1.4f);

        _planet.Uniforms.Set("uSeed", initialSeed);
        _planet.Uniforms.Set("uClouds", true);

        _seedTween = new Tween(
            initialSeed,
            initialSeed,
            2.0f,
            Easing.EaseInOut
        );

        SceneObject planet = AddObject("planeta", sphere, _planet);
        planet.Transform.Scale = new Vector3(1.5f);
        planet.Spin = new Vector3(0.0f, 12.0f, 0.0f);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        if (input.WasPressed(NewPlanet))
        {
            Vector3 newSeed = new Vector3(
                _random.NextSingle() * 100.0f,
                _random.NextSingle() * 100.0f,
                _random.NextSingle() * 100.0f
            );

            _seedTween.GoTo(newSeed, 2.0f);
            ConsolePanel.PrintMessage("Se generează o planetă nouă!");
        }

        if (input.WasPressed(ToggleClouds))
        {
            _cloudsVisible = !_cloudsVisible;
            _planet.Uniforms.Set("uClouds", _cloudsVisible);
        }

        _seedTween.Update(dt);
        _planet.Uniforms.Set("uSeed", _seedTween.Value);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe())
            yield return line;

        yield return $"Nori            : {(_cloudsVisible ? "activați" : "opriți")}";
        yield return $"Sămânță         : {_seedTween.Value}";
    }
}

