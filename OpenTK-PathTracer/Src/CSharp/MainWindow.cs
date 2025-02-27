using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK_PathTracer.GameObjects;
using OpenTK_PathTracer.Render;
using OpenTK_PathTracer.Render.Objects;

namespace OpenTK_PathTracer
{
    class MainWindow : GameWindow
    {
        // Constants
        public const int MAX_GAMEOBJECTS_SPHERES = 256, MAX_GAMEOBJECTS_CUBOIDS = 64;
        public const float EPSILON = 0.005f, FOV = 103;

        // Projection variables
        public Matrix4 projection, inverseProjection;
        Vector2 nearFarPlane = new Vector2(EPSILON, 200f);

        // Stats
        public int FPS, UPS;
        private int fps, ups;

        public bool IsRenderInBackground = true;

        // Used for resizing
        int lastWidth = -1, lastHeight = -1;

        readonly Stopwatch fpsTimer = new Stopwatch();

        public readonly Camera Camera;

        // Objects
        public readonly List<BaseGameObject> GameObjects = new List<BaseGameObject>();
        ShaderProgram finalProgram;
        public BufferObject BasicDataUBO, GameObjectsUBO;
        public PathTracer PathTracer;
        public Rasterizer Rasterizer;
        public ScreenEffect PostProcesser;
        public AtmosphericScattering AtmosphericScatterer;
        public Texture SkyBox;

        // Constructor
        public MainWindow() : base(800, 800, new GraphicsMode(0, 0, 0, 0)) {
            
            Camera = new Camera(new Vector3(-18.93f, -5.07f, -17.75f), new Vector3(0, 1, 0), 6.7f, 3.5f);
        }

        // Few inputs
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.E:
                    if (e.IsRepeat)
                        return;

                    if (CursorVisible)
                    {
                        MouseManager.Update();
                        Camera.Velocity = Vector3.Zero;
                    }

                    CursorVisible = !CursorVisible;
                    CursorGrabbed = !CursorGrabbed;
                    break;
                case Key.V:
                    if (e.IsRepeat)
                        return;

                    VSync = VSync == VSyncMode.Off ? VSyncMode.On : VSyncMode.Off;
                    break;
                case Key.F11:
                    if (e.IsRepeat)
                        return;

                    WindowState = WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
                    break;
                default:
                    break;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (Focused || IsRenderInBackground)
            {
                //AtmosphericScatterer.ViewPos = Camera.Position;
                //AtmosphericScatterer.Run();
                PathTracer.Run();

                Rasterizer.Run(new AABB(Vector3.One, Vector3.One));

                PostProcesser.Run(PathTracer.Result, Rasterizer.Result);

                GL.Viewport(0, 0, Width, Height);
                Framebuffer.Clear(0, ClearBufferMask.ColorBufferBit);
                PostProcesser.Result.AttachSampler(TextureUnit.Texture0);
                finalProgram.Use();
                GL.DrawArrays(PrimitiveType.Quads, 0, 4);

                if (Focused)
                {
                    Render.GUI.Final.Run(this, (float)e.Time, out bool frameChanged);
                    if (frameChanged)
                        PathTracer.ThisRenderNumFrame = 0;
                }
                SwapBuffers();
                fps++;
            }
            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            if (fpsTimer.ElapsedMilliseconds >= 1000)
            {
                Title = $"FPS: {fps}; RayDepth: {PathTracer.RayDepth}; UPS: {ups} Position {Camera.Position}";
                FPS = fps;
                UPS = ups;
                fps = 0;
                ups = 0;
                fpsTimer.Restart();
            }
            ThreadManager.InvokeQueuedActions();
            KeyboardManager.Update();
            MouseManager.Update();

            if (Focused)
            {
                if (!CursorVisible)
                {
                    Point _point = PointToScreen(new Point(Width / 2, Height / 2));
                    Mouse.SetPosition(_point.X, _point.Y);
                }
                
                Render.GUI.Final.Update(this);

                if (!CursorVisible)
                {
                    Camera.ProcessInputs((float)args.Time, out bool frameChanged);
                    if (frameChanged)
                        PathTracer.ThisRenderNumFrame = 0;
                }

                int oldOffset = Vector4.SizeInBytes * 4 * 2 + Vector4.SizeInBytes;
                BasicDataUBO.Bind(BufferTarget.UniformBuffer);
                BasicDataUBO.Append(Vector4.SizeInBytes * 4 * 3, new Matrix4[] { Camera.View, Camera.View.Inverted(), Camera.View * projection });
                BasicDataUBO.Append(Vector4.SizeInBytes, ref Camera.Position);
                BasicDataUBO.Append(Vector4.SizeInBytes, ref Camera.ViewDir);
                BasicDataUBO.BufferOffset = oldOffset;
            }
            ups++;
            base.OnUpdateFrame(args);
        }

        // Initialization
        protected override void OnLoad(EventArgs e)
        {
            // Showing system info
            Console.WriteLine($"{(Environment.Is64BitProcess ? "64" : "32")} bits process on {Environment.OSVersion} x{(Environment.Is64BitOperatingSystem ? "64" : "86")}.");
            Console.WriteLine($"\nGPU: {GL.GetString(StringName.Renderer)}");
            Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
            Console.WriteLine($"GLSL Version: {GL.GetString(StringName.ShadingLanguageVersion)}");
            // FIX: For some reason MaxUniformBlockSize seems to be ≈33728 for RX 5700XT, although GL.GetInteger(MaxUniformBlockSize) returns 572657868.
            // I dont want to use SSBO, because of performance. Also see: https://opengl.gpuinfo.org/displayreport.php?id=6204 
            Console.WriteLine($"\nMaxShaderStorageBlockSize: {GL.GetInteger((GetPName)All.MaxShaderStorageBlockSize)}");
            Console.WriteLine($"MaxUniformBlockSize: {GL.GetInteger(GetPName.MaxUniformBlockSize)}");

            // Setup functionality
            GL.DepthMask(false);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Multisample);
            GL.Enable(EnableCap.TextureCubeMapSeamless);

            VSync = VSyncMode.Off;
            CursorVisible = false;
            CursorGrabbed = true;

            SkyBox = new Texture(TextureTarget2d.TextureCubeMap);
            Helper.ParallelLoadCubemapImages(SkyBox, new string[]
            {
                "Res/Textures/EnvironmentMap/posx.png",
                "Res/Textures/EnvironmentMap/negx.png",
                "Res/Textures/EnvironmentMap/posy.png",
                "Res/Textures/EnvironmentMap/negy.png",
                "Res/Textures/EnvironmentMap/posz.png",
                "Res/Textures/EnvironmentMap/negz.png"
            }, (SizedInternalFormat)PixelInternalFormat.Srgb8Alpha8);

            // Starting objects
            AtmosphericScatterer = new AtmosphericScattering(128, 100, 10, 2.1f, 35.0f, 0.01f, new Vector3(680, 550, 440), new Vector3(0, 500 + 800.0f, 0), new Vector3(20.43f, -201.99f + 800.0f, -20.67f));
            PathTracer = new PathTracer(SkyBox, Width, Height, 13, 1, 20f, 0.14f);
            Rasterizer = new Rasterizer(Width, Height);
            PostProcesser = new ScreenEffect(new Shader(ShaderType.FragmentShader, "Res/Shaders/PostProcessing/fragment.glsl".GetPathContent()), Width, Height);

            finalProgram = new ShaderProgram(new Shader(ShaderType.VertexShader, "Res/Shaders/screenQuad.glsl".GetPathContent()), new Shader(ShaderType.FragmentShader, "Res/Shaders/final.glsl".GetPathContent()));

            BasicDataUBO = new BufferObject(BufferRangeTarget.UniformBuffer, 0);
            BasicDataUBO.MutableAllocate(Vector4.SizeInBytes * 4 * 5 + Vector4.SizeInBytes * 3, IntPtr.Zero, BufferUsageHint.StaticDraw);
            GameObjectsUBO = new BufferObject(BufferRangeTarget.UniformBuffer, 1);
            GameObjectsUBO.MutableAllocate(Sphere.GPU_INSTANCE_SIZE * MAX_GAMEOBJECTS_SPHERES + Cuboid.GPU_INSTANCE_SIZE * MAX_GAMEOBJECTS_CUBOIDS, IntPtr.Zero, BufferUsageHint.StaticDraw);

            float width = 40, height = 25, depth = 25;
            #region Spheres

            int balls = 6;
            float radius = 1.3f;
            Vector3 dimensions = new Vector3(width * 0.6f, height, depth);
            for (float x = 0; x < balls; x++)
                for (float y = 0; y < balls; y++)
                    GameObjects.Add(new Sphere(new Vector3(dimensions.X / balls * x * 1.1f - dimensions.X / 2, (dimensions.Y / balls) * y - dimensions.Y / 2 + radius, -5), radius, PathTracer.NumSpheres++, new Material(albedo: new Vector3(0.59f, 0.59f, 0.99f), emissiv: new Vector3(0), refractionColor: Vector3.Zero, specularChance: x / (balls - 1), specularRoughness: y / (balls - 1), indexOfRefraction: 1f, refractionChance: 0.0f, refractionRoughnes: 0.1f)));

            Vector3 delta = dimensions / balls;
            for (float x = 0; x < balls; x++)
            {
                Material material = Material.Zero;
                material.Albedo = new Vector3(0.9f, 0.25f, 0.25f);
                material.SpecularChance = 0.02f;
                material.IOR = 1.05f;
                material.RefractionChance = 0.98f;
                material.AbsorbanceColor = new Vector3(1, 2, 3) * (x / balls);
                Vector3 position = new Vector3(-dimensions.X / 2 + radius + delta.X * x, 3f, -20f);
                GameObjects.Add(new Sphere(position, radius, PathTracer.NumSpheres++, material));


                Material material1 = Material.Zero;
                material1.SpecularChance = 0.02f;
                material1.SpecularRoughness = (x / balls);
                material1.IOR = 1.1f;
                material1.RefractionChance = 0.98f;
                material1.RefractionRoughnes = x / balls;
                material1.AbsorbanceColor = Vector3.Zero;
                position = new Vector3(-dimensions.X / 2 + radius + delta.X * x, -6f, -20f);
                GameObjects.Add(new Sphere(position, radius, PathTracer.NumSpheres++, material1));
            }

            #endregion

            #region Cuboids

            Cuboid down = new Cuboid(new Vector3(0, -height / 2, -10), new Vector3(width, EPSILON, depth), PathTracer.NumCuboids++, new Material(albedo: new Vector3(0.2f, 0.04f, 0.04f), emissiv: new Vector3(0.0f), refractionColor: Vector3.Zero, specularChance: 0.0f, specularRoughness: 0.051f, indexOfRefraction: 1.0f, refractionChance: 0.0f, refractionRoughnes: 0.0f));

            //Cuboid up = new Cuboid(new Vector3(down.Position.X, down.Position.Y + height, down.Position.Z - down.Dimensions.Z / 4f), new Vector3(down.Dimensions.X, EPSILON, down.Dimensions.Z / 2), PathTracer.NumCuboids++, new Material(albedo: new Vector3(0.6f), emissiv: new Vector3(0.0f), refractionColor: Vector3.Zero, specularChance: 0.023f, specularRoughness: 0.051f, indexOfRefraction: 1f, refractionChance: 0.0f, refractionRoughnes: 0));
            Cuboid upLight0 = new Cuboid(new Vector3(0, 20.5f - EPSILON, 6), new Vector3(down.Dimensions.X * 0.3f, EPSILON, down.Dimensions.Z * 0.3f), PathTracer.NumCuboids++, new Material(albedo: new Vector3(0.04f), emissiv: new Vector3(0.917f, 0.945f, 0.513f) * 5f, refractionColor: Vector3.Zero, specularChance: 0.0f, specularRoughness: 1.0f, indexOfRefraction: 1.0f, refractionChance: 0.0f, refractionRoughnes: 0.0f));

            Cuboid back = new Cuboid(new Vector3(down.Position.X, down.Position.Y + height / 2, down.Position.Z - depth / 2), new Vector3(width, height, EPSILON), PathTracer.NumCuboids++, new Material(albedo: new Vector3(1.0f), emissiv: new Vector3(0.0f), refractionColor: Vector3.Zero, specularChance: 0.0f, specularRoughness: 0.0f, indexOfRefraction: 1.0f, refractionChance: 0.0f, refractionRoughnes: 0.0f));
            //Cuboid front = new Cuboid(new Vector3(down.Position.X, down.Position.Y + height / 2 + Epsilon, down.Position.Z + depth / 2 - 0.3f / 2), new Vector3(width, height - Epsilon * 2, 0.3f), instancesCuboids++, new Material(albedo: new Vector3(1f), emissiv: new Vector3(0), refractionColor: new Vector3(0.01f), specularChance: 0.04f, specularRoughness: 0f, indexOfRefraction: 1f, refractionChance: 0.954f, refractionRoughnes: 0));

            Cuboid right = new Cuboid(new Vector3(down.Position.X + width / 2, down.Position.Y + height / 2, down.Position.Z), new Vector3(EPSILON, height, depth), PathTracer.NumCuboids++, new Material(albedo: new Vector3(0.8f, 0.8f, 0.4f), emissiv: new Vector3(0.0f), refractionColor: Vector3.Zero, specularChance: 1.0f, specularRoughness: 0.0f, indexOfRefraction: 1.0f, refractionChance: 0.0f, refractionRoughnes: 0.0f));
            Cuboid left = new Cuboid(new Vector3(down.Position.X - width / 2, down.Position.Y + height / 2, down.Position.Z), new Vector3(EPSILON, height, depth), PathTracer.NumCuboids++, new Material(albedo: new Vector3(0.24f, 0.6f, 0.24f), emissiv: new Vector3(0.0f), refractionColor: Vector3.Zero, specularChance: 0.0f, specularRoughness: 0.0f, indexOfRefraction: 1.0f, refractionChance: 0.0f, refractionRoughnes: 0.0f));

            Cuboid middle = new Cuboid(new Vector3(-15f, -10.5f + EPSILON, -15), new Vector3(3f, 6, 3f), PathTracer.NumCuboids++, new Material(albedo: new Vector3(1.0f), emissiv: new Vector3(0.0f), refractionColor: Vector3.Zero, specularChance: 0.0f, specularRoughness: 0.0f, indexOfRefraction: 1.0f, refractionChance: 0.0f, refractionRoughnes: 0));

            GameObjects.AddRange(new Cuboid[] { down, upLight0, back, right, left, middle });

            #endregion

            for (int i = 0; i < GameObjects.Count; i++)
                GameObjects[i].Upload(GameObjectsUBO);

            fpsTimer.Start();
        }
        
        protected override void OnResize(EventArgs e)
        {
            if ((lastWidth != Width || lastHeight != Height) && Width != 0 && Height != 0) // dont resize when minimizing and maximizing
            {
                PathTracer.Result.Bind(TextureTarget.Texture2D);
                PathTracer.SetSize(Width, Height);

                Rasterizer.Result.Bind(TextureTarget.Texture2D);
                Rasterizer.SetSize(Width, Height);

                PostProcesser.Result.Bind(TextureTarget.Texture2D);
                PostProcesser.SetSize(Width, Height);

                Render.GUI.Final.SetSize(Width, Height);

                projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), Width / (float)Height, nearFarPlane.X, nearFarPlane.Y);
                inverseProjection = projection.Inverted();
                BasicDataUBO.BufferOffset = 0;
                BasicDataUBO.Bind(BufferTarget.UniformBuffer);
                BasicDataUBO.Append(Vector4.SizeInBytes * 4 * 2, new Matrix4[] { projection, inverseProjection });
                BasicDataUBO.Append(Vector4.SizeInBytes, ref nearFarPlane);
                lastWidth = Width; lastHeight = Height;
            }
            base.OnResize(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            Render.GUI.Final.ImGuiController.PressChar(e.KeyChar);
        }

        protected override void OnFocusedChanged(EventArgs e)
        {
            if (Focused)
                MouseManager.Update();
        }

        protected override void OnClosed(EventArgs e)
        {
            ImGuiNET.ImGui.SaveIniSettingsToDisk("Res/imgui.ini");
            base.OnClosed(e);
        }

        public bool RayTrace(Ray ray, out BaseGameObject gameObject, out float t1, out float t2)
        {
            t1 = t2 = 0;
            gameObject = null;
            float tMin = float.MaxValue;
            for (int i = 0; i < GameObjects.Count; i++)
            {
                if (GameObjects[i].IntersectsRay(ray, out float tempT1, out float tempT2) && tempT2 > 0 && tempT1 < tMin)
                {
                    t1 = tempT1; t2 = tempT2;
                    tMin = GetSmallestPositive(t1, t2);
                    gameObject = GameObjects[i];
                }
            }

            return tMin != float.MaxValue;
        }

        public static float GetSmallestPositive(float t1, float t2)
        {
            return t1 < 0 ? t2 : t1;
        }

        public void SetGameObjectsRandomMaterial<T>(int maxNum) where T : BaseGameObject
        {
            int changed = 0;
            GameObjectsUBO.Bind(BufferTarget.UniformBuffer);
            for (int i = 0; i < GameObjects.Count && changed < maxNum; i++)
            {
                if (GameObjects[i] is T)
                {
                    GameObjects[i].Material = Material.GetRndMaterial();
                    GameObjects[i].Upload(GameObjectsUBO);
                    changed++;
                }
            }
        }
    }
}