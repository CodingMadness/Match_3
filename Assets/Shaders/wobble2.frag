#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Output fragment color
out vec4 finalColor;

uniform bool shouldWobble;
uniform float seconds;
uniform vec2 size;
uniform float freqX;
uniform float freqY;
uniform float ampX;
uniform float ampY;
uniform float speedX;
uniform float speedY;

const float PI = 3.14159265359;
const float frequency = 3.0;  // Adjust the frequency of the wobble
const float amplitude = 0.03;  // Adjust the intensity of the wobble

void Wobble1()
{
    float pixelWidth = 1.0 / size.x;
    float pixelHeight = 1.0 / size.y;
    float aspect = pixelHeight / pixelWidth;
    float boxLeft = 0.5;
    float boxTop = 0.5;

    vec2 p = fragTexCoord;

    if (shouldWobble)
    {
        p.x += cos((fragTexCoord.y - boxTop) * freqX / (pixelWidth * 750.0) + (seconds * speedX)) * frequency * pixelWidth;
        p.y += sin((fragTexCoord.x - boxLeft) * freqY * aspect / (pixelHeight * 750.0) + (seconds * speedY)) * ampY * pixelHeight;
    }
    finalColor = texture(texture0, p) * colDiffuse * fragColor;
}


void main()
{
    Wobble1();
}

