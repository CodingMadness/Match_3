#version 330 

in vec2 fragTexCoord;
out vec4 fragColor;

uniform float seconds;             // Time to control the wobbling animation
uniform vec2 gridSize;          // Number of tiles in x and y
uniform bool shouldWobble;
                                // Add a uniform flag to control wobbling for specific tiles


const float PI = 3.14159265359;
const float frequency = 3.0;  // Adjust the frequency of the wobble
const float amplitude = 0.03;  // Adjust the intensity of the wobble

void main() {
    // Calculate the wobbling position for the fragment based on the texture coordinates
    vec2 position = fragTexCoord;

    // Check if the tile should wobble
    if (shouldWobble) {
        position.x += amplitude * sin(2.0 * PI * frequency * seconds + fragTexCoord.y);
        position.y += amplitude * sin(2.0 * PI * frequency * seconds + fragTexCoord.x);
    }

    // Map the position to the grid
    vec2 gridPosition = floor(position * gridSize) / gridSize;

    // Output the color based on the fragment position
    // You may want to sample a texture here or calculate color based on your specific requirements.
    fragColor = vec4(gridPosition, 0.0, 1.0);
}
