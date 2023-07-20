#version 330 core

in vec4 position;
in vec2 uv;
out vec4 FragColor;

uniform sampler2D image;

float hat(float lowerBound, float upperBound, float width, float x)
{
    return smoothstep(lowerBound - width, lowerBound + width, x) - smoothstep(upperBound - width , upperBound + width , x);
}

void main()
{
    float d = sqrt(texture(image, uv).r);
    float h = hat(10.0, 20.0, 1, d);
    FragColor = vec4(h, h, h, 1.0);
    d /= 500.0;
    //FragColor = vec4(d, d, d, 1.0);
}
