
using System;
using System.Diagnostics;
using System.IO;
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

        _timer = new Stopwatch();
        _timer.Start();

        _framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

        _colorAttachmentTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _colorAttachmentTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, base.Size[0], base.Size[1], 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindImageTexture(0, _colorAttachmentTexture, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);

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

        _computeProgram = SetupComputeShader("compute.compute");

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

        // first pass
        var timeValue = _timer.Elapsed.TotalSeconds;
        var redValue = (float)Math.Sin(timeValue) / 2.0f + 0.5f;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _shaderProgram.Use();

        var vertexColorLocation = _shaderProgram.GetUniformLocation("ourColor");
        GL.Uniform4(vertexColorLocation, redValue, 0.0f, 0.0f, 1.0f);

        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawElements(BeginMode.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

        // compute pass
        GL.UseProgram(_computeProgram);
        GL.DispatchCompute(base.Size[0], base.Size[1], 1);

        // make sure writing to image has finished before read
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);


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

    private int SetupComputeShader(string computePath)
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
}
