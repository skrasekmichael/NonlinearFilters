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
vec2 intersect_box(vec3 half_size)
{
	vec3 inv_dir = 1 / ray_dir;
	vec3 t0 = (-half_size - ray_pos) * inv_dir;
	vec3 t1 = (half_size - ray_pos) * inv_dir;

	float tmin = max(max(min(t0.x, t1.x), min(t0.y, t1.y)), min(t0.z, t1.z));
	float tmax = min(min(max(t0.x, t1.x), max(t0.y, t1.y)), max(t0.z, t1.z));

	return vec2(tmin, tmax);
}

void main()
{
	vec3 half_size = volume_size * 0.5;
	vec2 t_hit = intersect_box(half_size);
	if (t_hit.x > t_hit.y)
	{
		discard;
	}

	t_hit.x = max(t_hit.x, 0.0);

	vec3 dt_vec = volume_size * abs(ray_dir);
	float dt = 3 / max(dt_vec.x, max(dt_vec.y, dt_vec.z));

	vec3 inv_size = 1.0 / volume_size;
	vec3 coeff = ray_dir * inv_size * dt;
	vec3 p = (ray_pos + half_size + t_hit.x * ray_dir) * inv_size;

	float val = 0;
	for (float t = t_hit.x; t < t_hit.y; t += dt)
	{
		float new_val = texture(volume, p).r;
		val = max(val, new_val);
		p += coeff;
	}

	outColor = vec4(vec3(val), 1.0);
}
