
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class ShaderProgram: IDisposable
{
    private int _program;
    private bool _isDisposed = false;

    public ShaderProgram(string vertexPath, string fragmentPath)
    {
        var vertexShaderSource = File.ReadAllText(vertexPath);
        var fragmentShaderSource = File.ReadAllText(fragmentPath);

        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);

        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out var vertexShaderCompileStatus);
        if (vertexShaderCompileStatus == 0)
        {
            var infoLog = GL.GetShaderInfoLog(vertexShader);
            Debug.WriteLine(infoLog);
        }

        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out var fragmentShaderCompileStatus);
        if (fragmentShaderCompileStatus == 0)
        {
            var infoLog = GL.GetShaderInfoLog(fragmentShader);
            Debug.WriteLine(infoLog);
        }

        _program = GL.CreateProgram();

        GL.AttachShader(_program, vertexShader);
        GL.AttachShader(_program, fragmentShader);

        GL.LinkProgram(_program);

        GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out var programLinkStatus);
        if (programLinkStatus == 0)
        {
            var infoLog = GL.GetProgramInfoLog(_program);
            Debug.WriteLine(infoLog);
        }

        GL.DetachShader(_program, vertexShader);
        GL.DetachShader(_program, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use() => GL.UseProgram(_program);

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            GL.DeleteProgram(_program);
            _program = 0;
            _isDisposed = true;
        }
    }

    ~ShaderProgram()
    {
        if (_isDisposed == false)
        {
            Debug.WriteLine("Shader not disposed!");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal int GetUniformLocation(string uniformName) => GL.GetUniformLocation(_program, uniformName);
}

public class Game : GameWindow
{
    private static readonly float[] _vertices = { -0.5f, -0.5f, 0.0f, 0.5f, -0.5f, 0.0f, 0.0f, 0.5f, 0.0f };
    private static readonly uint[] _indices = {0, 1, 2};
    private ShaderProgram _shaderProgram;
    private ShaderProgram _drawQuad;
    private int _computeProgram;

    private DistanceTransform? _distanceTransform;

    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private int _elementBufferObject;

    private int _emptyVertexArrayObject;

    private int _framebuffer;
    private int _colorAttachmentTexture;

    private Stopwatch _timer;

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
    {
        GL.GetInteger(GetPName.MaxVertexAttribs, out var maxVertexAttributes);

        Debug.WriteLine($"MaxVertexAttributes: {maxVertexAttributes}");
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Debug.WriteLine(GL.GetString(StringName.Vendor));
        Debug.WriteLine(GL.GetString(StringName.Renderer));

        _timer = new Stopwatch();

        _framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

        _colorAttachmentTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _colorAttachmentTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, base.Size[0], base.Size[1], 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorAttachmentTexture, 0);

        var result = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (result != FramebufferErrorCode.FramebufferComplete)
        {
            Debug.WriteLine($"Framebuffer is not complete ({result})");
        }
        else
        {
            Debug.WriteLine($"Framebuffer is complete");
        }
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
        //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        _emptyVertexArrayObject = GL.GenVertexArray();

        _shaderProgram = new ShaderProgram("shader.vert", "shader.frag");
        _drawQuad = new ShaderProgram("drawQuad.vert", "drawQuad.frag");

        _computeProgram = SetupComputeShader("compute.comp");

        _distanceTransform = new DistanceTransform(this);

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        _shaderProgram.Dispose();
        _drawQuad.Dispose();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _timer.Reset();
        _timer.Start();

        // first pass
        var redValue = 1.0f;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _shaderProgram.Use();

        var vertexColorLocation = _shaderProgram.GetUniformLocation("ourColor");
        GL.Uniform4(vertexColorLocation, redValue, 0.0f, 0.0f, 1.0f);

        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawElements(BeginMode.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

        if (true)
        { 
            // compute pass
            GL.UseProgram(_computeProgram);

            GL.BindImageTexture(0, _colorAttachmentTexture, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.DispatchCompute(base.Size[0], base.Size[1], 1);

            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }

        if (true)
        { 
            _distanceTransform.RunFirstPass(this, _colorAttachmentTexture);
            _distanceTransform.RunSecondPass(this, _colorAttachmentTexture);
        }

        // second pass
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        _drawQuad.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _colorAttachmentTexture);

        GL.BindVertexArray(_emptyVertexArrayObject);
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

        Context.SwapBuffers();

        Debug.WriteLine(_timer.ElapsedMilliseconds);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        var input = KeyboardState;

        if (input.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    private static int SetupComputeShader(string computePath)
    {
        var computeProgram = GL.CreateProgram();
        var computeShader = GL.CreateShader(ShaderType.ComputeShader);
        var computeShaderSource = File.ReadAllText(computePath);

        GL.ShaderSource(computeShader, computeShaderSource);

        GL.CompileShader(computeShader);
        GL.GetShader(computeShader, ShaderParameter.CompileStatus, out var computeShaderCompileStatus);
        if (computeShaderCompileStatus != (int)All.True)
        {
            var infoLog = GL.GetShaderInfoLog(computeShader);
            Debug.WriteLine(infoLog);
        }

        GL.AttachShader(computeProgram, computeShader);
        GL.LinkProgram(computeProgram);
        GL.GetProgram(computeProgram, GetProgramParameterName.LinkStatus, out var computeProgramLinkStatus);
        if (computeProgramLinkStatus != (int)All.True)
        {
            var infoLog = GL.GetProgramInfoLog(computeProgram);
            Debug.WriteLine(infoLog);
        }

        GL.DetachShader(computeProgram, computeShader);
        GL.DeleteShader(computeShader);

        return computeProgram;
    }

    public class DistanceTransform
    {
        private readonly int _firstPassComputeShader;
        private readonly int _secondPassComputeShader;

        private readonly int _indicatriceWorkArray;
        private readonly int _tmpWorkArray;
        private readonly int _vWorkArray;
        private readonly int _zWorkArray;

        public DistanceTransform(GameWindow window)
        {
            _firstPassComputeShader = SetupComputeShader("distanceTransformFirstPass.comp");
            _secondPassComputeShader = SetupComputeShader("distanceTransformSecondPass.comp");

            _indicatriceWorkArray = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _indicatriceWorkArray);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, window.Size[0], window.Size[1], 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            _tmpWorkArray = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _tmpWorkArray);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, window.Size[0], window.Size[1], 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            _vWorkArray = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _vWorkArray);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, window.Size[0], window.Size[1], 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            _zWorkArray = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _zWorkArray);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, window.Size[0] + 1, window.Size[1] + 1, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        ///   Run horizontal pass on the indicatrice of the input image.
        /// </summary>
        /// <param name="inputImage">Should be a texture in R32ui format</param>
        public void RunFirstPass(GameWindow window, int inputImage)
        {
            GL.UseProgram(_firstPassComputeShader);

            GL.BindImageTexture(0, inputImage, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(1, _indicatriceWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(2, _vWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(3, _zWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);

            GL.DispatchCompute(1, window.Size[1], 1);
            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }

        /// <summary>
        ///   Run vertical pass on the distance samples of the input image.
        /// </summary>
        /// <param name="inputImage">Should be a texture in R32i format</param>
        public void RunSecondPass(GameWindow window, int inputImage)
        {
            GL.UseProgram(_secondPassComputeShader);

            GL.BindImageTexture(0, inputImage, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(1, _indicatriceWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(2, _vWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(3, _zWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);
            GL.BindImageTexture(4, _tmpWorkArray, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32f);

            GL.DispatchCompute(window.Size[0], 1, 1);

            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }
    }
}
