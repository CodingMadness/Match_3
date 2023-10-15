﻿/*******************************************************************************************
 *
 *   raylib-extras [ImGui] example - Simple Integration
 *
 *	This is a simple ImGui Integration
 *	It is done using C++ but with C style code
 *	It can be done in C as well if you use the C ImGui wrapper
 *	https://github.com/cimgui/cimgui
 *
 *   Copyright (c) 2021 Jeffery Myers
 *
 ********************************************************************************************/

using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Match_3.Service;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Match_3.Setup
{
    public static class RlImGui
    {
        private static nint ImGuiContext;
        private static ImGuiMouseCursor CurrentMouseCursor = ImGuiMouseCursor.COUNT;
        private static Dictionary<ImGuiMouseCursor, MouseCursor>? MouseCursorMap;
        private static Texture2D FontTexture;
        
        public static void Setup(bool darkTheme = true)
        {
            MouseCursorMap = new Dictionary<ImGuiMouseCursor, MouseCursor>((int)ImGuiMouseCursor.COUNT);

            FontTexture.id = 0;

            BeginInitImGui();

            if (darkTheme)
                ImGui.StyleColorsDark();
            else
                ImGui.StyleColorsLight();

            EndInitImGui();
        }

        private static void BeginInitImGui()
        {
            ImGuiContext = ImGui.CreateContext();
        }

        private static void SetupMouseCursors()
        {
            MouseCursorMap!.Clear();
            MouseCursorMap[ImGuiMouseCursor.Arrow]      = MouseCursor.MOUSE_CURSOR_ARROW;
            MouseCursorMap[ImGuiMouseCursor.TextInput]  = MouseCursor.MOUSE_CURSOR_IBEAM;
            MouseCursorMap[ImGuiMouseCursor.Hand]       = MouseCursor.MOUSE_CURSOR_POINTING_HAND;
            MouseCursorMap[ImGuiMouseCursor.ResizeAll]  = MouseCursor.MOUSE_CURSOR_RESIZE_ALL;
            MouseCursorMap[ImGuiMouseCursor.ResizeEW]   = MouseCursor.MOUSE_CURSOR_RESIZE_EW;
            MouseCursorMap[ImGuiMouseCursor.ResizeNESW] = MouseCursor.MOUSE_CURSOR_RESIZE_NESW;
            MouseCursorMap[ImGuiMouseCursor.ResizeNS]   = MouseCursor.MOUSE_CURSOR_RESIZE_NS;
            MouseCursorMap[ImGuiMouseCursor.ResizeNWSE] = MouseCursor.MOUSE_CURSOR_RESIZE_NWSE;
            MouseCursorMap[ImGuiMouseCursor.NotAllowed] = MouseCursor.MOUSE_CURSOR_NOT_ALLOWED;
        }

        private static unsafe void ReloadFonts()
        {
            ImGui.SetCurrentContext(ImGuiContext);
            ImGuiIOPtr io = ImGui.GetIO();

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out _);
           
            var image = new Image
            {
                data = pixels,
                width = width,
                height = height,
                mipmaps = 1,
                format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
            };
            
            FontTexture = LoadTextureFromImage(image);
    
            io.Fonts.SetTexID((nint)FontTexture.id);
        }

        private static void EndInitImGui()
        {
            SetupMouseCursors();
            ImGui.SetCurrentContext(ImGuiContext);
            
            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)KeyboardKey.KEY_TAB;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)KeyboardKey.KEY_LEFT;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)KeyboardKey.KEY_RIGHT;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)KeyboardKey.KEY_UP;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)KeyboardKey.KEY_DOWN;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)KeyboardKey.KEY_PAGE_UP;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)KeyboardKey.KEY_PAGE_DOWN;
            io.KeyMap[(int)ImGuiKey.Home] = (int)KeyboardKey.KEY_HOME;
            io.KeyMap[(int)ImGuiKey.End] = (int)KeyboardKey.KEY_END;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)KeyboardKey.KEY_DELETE;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)KeyboardKey.KEY_BACKSPACE;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)KeyboardKey.KEY_ENTER;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)KeyboardKey.KEY_ESCAPE;
            io.KeyMap[(int)ImGuiKey.Space] = (int)KeyboardKey.KEY_SPACE;
            io.KeyMap[(int)ImGuiKey.A] = (int)KeyboardKey.KEY_A;
            io.KeyMap[(int)ImGuiKey.C] = (int)KeyboardKey.KEY_C;
            io.KeyMap[(int)ImGuiKey.V] = (int)KeyboardKey.KEY_V;
            io.KeyMap[(int)ImGuiKey.X] = (int)KeyboardKey.KEY_X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)KeyboardKey.KEY_Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)KeyboardKey.KEY_Z;

            ReloadFonts();
        }

        private static void NewFrame()
        {
            var io = ImGui.GetIO();

            if (IsWindowFullscreen())
            {
                int monitor = GetCurrentMonitor();
                io.DisplaySize = new Vector2(GetMonitorWidth(monitor), GetMonitorHeight(monitor));
            }
            else
            {
                io.DisplaySize = new Vector2(GetScreenWidth(), GetScreenHeight());
            }

            io.DisplayFramebufferScale = Vector2.One;
            io.DeltaTime = GetFrameTime();

            io.KeyCtrl = IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL) || IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL);
            io.KeyShift = IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT) || IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT);
            io.KeyAlt = IsKeyDown(KeyboardKey.KEY_RIGHT_ALT) || IsKeyDown(KeyboardKey.KEY_LEFT_ALT);
            io.KeySuper = IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER) || IsKeyDown(KeyboardKey.KEY_LEFT_SUPER);

            if (io.WantSetMousePos)
            {
                SetMousePosition((int)io.MousePos.X, (int)io.MousePos.Y);
            }
            else
            {
                io.MousePos = GetMousePosition();
            }

            io.MouseDown[0] = IsMouseButtonDown(MouseButton.MOUSE_LEFT_BUTTON);
            io.MouseDown[1] = IsMouseButtonDown(MouseButton.MOUSE_RIGHT_BUTTON);
            io.MouseDown[2] = IsMouseButtonDown(MouseButton.MOUSE_MIDDLE_BUTTON);
            io.MouseWheel     = GetMouseWheelMove() > 0 ? io.MouseWheel + 1 : io.MouseWheel - 1;
            
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
            {
                ImGuiMouseCursor imGuiCursor = ImGui.GetMouseCursor();
                
                if (imGuiCursor != CurrentMouseCursor || io.MouseDrawCursor)
                {
                    CurrentMouseCursor = imGuiCursor;
                    
                    if (io.MouseDrawCursor || imGuiCursor == ImGuiMouseCursor.None)
                    {
                        HideCursor();
                    }
                    else
                    {
                        ShowCursor();

                        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
                        {
                            SetMouseCursor(!MouseCursorMap.ContainsKey(imGuiCursor)
                                ? MouseCursor.MOUSE_CURSOR_DEFAULT
                                : MouseCursorMap[imGuiCursor]);
                        }
                    }
                }
            }
        }

        private static void FrameEvents()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            FastSpanEnumerator<KeyboardKey> keyEnumerator = new(Enum.GetValues<KeyboardKey>()/*FastEnum.GetValues<KeyboardKey, int>()*/);
            
            foreach (KeyboardKey key in keyEnumerator)
            {
                io.KeysDown[(int)key] = IsKeyDown(key);
            }

            uint pressed;
            
            while ((pressed = (uint)GetCharPressed()) != 0)
            {
                io.AddInputCharacter(pressed);
            }
        }
        
        private static void EnableScissor(float x, float y, float width, float height)
        {
            Rlgl.rlEnableScissorTest();
            Rlgl.rlScissor((int)x, GetScreenHeight() - (int)(y + height), (int)width, (int)height);
        }

        private static void TriangleVert(ImDrawVertPtr idxVert)
        {
            Vector4 color = ImGui.ColorConvertU32ToFloat4(idxVert.col);
            
            Rlgl.rlColor4f(color.X, color.Y, color.Z, color.W);
            Rlgl.rlTexCoord2f(idxVert.uv.X, idxVert.uv.Y);
            Rlgl.rlVertex2f(idxVert.pos.X, idxVert.pos.Y);
        }

        private static void RenderTriangles(uint count, uint indexStart, ImVector<ushort> indexBuffer, ImPtrVector<ImDrawVertPtr> vertBuffer, IntPtr texturePtr)
        {
            const byte triangleCount = 3;

            if (count < triangleCount)
                return;

            uint textureId = 0;
            if (texturePtr != IntPtr.Zero)
                textureId = (uint)texturePtr.ToInt32();

            Rlgl.rlBegin(DrawMode.TRIANGLES);
            Rlgl.rlSetTexture(textureId);
            
            for (int i = 0; i <= count - triangleCount; i += triangleCount)
            {
                if (Rlgl.rlCheckRenderBatchLimit(triangleCount))
                {
                    Rlgl.rlBegin(DrawMode.TRIANGLES);
                    Rlgl.rlSetTexture(textureId);
                }

                ushort indexA = indexBuffer[(int)indexStart + i];
                ushort indexB = indexBuffer[(int)indexStart + i + 1];
                ushort indexC = indexBuffer[(int)indexStart + i + 2];

                ImDrawVertPtr vertexA = vertBuffer[indexA];
                ImDrawVertPtr vertexB = vertBuffer[indexB];
                ImDrawVertPtr vertexC = vertBuffer[indexC];

                TriangleVert(vertexA);
                TriangleVert(vertexB);
                TriangleVert(vertexC);
            }
            Rlgl.rlEnd();
        }

        private delegate void Callback(ImDrawListPtr list, ImDrawCmdPtr cmd);

        private static void RenderData()
        {
            Rlgl.rlDrawRenderBatchActive();
            Rlgl.rlDisableBackfaceCulling();

            var data = ImGui.GetDrawData();

            for (int l = 0; l < data.CmdListsCount; l++)
            {
                ImDrawListPtr commandList = data.CmdLists[l];

                for (int cmdIndex = 0; cmdIndex < commandList.CmdBuffer.Size; cmdIndex++)
                {
                    var cmd = commandList.CmdBuffer[cmdIndex];

                    EnableScissor(cmd.ClipRect.X - data.DisplayPos.X, cmd.ClipRect.Y - data.DisplayPos.Y,
                        cmd.ClipRect.Z - (cmd.ClipRect.X - data.DisplayPos.X),
                        cmd.ClipRect.W - (cmd.ClipRect.Y - data.DisplayPos.Y));

                    if (cmd.UserCallback != nint.Zero)
                    {
                        Callback cb = Marshal.GetDelegateForFunctionPointer<Callback>(cmd.UserCallback);
                        cb(commandList, cmd);
                        continue;
                    }

                    RenderTriangles(cmd.ElemCount, cmd.IdxOffset, commandList.IdxBuffer, commandList.VtxBuffer, cmd.TextureId);

                    Rlgl.rlDrawRenderBatchActive();
                }
            }

            Rlgl.rlSetTexture(0);
            Rlgl.rlDisableScissorTest();
        }

       
        //PUBLIC METHODS//
        public static void Begin()
        {
            ImGui.SetCurrentContext(ImGuiContext);
            NewFrame();
            FrameEvents();
            ImGui.NewFrame();
        }
        
        public static void End()
        {
            ImGui.SetCurrentContext(ImGuiContext);
            ImGui.Render();
            RenderData();
        }

        public static void Shutdown()
        {
            UnloadTexture(FontTexture);
            ImGui.DestroyContext();
        }

        public static void Image(Texture2D image)
        {
            ImGui.Image(new IntPtr(image.id), new Vector2(image.width, image.height));
        }

        public static void ImageSize(Texture2D image, int width, int height)
        {
            ImGui.Image(new IntPtr(image.id), new Vector2(width, height));
        }

        public static void ImageSize(Texture2D image, Vector2 size)
        {
            ImGui.Image(new IntPtr(image.id), size);
        }

        public static void ImageRect(Texture2D image, int destWidth, int destHeight, Rectangle sourceRect)
        {
            Vector2 uv0 = Vector2.Zero;
            Vector2 uv1 = Vector2.Zero;

            if (sourceRect.width < 0)
            {
                uv0.X = -(sourceRect.x / image.width);
                uv1.X = uv0.X - Math.Abs(sourceRect.width) / image.width;
            }
            else
            {
                uv0.X = sourceRect.x / image.width;
                uv1.X = uv0.X + sourceRect.width / image.width;
            }

            if (sourceRect.height < 0)
            {
                uv0.Y = -(sourceRect.y / image.height);
                uv1.Y = uv0.Y - Math.Abs(sourceRect.height) / image.height;
            }
            else
            {
                uv0.Y = sourceRect.y / image.height;
                uv1.Y = uv0.Y + sourceRect.height / image.height;
            }

            ImGui.Image((nint)image.id, new Vector2(destWidth, destHeight), uv0, uv1);
        }
    }
}