#version 330 core

in vec3 vColor; // Ya llega interpolado desde el vertex shader.
out vec4 FragColor;

void main()
{
    FragColor = vec4(vColor, 1.0);
}
