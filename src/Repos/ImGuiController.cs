﻿using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Veldrid;

namespace Repos
{
    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
    public class ImGuiController : IDisposable
    {
        const IntPtr FontAtlasId = 1;
        readonly Vector2 _scaleFactor = Vector2.One;

        // Image trackers
        readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new();
        readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new();
        readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new();
        readonly List<IDisposable> _ownedResources = [];

        int _lastAssignedId = 100;
        bool _frameBegun;
        int _windowWidth;
        int _windowHeight;

        // Veldrid objects
        GraphicsDevice? _gd;
        DeviceBuffer? _vertexBuffer;
        DeviceBuffer? _indexBuffer;
        DeviceBuffer? _projMatrixBuffer;
        Texture? _fontTexture;
        TextureView? _fontTextureView;
        Shader? _vertexShader;
        Shader? _fragmentShader;
        ResourceLayout? _layout;
        ResourceLayout? _textureLayout;
        Pipeline? _pipeline;
        ResourceSet? _mainResourceSet;
        ResourceSet? _fontTextureResourceSet;

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, int width, int height)
        {
            _gd = gd;
            _windowWidth = width;
            _windowHeight = height;

            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
            io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

            CreateDeviceResources(gd, outputDescription);
            SetPerFrameImGuiData(1f / 60f);
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void DestroyDeviceObjects() => Dispose();

        public void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription)
        {
            _gd = gd;
            ResourceFactory factory = gd.ResourceFactory;
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = "ImGui.NET Vertex Buffer";

            _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name = "ImGui.NET Index Buffer";

            _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
            byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
            _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
            _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

            VertexLayoutDescription[] vertexLayouts =
            [
                new(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
            ];

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            var pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, [_vertexShader, _fragmentShader]),
                [_layout, _textureLayout],
                outputDescription,
                ResourceBindingModel.Default);

            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
                _projMatrixBuffer,
                gd.PointSampler));

            RecreateFontDeviceTexture();
        }

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
        {
            if (!_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
            {
                ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
                rsi = new ResourceSetInfo(GetNextImGuiBindingId(), resourceSet);

                _setsByView.Add(textureView, rsi);
                _viewsById.Add(rsi.ImGuiBinding, rsi);
                _ownedResources.Add(resourceSet);
            }

            return rsi.ImGuiBinding;
        }

        IntPtr GetNextImGuiBindingId() => Interlocked.Increment(ref _lastAssignedId);

        /// <summary>
        /// Gets or creates a handle for a texture to be drawn with ImGui.
        /// Pass the returned handle to Image() or ImageButton().
        /// </summary>
        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
        {
            if (!_autoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
            {
                textureView = factory.CreateTextureView(texture);
                _autoViewsByTexture.Add(texture, textureView);
                _ownedResources.Add(textureView);
            }

            return GetOrCreateImGuiBinding(factory, textureView);
        }

        /// <summary>
        /// Retrieves the shader texture binding for the given helper handle.
        /// </summary>
        public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
        {
            if (!_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi))
            {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding);
            }

            return tvi.ResourceSet;
        }

        public void ClearCachedImageResources()
        {
            foreach (IDisposable resource in _ownedResources)
            {
                resource.Dispose();
            }

            _ownedResources.Clear();
            _setsByView.Clear();
            _viewsById.Clear();
            _autoViewsByTexture.Clear();
            _lastAssignedId = 100;
        }

        static byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
        {
            switch (factory.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    {
                        string resourceName = name + ".hlsl.bytes";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                case GraphicsBackend.OpenGL:
                    {
                        string resourceName = name + ".glsl";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                case GraphicsBackend.Vulkan:
                    {
                        string resourceName = name + ".spv";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                case GraphicsBackend.Metal:
                    {
                        string resourceName = name + ".metallib";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        static byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            Assembly assembly = typeof(ImGuiController).Assembly;
            using Stream? s = assembly.GetManifestResourceStream(resourceName);

            if (s == null)
            {
                var allResources = string.Join(", ", assembly.GetManifestResourceNames());
                throw new InvalidOperationException($"Could not find embedded resource {resourceName}, valid resources: {allResources}");
            }

            byte[] ret = new byte[s.Length];
            _ = s.Read(ret, 0, (int)s.Length);
            return ret;
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture()
        {
            if (_gd == null)
                return;

            ImGuiIOPtr io = ImGui.GetIO();
            // Build
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out var bytesPerPixel);

            // Store our identifier
            io.Fonts.SetTexID(FontAtlasId);

            _fontTexture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)width,
                (uint)height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled));

            _fontTexture.Name = "ImGui.NET Font Texture";
            _gd.UpdateTexture(
                _fontTexture,
                pixels,
                (uint)(bytesPerPixel * width * height),
                0,
                0,
                0,
                (uint)width,
                (uint)height,
                1,
                0,
                0);

            _fontTextureView = _gd.ResourceFactory.CreateTextureView(_fontTexture);

            var factory = _gd.ResourceFactory;
            _fontTextureResourceSet?.Dispose();
            _fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTextureView));

            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
        /// or index data has increased beyond the capacity of the existing buffers.
        /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
        /// </summary>
        public void Render(GraphicsDevice gd, CommandList cl)
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData(), gd, cl);
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds, InputSnapshot snapshot)
        {
            if (_frameBegun)
                ImGui.Render();

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(snapshot);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(
                _windowWidth / _scaleFactor.X,
                _windowHeight / _scaleFactor.Y);

            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        bool TryMapKey(Key key, out ImGuiKey result)
        {
            ImGuiKey KeyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiKey startKey2)
            {
                int changeFromStart1 = (int)keyToConvert - (int)startKey1;
                return startKey2 + changeFromStart1;
            }

            result = key switch
            {
                >= Key.F1 and <= Key.F24 => KeyToImGuiKeyShortcut(key, Key.F1, ImGuiKey.F1),
                >= Key.Keypad0 and <= Key.Keypad9 => KeyToImGuiKeyShortcut(key, Key.Keypad0, ImGuiKey.Keypad0),
                >= Key.A and <= Key.Z => KeyToImGuiKeyShortcut(key, Key.A, ImGuiKey.A),
                >= Key.Number0 and <= Key.Number9 => KeyToImGuiKeyShortcut(key, Key.Number0, ImGuiKey._0),
                Key.ShiftLeft or Key.ShiftRight => ImGuiKey.ModShift,
                Key.ControlLeft or Key.ControlRight => ImGuiKey.ModCtrl,
                Key.AltLeft or Key.AltRight => ImGuiKey.ModAlt,
                Key.WinLeft or Key.WinRight => ImGuiKey.ModSuper,
                Key.Menu => ImGuiKey.Menu,
                Key.Up => ImGuiKey.UpArrow,
                Key.Down => ImGuiKey.DownArrow,
                Key.Left => ImGuiKey.LeftArrow,
                Key.Right => ImGuiKey.RightArrow,
                Key.Enter => ImGuiKey.Enter,
                Key.Escape => ImGuiKey.Escape,
                Key.Space => ImGuiKey.Space,
                Key.Tab => ImGuiKey.Tab,
                Key.BackSpace => ImGuiKey.Backspace,
                Key.Insert => ImGuiKey.Insert,
                Key.Delete => ImGuiKey.Delete,
                Key.PageUp => ImGuiKey.PageUp,
                Key.PageDown => ImGuiKey.PageDown,
                Key.Home => ImGuiKey.Home,
                Key.End => ImGuiKey.End,
                Key.CapsLock => ImGuiKey.CapsLock,
                Key.ScrollLock => ImGuiKey.ScrollLock,
                Key.PrintScreen => ImGuiKey.PrintScreen,
                Key.Pause => ImGuiKey.Pause,
                Key.NumLock => ImGuiKey.NumLock,
                Key.KeypadDivide => ImGuiKey.KeypadDivide,
                Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
                Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
                Key.KeypadAdd => ImGuiKey.KeypadAdd,
                Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
                Key.KeypadEnter => ImGuiKey.KeypadEnter,
                Key.Tilde => ImGuiKey.GraveAccent,
                Key.Minus => ImGuiKey.Minus,
                Key.Plus => ImGuiKey.Equal,
                Key.BracketLeft => ImGuiKey.LeftBracket,
                Key.BracketRight => ImGuiKey.RightBracket,
                Key.Semicolon => ImGuiKey.Semicolon,
                Key.Quote => ImGuiKey.Apostrophe,
                Key.Comma => ImGuiKey.Comma,
                Key.Period => ImGuiKey.Period,
                Key.Slash => ImGuiKey.Slash,
                Key.BackSlash or Key.NonUSBackSlash => ImGuiKey.Backslash,
                _ => ImGuiKey.None
            };

            return result != ImGuiKey.None;
        }

        void UpdateImGuiInput(InputSnapshot snapshot)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.AddMousePosEvent(snapshot.MousePosition.X, snapshot.MousePosition.Y);
            io.AddMouseButtonEvent(0, snapshot.IsMouseDown(MouseButton.Left));
            io.AddMouseButtonEvent(1, snapshot.IsMouseDown(MouseButton.Right));
            io.AddMouseButtonEvent(2, snapshot.IsMouseDown(MouseButton.Middle));
            io.AddMouseButtonEvent(3, snapshot.IsMouseDown(MouseButton.Button1));
            io.AddMouseButtonEvent(4, snapshot.IsMouseDown(MouseButton.Button2));
            io.AddMouseWheelEvent(0f, snapshot.WheelDelta);

            for (int i = 0; i < snapshot.KeyCharPresses.Count; i++)
            {
                io.AddInputCharacter(snapshot.KeyCharPresses[i]);
            }

            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent keyEvent = snapshot.KeyEvents[i];
                if (TryMapKey(keyEvent.Key, out ImGuiKey imguikey))
                {
                    io.AddKeyEvent(imguikey, keyEvent.Down);
                }
            }
        }

        void RenderImDrawData(ImDrawDataPtr drawData, GraphicsDevice gd, CommandList cl)
        {
            if (_gd == null || _vertexBuffer == null || _indexBuffer == null || drawData.CmdListsCount == 0)
                return;

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            uint totalVBSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBSize > _vertexBuffer.SizeInBytes)
            {
                gd.DisposeWhenIdle(_vertexBuffer);
                _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBuffer.SizeInBytes)
            {
                gd.DisposeWhenIdle(_indexBuffer);
                _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr commandList = drawData.CmdLists[i];

                cl.UpdateBuffer(
                    _vertexBuffer,
                    vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                    commandList.VtxBuffer.Data,
                    (uint)(commandList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

                cl.UpdateBuffer(
                    _indexBuffer,
                    indexOffsetInElements * sizeof(ushort),
                    commandList.IdxBuffer.Data,
                    (uint)(commandList.IdxBuffer.Size * sizeof(ushort)));

                vertexOffsetInVertices += (uint)commandList.VtxBuffer.Size;
                indexOffsetInElements += (uint)commandList.IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);

            _gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainResourceSet);

            drawData.ScaleClipRects(io.DisplayFramebufferScale);

            // Render command lists
            int vertexOffset = 0;
            int indexOffset = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr commandList = drawData.CmdLists[n];
                for (int i = 0; i < commandList.CmdBuffer.Size; i++)
                {
                    ImDrawCmdPtr pcmd = commandList.CmdBuffer[i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                        throw new NotImplementedException();

                    if (pcmd.TextureId != IntPtr.Zero)
                    {
                        var resourceSet =
                            pcmd.TextureId == FontAtlasId
                                ? _fontTextureResourceSet
                                : GetImageResourceSet(pcmd.TextureId);

                        cl.SetGraphicsResourceSet(1, resourceSet);
                    }

                    cl.SetScissorRect(
                        0,
                        (uint)pcmd.ClipRect.X,
                        (uint)pcmd.ClipRect.Y,
                        (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)indexOffset, (int)pcmd.VtxOffset + vertexOffset, 0);
                }
                vertexOffset += commandList.VtxBuffer.Size;
                indexOffset += commandList.IdxBuffer.Size;
            }
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _projMatrixBuffer?.Dispose();
            _fontTexture?.Dispose();
            _fontTextureView?.Dispose();
            _vertexShader?.Dispose();
            _fragmentShader?.Dispose();
            _layout?.Dispose();
            _textureLayout?.Dispose();
            _pipeline?.Dispose();
            _mainResourceSet?.Dispose();

            foreach (IDisposable resource in _ownedResources)
                resource.Dispose();
        }

        struct ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
        {
            public readonly IntPtr ImGuiBinding = imGuiBinding;
            public readonly ResourceSet ResourceSet = resourceSet;
        }
    }
}
