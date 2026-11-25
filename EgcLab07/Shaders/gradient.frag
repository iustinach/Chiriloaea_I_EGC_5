
#version 330 core

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform vec3 uColorA;
uniform vec3 uColorB;
uniform float uAngle;
uniform int uSteps;

out vec4 FragColor;

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec2 dir = vec2(cos(radians(uAngle)), sin(radians(uAngle)));

    float t = clamp(dot(vUV - 0.5, dir) + 0.5, 0.0, 1.0);

    if (uSteps >= 2)
    {
        float n = float(uSteps);
        t = min(floor(t * n), n - 1.0) / (n - 1.0);
    }

    vec3 color = mix(uColorA, uColorB, t);

    FragColor = vec4(color * vColor, 1.0);
}
