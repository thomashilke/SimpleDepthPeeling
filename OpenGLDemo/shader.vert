#version 330 core
layout (location = 0) in vec3 aPosition;

out vec4 vertexColor;

uniform vec4 ourColor;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
    vertexColor = ourColor;
}
