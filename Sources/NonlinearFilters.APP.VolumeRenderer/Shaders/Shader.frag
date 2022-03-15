#version 330 core

precision highp int;
precision highp float;

uniform highp sampler3D volume;
uniform highp sampler1D transfer_fcn;
uniform vec3 volume_size;

in vec2 pos;
in vec3 ray_dir;
flat in vec3 ray_pos;

out vec4 outColor;

// source: https://gamedev.stackexchange.com/questions/18436/most-efficient-aabb-vs-ray-collision-algorithms
vec2 intersect_box(vec3 size)
{
	vec3 box_min = -size * 0.5;
	vec3 box_max = size * 0.5;

	vec3 inv_dir = 1 / ray_dir;
	vec3 t0 = (box_min - ray_pos) * inv_dir;
	vec3 t1 = (box_max - ray_pos) * inv_dir;

	float tmin = max(max(min(t0.x, t1.x), min(t0.y, t1.y)), min(t0.z, t1.z));
	float tmax = min(min(max(t0.x, t1.x), max(t0.y, t1.y)), max(t0.z, t1.z));

	return vec2(tmin, tmax);
}

void main()
{
	vec2 t_hit = intersect_box(volume_size);
	if (t_hit.x > t_hit.y)
	{
		discard;
	}

	t_hit.x = max(t_hit.x, 0.0);

	vec3 dt_vec = volume_size * abs(ray_dir);
	float dt = 1 / max(dt_vec.x, max(dt_vec.y, dt_vec.z)) * 3;

	vec3 inv_size = 1.0 / volume_size;
	vec3 p = ray_pos + (volume_size * 0.5) + t_hit.x * ray_dir;
	
	//vec4 color = vec4(vec3(0.0), 0.0);

	int val = 0;
	for (float t = t_hit.x; t < t_hit.y; t += dt)
	{
		int new_val = int(texture(volume, p * inv_size).r * 255);
		val = max(val, new_val);

		/*
		vec4 val_color = vec4(texture(transfer_fcn, val).rgb, val);

		color.rgb += (1.0 - color.a) * val_color.a * val_color.rgb;
		color.a += (1.0 - color.a) * val_color.a;

		if (color.a >= 0.95) {
			break;
		}
		*/

		p += ray_dir * dt;
	}

	outColor = vec4(vec3(val / 255.0), 1.0);
}
