#version 330 core
layout (location = 0) in vec3 position;

uniform vec4 view;
uniform vec2 resolution;

out vec2 pos;
out vec3 ray_dir;
flat out vec3 ray_pos;

mat3 rotate(vec3 angles)
{
	float cosa = cos(angles.x);
	float sina = sin(angles.x);

	float cosb = cos(angles.y);
	float sinb = sin(angles.y);

	float cosc = cos(angles.z);
	float sinc = sin(angles.z);

	//roll, pitch, yaw
/*
	return mat3(
		cosa * cosb, cosa * sinb * sinc - sina * cosc, cosa * sinb * cosc + sina * sinc,
		sina * cosb, sina * sinb * sinc + cosa * cosc, sina * sinb * cosc - cosa * sinc,
		-sinb, cosb * sinc, cosb * cosc
	);*/

	return mat3(
		cosb * cosc, sina * sinb * cosc - cosa * sinc, cosa * sinb * cosc + sina * sinc,
		cosb * sinc, sina * sinb * sinc + cosa * cosc, cosa * sinb * sinc - sina * cosc,
		-sinb, sina * cosb, cosa * cosb
	);
}

void main()
{
	gl_Position = vec4(position, 1);

	vec2 NDC = position.xy * normalize(resolution);

	mat3 rot = rotate(view.xyz);

	ray_dir = rot * vec3(NDC, 1.0);
	ray_pos = rot * vec3(0.0, 0.0, -view.w);

	pos = (position.xy + 1) * 0.5;
}
