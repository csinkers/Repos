using System.Numerics;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Repos.Ui;

public sealed class UiRenderer : IDisposable
{
    readonly ImFontPtr _font;
    readonly CommandList _cl;
    readonly Sdl2Window _window;
    readonly ImGuiController _controller;
    readonly GraphicsDevice _gd;
    DateTime _lastFrame = DateTime.Now;

    public static void Run(Action<Input> drawAction)
    {
        using var ui = new UiRenderer();
        ui.RunInner(drawAction);
    }

    UiRenderer()
    {
        var windowInfo = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 960,
            WindowInitialState = WindowState.Normal,
            WindowTitle = "Repo Manager"
        };

        var gdOptions = new GraphicsDeviceOptions(
            true,
            null,
            true,
            ResourceBindingModel.Improved,
            true,
            true,
            false);

        VeldridStartup.CreateWindowAndGraphicsDevice(
            windowInfo,
            gdOptions,
            out _window,
            out _gd);

        _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

        _cl = _gd.ResourceFactory.CreateCommandList();
        _window.Resized += () =>
        {
            _gd.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
            _controller.WindowResized(_window.Width, _window.Height);
        };

        var io = ImGui.GetIO();
        unsafe
        {
            var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            nativeConfig->OversampleH = 8;
            nativeConfig->OversampleV = 8;
            nativeConfig->RasterizerMultiply = 1f;
            nativeConfig->GlyphOffset = new Vector2(0);

            var dir = Directory.GetCurrentDirectory();
            var fontPath = Path.Combine(dir, "SpaceMono-Regular.ttf");

            if (!File.Exists(fontPath))
                throw new FileNotFoundException("Could not find font file at " + fontPath);

            _font = io.Fonts.AddFontFromFileTTF(
                fontPath,
                12, // size in pixels
                nativeConfig);

            if (_font.NativePtr == (ImFont *)0 )
                throw new InvalidOperationException("Font could not be loaded");
            _controller.RecreateFontDeviceTexture();
        }

        io.FontGlobalScale = 2.0f;
    }

    void RunInner(Action<Input> drawCallback)
    {
        var inputWrapper = new Input();
        while (_window.Exists)
        {
            var input = _window.PumpEvents();
            inputWrapper.Snapshot = input;

            if (!_window.Exists)
                break;

            var thisFrame = DateTime.Now;
            _controller.Update((float)(thisFrame - _lastFrame).TotalSeconds, input);
            _lastFrame = thisFrame;

            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(_window.Width, _window.Height));
            ImGui.PushFont(_font);
            ImGui.Begin("Repos", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            drawCallback(inputWrapper);

            ImGui.End();
            ImGui.PopFont();

            _cl.Begin();
            _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _controller.Render(_gd, _cl);
            _cl.End();

            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_gd.MainSwapchain);
        }
    }

    public void Dispose()
    {
        _controller.DestroyDeviceObjects();
        _gd.Dispose();
    }
}