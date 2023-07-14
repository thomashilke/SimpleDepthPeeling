using System.Diagnostics;

/*
 * Baseline for comparison with the depth peeling algorithm.
 */
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

/*
 * Simulate the blending model of OpenGL (glBlendFunc, glBlendFuncSeparate, glBlendEquation).
 */
Color Blend(Color cSrc, Color cDst, BlendFactor srcColorFactor, BlendFactor srcAlphaFactor, BlendFactor dstColorFactor, BlendFactor dstAlphaFactor, BlendEquation blendEquation)
{
    var fSrc = Factor(srcColorFactor, srcAlphaFactor, cSrc, cDst);
    var fDst = Factor(dstColorFactor, dstAlphaFactor, cSrc, cDst);

    var newColor = blendEquation switch
    {
        BlendEquation.GL_FUNC_ADD => cSrc * fSrc + cDst * fDst,
        BlendEquation.GL_FUNC_SUBSTRACT => cSrc * fSrc - cDst * fDst,
        BlendEquation.GL_FUNC_REVERSE_SUBSTRACT => cDst * fDst - cSrc * fSrc,
    };

    return newColor;

    static Color Factor(BlendFactor colorFactor, BlendFactor alphaFactor, Color src, Color dst)
    {
        var rgb = colorFactor switch
        {
            BlendFactor.GL_ZERO => 0.0,
            BlendFactor.GL_ONE => 1.0,

            BlendFactor.GL_SRC_COLOR => src.Rgb,
            BlendFactor.GL_ONE_MINUS_SRC_COLOR => 1.0 - src.Rgb,
            BlendFactor.GL_DST_COLOR => dst.Rgb,
            BlendFactor.GL_ONE_MINUS_DST_COLOR => 1.0 - dst.Rgb,

            BlendFactor.GL_SRC_ALPHA => src.A,
            BlendFactor.GL_ONE_MINUS_SRC_ALPHA => 1.0 - src.A,
            BlendFactor.GL_DST_ALPHA => dst.A,
            BlendFactor.GL_ONE_MINUS_DST_ALPHA => 1.0 - dst.A
        };

        var alpha = alphaFactor switch
        {
            BlendFactor.GL_ZERO => 0.0,
            BlendFactor.GL_ONE => 1.0,

            BlendFactor.GL_SRC_COLOR => src.A,
            BlendFactor.GL_ONE_MINUS_SRC_COLOR => 1.0 - src.A,
            BlendFactor.GL_DST_COLOR => dst.A,
            BlendFactor.GL_ONE_MINUS_DST_COLOR => 1.0 - dst.A,

            BlendFactor.GL_SRC_ALPHA => src.A,
            BlendFactor.GL_ONE_MINUS_SRC_ALPHA => 1.0 - src.A,
            BlendFactor.GL_DST_ALPHA => dst.A,
            BlendFactor.GL_ONE_MINUS_DST_ALPHA => 1.0 - dst.A
        };

        return new Color(rgb, alpha);
    }
}

void Render(FrameBuffer frameBuffer, IEnumerable<Fragment> fragments,
    BlendFactor srcColorFactor, BlendFactor srcAlphaFactor, BlendFactor dstColorFactor, BlendFactor dstAlphaFactor,
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

            frameBuffer.ColorBuffer.Set(Blend(cSrc, cDst, srcColorFactor, srcAlphaFactor, dstColorFactor, dstAlphaFactor, blendEquation));
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

/*
 * Create the fragments
 */
var rand = new Random(42);
var fragments = Enumerable.Range(0, 5).Select(i => Fragment.Random(rand)).ToList();

var colorBuffer = new Buffer<Color>(Color.OpaqueBlack);
var depthBuffers = new [] { new Buffer<double>(double.PositiveInfinity), new Buffer<double>(double.PositiveInfinity) };

var accumulationBuffer = new Buffer<Color>(new Color(0.0, 0.0, 0.0, 1.0));

/*
 * Simple depth peeling passes front to back, with front to back blending into accumulation buffer.
 */
for (var i = 0; i < 10; ++i)
{
    /*
     * depthBuffers[i % 2] is the depth that is to be peeled in this pass, and
     * depthBuffers[(i + 1) % 2] is used as a regular depth buffer and must be cleared. 
     */
    var frameBuffer = new FrameBuffer(colorBuffer, depthBuffers[i % 2], depthBuffers[(i + 1) % 2]);

    frameBuffer.ColorBuffer.Set(new Color(0.0, 0.0, 0.0, 0.0));
    frameBuffer.DepthBuffer2.Set(double.PositiveInfinity);

    var depthTest1 = i == 0 ? DepthTest.GL_ALWAYS : DepthTest.GL_GREATER;
    var depthTest2 = DepthTest.GL_LESS;

    Render(frameBuffer, fragments,
        BlendFactor.GL_SRC_ALPHA, BlendFactor.GL_ONE, BlendFactor.GL_ZERO, BlendFactor.GL_ZERO,
        BlendEquation.GL_FUNC_ADD,
        depthTest1, depthTest2,
        false, true);

    /*
     * Accumulate the layer color into the accumulation buffer front to back.
     * Src R,G,B have already be premultiplied by A in the peeling pass (BlendFactor.GL_SRC_ALPHA).
     */
    var src = frameBuffer.ColorBuffer.Read();
    var dst = accumulationBuffer.Read();

    accumulationBuffer.Set(
        Blend(
            src, dst,
            BlendFactor.GL_DST_ALPHA, BlendFactor.GL_ZERO,
            BlendFactor.GL_ONE, BlendFactor.GL_ONE_MINUS_SRC_ALPHA,
            BlendEquation.GL_FUNC_ADD));
}

var referenceColor = FrontToBackBlend(fragments);

Debug.Assert(referenceColor == accumulationBuffer.Read());
