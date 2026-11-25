#version 330 core

in vec3 vColor;
in vec2 vUV;
in vec3 vLocalPos;

uniform bool uShowUV;
uniform float uTime;

uniform vec3 uSeed;
uniform float uSeaLevel;
uniform float uCloudScale;
uniform float uCloudOpacity;
uniform float uIceLatitude;
uniform bool uClouds;

out vec4 FragColor;

// Hash 3D
float hash(vec3 p)
{
    p = fract(p * 0.3183099 + vec3(0.1));
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Zgomot 3D
float noise3D(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);

    f = f * f * (3.0 - 2.0 * f);

    float n000 = hash(i);
    float n100 = hash(i + vec3(1, 0, 0));
    float n010 = hash(i + vec3(0, 1, 0));
    float n110 = hash(i + vec3(1, 1, 0));

    float n001 = hash(i + vec3(0, 0, 1));
    float n101 = hash(i + vec3(1, 0, 1));
    float n011 = hash(i + vec3(0, 1, 1));
    float n111 = hash(i + vec3(1, 1, 1));

    float x00 = mix(n000, n100, f.x);
    float x10 = mix(n010, n110, f.x);
    float x01 = mix(n001, n101, f.x);
    float x11 = mix(n011, n111, f.x);

    float y0 = mix(x00, x10, f.y);
    float y1 = mix(x01, x11, f.y);

    return mix(y0, y1, f.z);
}

// Fractal Brownian Motion
float fbm(vec3 p)
{
    float sum = 0.0;
    float amplitude = 0.5;

    for (int i = 0; i < 5; i++)
    {
        sum += amplitude * noise3D(p);
        p *= 2.0;
        amplitude *= 0.5;
    }

    return sum;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec3 p = normalize(vLocalPos);

    float terrain = fbm(p * 3.0 + uSeed);

    vec3 ocean = vec3(0.02, 0.20, 0.55);
    vec3 beach = vec3(0.85, 0.75, 0.45);
    vec3 grass = vec3(0.20, 0.48, 0.16);
    vec3 forest = vec3(0.04, 0.25, 0.10);
    vec3 mountain = vec3(0.38, 0.34, 0.29);
    vec3 snow = vec3(0.95, 0.97, 1.0);

    vec3 color = ocean;

    color = mix(color, beach, smoothstep(uSeaLevel - 0.04, uSeaLevel + 0.01, terrain));
    color = mix(color, grass, smoothstep(uSeaLevel + 0.02, uSeaLevel + 0.12, terrain));
    color = mix(color, forest, smoothstep(0.52, 0.62, terrain));
    color = mix(color, mountain, smoothstep(0.62, 0.76, terrain));
    color = mix(color, snow, smoothstep(0.76, 0.84, terrain));

    // Calote polare cu margine neregulata
    float polarNoise = fbm(p * 9.0 + uSeed);
    float latitude = abs(p.y) + (polarNoise - 0.5) * 0.12;
    float ice = smoothstep(uIceLatitude - 0.03, uIceLatitude + 0.03, latitude);
    color = mix(color, snow, ice);

    // Nori animati
    if (uClouds)
    {
        vec3 cloudPos = p * uCloudScale
                      + uSeed
                      + vec3(uTime * 0.04, 0.0, uTime * 0.02);

        float clouds = fbm(cloudPos);
        float cloudMask = smoothstep(0.57, 0.70, clouds);

        color = mix(color, vec3(1.0), cloudMask * uCloudOpacity);
    }

    FragColor = vec4(color * vColor, 1.0);
}