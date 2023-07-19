#version 330 core

out vec2 uv;

struct Vertex
{
    vec2 position;
    vec2 uv;
};

Vertex vertices[4] = Vertex[4](
    Vertex(vec2(-1.0, -1.0), vec2(0.0, 0.0)),
    Vertex(vec2(1.0, -1.0), vec2(1.0, 0.0)),
    Vertex(vec2(1.0, 1.0), vec2(1.0, 1.0)),
    Vertex(vec2(-1.0, 1.0), vec2(0.0, 1.0))
);

void main()
{
    gl_Position = vec4(vertices[gl_VertexID].position, 0.0, 1.0);
    uv = vertices[gl_VertexID].uv;
}
