#version 330 core

in vec4 position;
in vec2 uv;
out vec4 FragColor;

uniform sampler2D image;

void main()
{
    FragColor = texture(image, uv);
}
