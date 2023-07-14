

using System.Diagnostics;

Color FrontToBackBlend(IEnumerable<Fragment> fragments)
{
    var dst = new Color(0.0, 0.0, 0.0, 1.0);

    foreach (var f in fragments.OrderBy(f => f.Depth))
    {
        dst = new Color(
            dst.R + f.Color.A * dst.A * f.Color.R,
            dst.G + f.Color.A * dst.A * f.Color.G,
            dst.B + f.Color.A * dst.A * f.Color.B,
            dst.A * (1.0 - f.Color.A)
        );
    }

    return dst;
}

Color Blend(Color cSrc, Color cDst, BlendFactor srcFactor, BlendFactor dstFactor, BlendEquation blendEquation)
{
    var fSrc = Factor(srcFactor, cSrc, cDst);

    var fDst = Factor(dstFactor, cSrc, cDst);

    var newColor = blendEquation switch
    {
        BlendEquation.GL_FUNC_ADD => cSrc * fSrc + cDst * fDst,
        BlendEquation.GL_FUNC_SUBSTRACT => cSrc * fSrc - cDst * fDst,
        BlendEquation.GL_FUNC_REVERSE_SUBSTRACT => cDst * fDst - cSrc * fSrc,
    };

    return newColor;

    static Color Factor(BlendFactor factor, Color src, Color dst)
    {
        return factor switch
        {
            BlendFactor.GL_ZERO => 0.0,
            BlendFactor.GL_ONE => 1.0,

            BlendFactor.GL_SRC_COLOR => src,
            BlendFactor.GL_ONE_MINUS_SRC_COLOR => 1.0 - src,
            BlendFactor.GL_DST_COLOR => dst,
            BlendFactor.GL_ONE_MINUS_DST_COLOR => 1.0 - dst,

            BlendFactor.GL_SRC_ALPHA => src.A,
            BlendFactor.GL_ONE_MINUS_SRC_ALPHA => 1.0 - src.A,
            BlendFactor.GS_DST_ALPHA => dst.A,
            BlendFactor.GL_ONE_MINUS_DST_ALPHA => 1.0 - dst.A
        };
    }
}


void Render(FrameBuffer frameBuffer, IEnumerable<Fragment> fragments,
    BlendFactor srcFactor, BlendFactor dstFactor,
    BlendEquation blendEquation,
    DepthTest depthTest1, DepthTest depthTest2,
    bool enableDepth1Write, bool enableDepth2Write)
{
    foreach (var fragment in fragments)
    {
        var depthTest1Result = doDepthTest(frameBuffer.DepthBuffer1.Read(), fragment.Depth, depthTest1);
        var depthTest2Result = doDepthTest(frameBuffer.DepthBuffer2.Read(), fragment.Depth, depthTest2);

        if (depthTest1Result && depthTest2Result)
        {
            if (enableDepth1Write)
            {
                frameBuffer.DepthBuffer1.Set(fragment.Depth);
            }

            if (enableDepth2Write)
            {
                frameBuffer.DepthBuffer2.Set(fragment.Depth);
            }
        
            var cSrc = fragment.Color;
            var cDst = frameBuffer.ColorBuffer.Read();

            frameBuffer.ColorBuffer.Set(Blend(cSrc, cDst, srcFactor, dstFactor, blendEquation));
        }
    }

    bool doDepthTest(double depth, double fragmentDepth, DepthTest depthTest)
    {
        return depthTest switch
        {
            DepthTest.GL_NEVER => false,
            DepthTest.GL_LESS => fragmentDepth < depth,
            DepthTest.GL_GREATER => fragmentDepth > depth,
            DepthTest.GL_EQUAL => fragmentDepth == depth,
            DepthTest.GL_ALWAYS => true,
            DepthTest.GL_LEQUAL => fragmentDepth <= depth,
            DepthTest.GL_GEQUAL => fragmentDepth >= depth,
            DepthTest.GL_NOTEQUAL => fragmentDepth != depth
        };
    }
}

Console.OutputEncoding = System.Text.Encoding.Unicode;

var rand = new Random(42);
var fragments = Enumerable.Range(0, 5).Select(i => Fragment.Random(rand)).ToList();

var colorBuffer = new Buffer<Color>(Color.Black);
var depthBuffers = new [] { new Buffer<double>(double.PositiveInfinity), new Buffer<double>(double.PositiveInfinity) };


var accumulationBuffer = new Buffer<Color>(new Color(0.0, 0.0, 0.0, 1.0));

for (var i = 0; i < 10; ++i)
{
    var frameBuffer = new FrameBuffer(colorBuffer, depthBuffers[i % 2], depthBuffers[(i + 1) % 2]);

    frameBuffer.ColorBuffer.Set(new Color(0.0, 0.0, 0.0, 0.0));
    frameBuffer.DepthBuffer2.Set(double.PositiveInfinity);

    if (i == 0)
    {
        Render(frameBuffer, fragments, BlendFactor.GL_ONE, BlendFactor.GL_ZERO, BlendEquation.GL_FUNC_ADD, DepthTest.GL_ALWAYS, DepthTest.GL_LESS, false, true);
    }
    else
    {
        Render(frameBuffer, fragments, BlendFactor.GL_ONE, BlendFactor.GL_ZERO, BlendEquation.GL_FUNC_ADD, DepthTest.GL_GREATER, DepthTest.GL_LESS, false, true);
    }

    Console.WriteLine($"{frameBuffer.DepthBuffer2.Read()}");

    var src = frameBuffer.ColorBuffer.Read();
    var dst = accumulationBuffer.Read();
    var newPixel = new Color(
        dst.R + dst.A * src.A * src.R,
        dst.G + dst.A * src.A * src.G,
        dst.B + dst.A * src.A * src.B,
        (1.0 - src.A)*dst.A);

    accumulationBuffer.Set(newPixel);
}

var referenceValue = FrontToBackBlend(fragments);

Debug.Assert(referenceValue == accumulationBuffer.Read());

public record FrameBuffer(Buffer<Color> ColorBuffer, Buffer<double> DepthBuffer1, Buffer<double> DepthBuffer2);

public enum DepthTest
{
    GL_NEVER,
    GL_LESS,
    GL_GREATER,
    GL_EQUAL,
    GL_ALWAYS,
    GL_LEQUAL,
    GL_GEQUAL,
    GL_NOTEQUAL
}

public enum BlendEquation
{
    GL_FUNC_ADD,
    GL_FUNC_SUBSTRACT,
    GL_FUNC_REVERSE_SUBSTRACT,
    GL_MIN, GL_MAX
}

public enum BlendFactor
{
    GL_ZERO,
    GL_ONE,

    GL_SRC_COLOR,
    GL_ONE_MINUS_SRC_COLOR,
    GL_DST_COLOR,
    GL_ONE_MINUS_DST_COLOR,

    GL_SRC_ALPHA,
    GL_ONE_MINUS_SRC_ALPHA,
    GS_DST_ALPHA,
    GL_ONE_MINUS_DST_ALPHA,

    GL_CONSTANT_COLOR,
    GL_ONE_MINUS_CONSTANT_COLOR,
    GL_CONSTANT_ALPHA,
    GL_ONE_MINUS_CONSTANT_ALPHA,

    GL_SRC_ALPHA_SATURATE,

    /*
    GL_SRC1_COLOR,
    GL_ONE_MINUS_SRC1_COLOR,
    GL_SRC1_ALPHA,
    GL_ONE_MINUS_SRC1_ALPHA
    */
}
