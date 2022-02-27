using NonlinearFilters.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace NonlinearFilters.APP
{
	public class VolumeWindow : GameWindow
	{
		private int VertexBufferObject;
		private int ElementBufferObject;
		private int VertexArrayObject;
		private int ColorMap;
		private int VolumeObject;

		private Shader shader = null!;

		private VolumetricImage volume = null!;

		private float zoom = 300, rotX = -1.55f, rotY = 1.87f, rotZ = 0;

		public VolumeWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
		}

		public void SetVolume(VolumetricImage volume)
		{
			this.volume = volume;
		}

		protected override void OnLoad()
		{
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

			/*
			using (var bmp = new System.Drawing.Bitmap("path to color map"))
			{
				var data = bmp.LockBits(new(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				ColorMap = GL.GenTexture();
				GL.ActiveTexture(TextureUnit.Texture1);
				GL.BindTexture(TextureTarget.Texture1D, ColorMap);
				GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
				GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
				GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba, bmp.Width, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				GL.ActiveTexture(TextureUnit.Texture1);
				GL.BindTexture(TextureTarget.Texture1D, ColorMap);

				int transLoc = shader.GetUniformLocation("transfer_fcn");
				GL.Uniform1(transLoc, 1);
			}
			*/

			base.OnLoad();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			shader.Use();

			int viewLoc = shader.GetUniformLocation("view");
			GL.Uniform4(viewLoc, rotX, rotY, rotZ, zoom);

			int resLoc = shader.GetUniformLocation("resolution");
			GL.Uniform2(resLoc, (float)Size.X, (float)Size.Y);

			GL.BindVertexArray(VertexArrayObject);

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture3D, VolumeObject);
			
			/*
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture1D, ColorMap);
			*/

			GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

			Context.SwapBuffers();
			base.OnRenderFrame(e);
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			GL.Viewport(0, 0, Size.X, Size.Y);

			base.OnResize(e);
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			var input = KeyboardState;

			if (input.IsKeyDown(Keys.Escape))
			{
				Close();
			}

			if (MouseState.IsButtonDown(MouseButton.Left))
			{
				rotX = (rotX + MouseState.Delta.Y / 100) % (2 * MathF.PI);
				rotY = (rotY + MouseState.Delta.X / 100) % (2 * MathF.PI);

				Title = $"X: {rotX} | Y: {rotY}";
			}

			zoom -= MouseState.ScrollDelta.Y;

			base.OnUpdateFrame(e);
		}

		protected override void OnUnload()
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.DeleteBuffer(VertexBufferObject);
			GL.BindVertexArray(0);
			GL.UseProgram(0);
			GL.DeleteVertexArray(VertexArrayObject);

			shader.Dispose();

			base.OnUnload();
		}
	}
}
