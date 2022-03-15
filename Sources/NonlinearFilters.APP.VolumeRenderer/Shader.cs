using OpenTK.Graphics.OpenGL;
using System.Text;

namespace NonlinearFilters.APP.VolumeRenderer
{
	public class Shader : IDisposable
	{
		public int ProgramHandle;

		private readonly Dictionary<string, int> uniformLocations = new();

		public Shader(string vertexShaderName, string fragmentShaderName)
		{
			var assembly = typeof(Shader).Assembly;

			var vertexStream = assembly.GetManifestResourceStream($"VertexShaders.{vertexShaderName}");
			if (vertexStream is null)
				throw new ArgumentException($"missing VertexShaders.{vertexShaderName}");

			string vertexShaderSource;
			using (var reader = new StreamReader(vertexStream, Encoding.UTF8))
			{
				vertexShaderSource = reader.ReadToEnd();
			}

			var fragmentStream = assembly.GetManifestResourceStream($"FragmentShaders.{fragmentShaderName}");
			if (fragmentStream is null)
				throw new ArgumentException($"missing FragmentShaders.{fragmentShaderName}");

			string fragmentShaderSource;
			using (var reader = new StreamReader(fragmentStream, Encoding.UTF8))
			{
				fragmentShaderSource = reader.ReadToEnd();
			}

			var vertexShader = GL.CreateShader(ShaderType.VertexShader);
			GL.ShaderSource(vertexShader, vertexShaderSource);

			var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(fragmentShader, fragmentShaderSource);

			GL.CompileShader(vertexShader);

			string infoLogVert = GL.GetShaderInfoLog(vertexShader);
			if (infoLogVert != string.Empty)
				throw new Exception(infoLogVert);

			GL.CompileShader(fragmentShader);

			string infoLogFrag = GL.GetShaderInfoLog(fragmentShader);
			if (infoLogFrag != string.Empty)
				throw new Exception(infoLogFrag);

			ProgramHandle = GL.CreateProgram();
			GL.AttachShader(ProgramHandle, vertexShader);
			GL.AttachShader(ProgramHandle, fragmentShader);
			GL.LinkProgram(ProgramHandle);

			GL.DetachShader(ProgramHandle, vertexShader);
			GL.DetachShader(ProgramHandle, fragmentShader);
			GL.DeleteShader(vertexShader);
			GL.DeleteShader(fragmentShader);

			GL.GetProgram(ProgramHandle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

			for (var i = 0; i < numberOfUniforms; i++)
			{
				var key = GL.GetActiveUniform(ProgramHandle, i, out _, out _);
				var location = GL.GetUniformLocation(ProgramHandle, key);
				uniformLocations.Add(key, location);
			}
		}

		~Shader()
		{
			GL.DeleteProgram(ProgramHandle);
		}

		public void Dispose()
		{
			GL.DeleteProgram(ProgramHandle);
			GC.SuppressFinalize(this);
		}

		public void Use()
		{
			GL.UseProgram(ProgramHandle);
		}

		public int GetAttribLocation(string attribName)
		{
			return GL.GetAttribLocation(ProgramHandle, attribName);
		}

		public int GetUniformLocation(string uniformName)
		{
			return uniformLocations[uniformName];
		}
	}
}
