using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_PathTracer.Render.Objects
{
    class Framebuffer : IDisposable
    {
        public readonly int ID;

        private int _rbo = -1;
        public int RBO
        {
            get
            {
                if (_rbo == -1)
                    throw new System.Exception("No RBO was attached to the framebuffer yet");

                return _rbo;
            }
        }

        public FramebufferTarget Target { get; private set; }

        private static int lastBindedID = -1;
        public Framebuffer()
        {
            ID = GL.GenFramebuffer();
        }

        public void Clear(ClearBufferMask clearBufferMask)
        {
            Bind();
            GL.Clear(clearBufferMask);
        }

        public void AddRenderTarget(FramebufferAttachment framebufferAttachment, Texture texture)
        {
            Bind();
            GL.FramebufferTexture(Target, framebufferAttachment, texture.ID, 0);
        }

        public void SetRenderbuffer(RenderbufferStorage renderbufferStorage, FramebufferAttachment framebufferAttachment, int width, int height)
        {
            Bind();

            _rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, renderbufferStorage, width, height);
            GL.FramebufferRenderbuffer(Target, framebufferAttachment, RenderbufferTarget.Renderbuffer, _rbo);
        }

        public void SetRenderTarget(params DrawBuffersEnum[] drawBuffersEnums)
        {
            Bind();
            GL.DrawBuffers(drawBuffersEnums.Length, drawBuffersEnums);
        }
        public void SetReadTarget(ReadBufferMode readBufferMode)
        {
            Bind();
            GL.ReadBuffer(readBufferMode);
        }

        public FramebufferErrorCode GetGBOStatus()
        {
            Bind();
            return GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        }

        public void Bind(FramebufferTarget framebufferTarget = FramebufferTarget.Framebuffer)
        {
            if (lastBindedID != ID)
            {
                GL.BindFramebuffer(framebufferTarget, ID);
                lastBindedID = ID;
                Target = framebufferTarget;
            }  
        }

        public static void Bind(int id, FramebufferTarget framebufferTarget = FramebufferTarget.Framebuffer)
        {
            if (lastBindedID != id)
            {
                GL.BindFramebuffer(framebufferTarget, id);
                lastBindedID = id;
            }
        }


        public static void Clear(int id, ClearBufferMask clearBufferMask)
        {
            Framebuffer.Bind(id);
            GL.Clear(clearBufferMask);
        }

        public static Bitmap GetBitmapFramebufferAttachment(int id, FramebufferAttachment framebufferAttachment, int width, int height, int x = 0, int y = 0)
        {
            Bitmap bmp = new Bitmap(width, height);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Bind(id, FramebufferTarget.Framebuffer);
            GL.ReadBuffer((ReadBufferMode)framebufferAttachment);

            GL.ReadPixels(x, y, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
            GL.Finish();

            bmp.UnlockBits(bmpData);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return bmp;
        }

        public void Dispose()
        {
            GL.DeleteFramebuffer(ID);
        }
    }
}
