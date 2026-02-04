#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uTex;

// Luz “básica”
uniform vec3 uLightDir;    // dirección HACIA la luz (en el mismo espacio que la normal)
uniform vec3 uLightColor;  // color de la luz (ej. blanco)
uniform vec3 uBaseColor;   // color base del material (tinte)
uniform float uAmbient;    // luz ambiente mínima [0..1]

void main()
{
    // Para este primer ejercicio: normal constante del plano (apunta a +Z)
    vec3 N = vec3(0.0, 0.0, 1.0);

    // uLightDir la interpretamos como “hacia la luz”
    vec3 L = normalize(uLightDir);

    // Difusa (Lambert): cuánta luz pega según el ángulo
    float diff = max(dot(N, L), 0.0);

    // Intensidad final con ambiente
    float intensity = clamp(uAmbient + diff, 0.0, 1.0);

    // Color del material = textura * tinte
    vec3 tex = texture(uTex, vUV).rgb;
    vec3 base = tex * uBaseColor;

    // Iluminación
    vec3 color = base * (uLightColor * intensity);

    FragColor = vec4(color, 1.0);
}
