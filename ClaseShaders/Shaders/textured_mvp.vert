#version 330 core

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUV;

out vec2 vUV;

uniform mat4 uMVP; // Matriz Model-View-Projection

void main()
{
    vUV = aUV;

    // Convertimos vec2 -> vec4 (z=0, w=1) y aplicamos la matriz
    gl_Position = uMVP * vec4(aPos, 0.0, 1.0);
}
