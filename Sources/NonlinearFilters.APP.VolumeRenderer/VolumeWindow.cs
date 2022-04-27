using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.Runtime.CompilerServices;
using NonlinearFilters.Volume;

namespace NonlinearFilters.APP.VolumeRenderer
{
	public class VolumeWindow : GameWindow
	{
		private int VertexBufferObject;
		private int ElementBufferObject;
		private int VertexArrayObject;
		private int VolumeObject;

		private Shader shader = null!;

		private VolumetricData volume = null!;

		private float zoom = 1, rotX = -1.55f, rotY = 1.87f, rotZ = 0;
		private int longestSide;

		public VolumeWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
		}

		public void InitVolume(VolumetricData volume)
		{
			this.volume = volume;
			longestSide = Math.Max(volume.Size.X, Math.Max(volume.Size.Y, volume.Size.Z));
			zoom = longestSide;
		}

		public void SetVolume(VolumetricData volume)
		{
			InitVolume(volume);
			GL.DeleteTexture(VolumeObject);
			LoadVolume();
		}

		private void LoadVolume()
		{
			float w = volume.Size.Z, h = volume.Size.Y, d = volume.Size.X;
			int sizeLoc = shader.GetUniformLocation("volume_size");
			GL.Uniform3(sizeLoc, w, h, d);

			unsafe
			{
				fixed (byte* dataPtr = volume.Data)
				{
					var ptr = (IntPtr)dataPtr;

					VolumeObject = GL.GenTexture();
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.BindTexture(TextureTarget.Texture3D, VolumeObject);
					GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
					GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
					GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
					GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

					GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

					GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba, volume.Size.Z, volume.Size.Y, volume.Size.X, 0, PixelFormat.Red, PixelType.UnsignedByte, ptr);
					GL.GenerateMipmap(GenerateMipmapTarget.Texture3D);

					GL.ActiveTexture(TextureUnit.Texture0);
					GL.BindTexture(TextureTarget.Texture3D, VolumeObject);

					int volLoc = shader.GetUniformLocation("volume");
					GL.Uniform1(volLoc, 0);
				}
			}
		}

		public unsafe Image<Rgba32> Capture()
		{
			var screenWidth = Size.X;
			var screenHeight = Size.Y;
			
			var buffer = new byte[screenWidth * screenHeight * Unsafe.SizeOf<Rgba32>()];
			GL.ReadPixels(0, 0, screenWidth, screenHeight, PixelFormat.Bgra, PixelType.UnsignedByte, buffer);

			var img = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(buffer, screenWidth, screenHeight);
			img.Mutate(x => x.Flip(FlipMode.Vertical));

			return img;
		}

		protected override void OnLoad()
		{
			base.OnLoad();
			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

			float[] vertices = {
				 1f,  1f, 0.0f,  // top right
				 1f, -1f, 0.0f,  // bottom right
				-1f, -1f, 0.0f,  // bottom left
				-1f,  1f, 0.0f   // top left
			};

			uint[] indices = {
				0, 1, 3,
				1, 2, 3
			};

			VertexBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
			GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

			VertexArrayObject = GL.GenVertexArray();
			GL.BindVertexArray(VertexArrayObject);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
			GL.EnableVertexAttribArray(0);

			ElementBufferObject = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			shader = new Shader("Shader", "Shader");
			shader.Use();

			LoadVolume();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			Context.MakeCurrent();

			GL.Clear(ClearBufferMask.ColorBufferBit);

			shader.Use();

			int viewLoc = shader.GetUniformLocation("view");
			GL.Uniform4(viewLoc, rotX, rotY, rotZ, zoom);

			int resLoc = shader.GetUniformLocation("resolution");
			GL.Uniform2(resLoc, (float)Size.X, (float)Size.Y);

			GL.BindVertexArray(VertexArrayObject);

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture3D, VolumeObject);

			GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

			Context.SwapBuffers();
		}

		protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			Context.MakeCurrent();

			if (MouseState.IsButtonDown(MouseButton.Left))
			{
				rotX = (rotX + MouseState.Delta.Y / 100) % (2 * MathF.PI);
				rotY = (rotY + MouseState.Delta.X / 100) % (2 * MathF.PI);

				Title = $"X: {rotX} | Y: {rotY}";
			}

			zoom -= (int)(MouseState.ScrollDelta.Y * (longestSide * 0.1));

			base.OnUpdateFrame(e);
		}

		protected override void OnUnload()
		{
			Context.MakeCurrent();

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.DeleteBuffer(VertexBufferObject);
			GL.BindVertexArray(0);
			GL.UseProgram(0);
			GL.DeleteVertexArray(VertexArrayObject);
			GL.DeleteTexture(VolumeObject);

			shader.Dispose();
			base.OnUnload();
		}
	}
}
