using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace OpenTK_PathTracer.Render.Objects
{
    class Texture : IDisposable
    {
        public enum PixelTypeSize
        {
            TextureRedSize = 32860,
            TextureGreenSize = 32861,
            TextureBlueSize = 32862,
            TextureAlphaSize = 32863,
        }

        public enum TextureDimension
        {
            Undefined = 0,
            One = 1,
            Two = 2,
            Three = 3,
        }

        public readonly int ID;
        public readonly TextureDimension Dimension;
        public TextureTarget Target { get; private set; }
        public int Width { get; private set; } = 1;
        public int Height { get; private set; } = 1;
        public int Depth { get; private set; } = 1;
        public PixelInternalFormat PixelInternalFormat { get; private set; }

        public Texture(TextureTarget3d textureTarget3D)
        {
            Dimension = TextureDimension.Three;

            ID = GL.GenTexture();
            Bind((TextureTarget)textureTarget3D);
            SetFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);
        }

        public Texture(TextureTarget2d textureTarget2D)
        {
            Dimension = TextureDimension.Two;

            ID = GL.GenTexture();
            Bind((TextureTarget)textureTarget2D);
            SetFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);
        }

        public Texture(TextureTarget1d textureTarget1D)
        {
            Dimension = TextureDimension.One;

            ID = GL.GenTexture();
            Bind((TextureTarget)textureTarget1D);
            SetFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);
        }

        public Texture(TextureBufferTarget textureBufferTarget, BufferObject bufferObject, SizedInternalFormat sizedInternalFormat = SizedInternalFormat.Rgba32f)
        {
            Dimension = TextureDimension.Undefined;

            ID = GL.GenTexture();
            Bind((TextureTarget)textureBufferTarget);
            GL.TexBuffer((TextureBufferTarget)Target, sizedInternalFormat, bufferObject.ID);
            GL.TexBufferRange((TextureBufferTarget)Target, sizedInternalFormat, bufferObject.ID, IntPtr.Zero, bufferObject.Size);
        }

        public void SetFilter(TextureMinFilter minFilter, TextureMagFilter magFilter)
        {
            /// Explanation for Mipmap filters from https://learnopengl.com/Getting-started/Textures:
            /// GL_NEAREST_MIPMAP_NEAREST: takes the nearest mipmap to match the pixel size and uses nearest neighbor interpolation for texture sampling.
            /// GL_LINEAR_MIPMAP_NEAREST: takes the nearest mipmap level and samples that level using linear interpolation.
            /// GL_NEAREST_MIPMAP_LINEAR: linearly interpolates between the two mipmaps that most closely match the size of a pixel and samples the interpolated level via nearest neighbor interpolation.
            /// GL_LINEAR_MIPMAP_LINEAR: linearly interpolates between the two closest mipmaps and samples the interpolated level via linear interpolation.
            GL.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)magFilter);
        }

        public void SetWrapMode(TextureWrapMode wrapS, TextureWrapMode wrapT)
        {
            GL.TexParameter(Target, TextureParameterName.TextureWrapS, (int)wrapS);
            GL.TexParameter(Target, TextureParameterName.TextureWrapT, (int)wrapT);
        }

        public void SetWrapMode(TextureWrapMode wrapS, TextureWrapMode wrapT, TextureWrapMode wrapR)
        {
            GL.TexParameter(Target, TextureParameterName.TextureWrapS, (int)wrapS);
            GL.TexParameter(Target, TextureParameterName.TextureWrapT, (int)wrapT);
            GL.TexParameter(Target, TextureParameterName.TextureWrapR, (int)wrapR);
        }

        public void Bind(TextureTarget textureTarget)
        {
            GL.BindTexture(textureTarget, ID);
            Target = textureTarget;
        }

        public void AttachImage(int unit, int level, bool layered, int layer, TextureAccess textureAccess, SizedInternalFormat sizedInternalFormat)
        {
            GL.BindImageTexture(unit, ID, level, layered, layer, textureAccess, sizedInternalFormat);
        }
        public void AttachSampler(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(Target, ID);
        }

        public void SubTexture3D<T>(int width, int heigth, int depth, PixelFormat pixelFormat, PixelType pixelType, T[] pixels, int level = 0, int xOffset = 0, int yOffset = 0, int zOffset = 0) where T : struct
        {
            GL.TexSubImage3D(Target, level, xOffset, yOffset, zOffset, width, heigth, depth, pixelFormat, pixelType, pixels);
        }
        public void SubTexture3D(int width, int heigth, int depth, PixelFormat pixelFormat, PixelType pixelType, IntPtr pixels, int level = 0, int xOffset = 0, int yOffset = 0, int zOffset = 0)
        {
            GL.TexSubImage3D(Target, level, xOffset, yOffset, zOffset, width, heigth, depth, pixelFormat, pixelType, pixels);
        }
        public void SubTexture2D<T>(int width, int heigth, PixelFormat pixelFormat, PixelType pixelType, T[] pixels, int level = 0, int xOffset = 0, int yOffset = 0) where T : struct
        {
            GL.TexSubImage2D(Target, level, xOffset, yOffset, width, heigth, pixelFormat, pixelType, pixels);
        }
        public void SubTexture2D(int width, int heigth, PixelFormat pixelFormat, PixelType pixelType, IntPtr pixels, int level = 0, int xOffset = 0, int yOffset = 0)
        {
            GL.TexSubImage2D(Target, level, xOffset, yOffset, width, heigth, pixelFormat, pixelType, pixels);
        }
        public void SubTexture1D<T>(int width, PixelFormat pixelFormat, PixelType pixelType, T[] pixels, int level = 0, int xOffset = 0) where T : struct
        {
            GL.TexSubImage1D(Target, level, xOffset, width, pixelFormat, pixelType, pixels);
        }
        public void SubTexture1D(int width, PixelFormat pixelFormat, PixelType pixelType, IntPtr pixels, int level = 0, int xOffset = 0)
        {
            GL.TexSubImage1D(Target, level, xOffset, width, pixelFormat, pixelType, pixels);
        }


        /// <summary>
        /// To properly generate mipmaps <see cref="TextureMinFilter"/> must be set to one of the mipmap options 
        /// and if immutable storage is used the level parameter should match the number of desired mipmap levels to generate (default: 1).
        /// </summary>
        public void GenerateMipmap()
        {
            GL.GenerateMipmap((GenerateMipmapTarget)Target);
        }

        /// <summary>
        /// ARB_seamless_cubemap_per_texture must be available
        /// </summary>
        /// <param name="param"></param>
        public void SetSeamlessCubeMapPerTexture(bool param)
        {
            if (Target == TextureTarget.TextureCubeMap)
                GL.TexParameter(Target, (TextureParameterName)All.TextureCubeMapSeamless, param ? 1 : 0);
        }

        public void SetBorderColor(Vector4 color)
        {
            unsafe
            {
                float* colors = stackalloc[] { color.X, color.Y, color.Z, color.W };
                GL.TexParameter(Target, TextureParameterName.TextureBorderColor, colors);
            }
        }

        public void SetMipmapLodBias(float bias)
        {
            GL.TextureParameter(ID, TextureParameterName.TextureLodBias, bias);
        }

        public void MutableAllocate(int width, int height, int depth, PixelInternalFormat pixelInternalFormat)
        {
            switch (Dimension)
            {
                case TextureDimension.One:
                    GL.TexImage1D(Target, 0, pixelInternalFormat, width, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
                    Width = width;
                    break;

                case TextureDimension.Two:
                    if (Target == TextureTarget.TextureCubeMap)
                        for (int i = 0; i < 6; i++)
                            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, pixelInternalFormat, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
                    else
                        GL.TexImage2D(Target, 0, pixelInternalFormat, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
                    Width = width; Height = height;
                    break;

                case TextureDimension.Three:
                    GL.TexImage3D(Target, 0, pixelInternalFormat, width, height, depth, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
                    Width = width; Height = height; Depth = depth;
                    break;

                default:
                    return;
            }
            PixelInternalFormat = pixelInternalFormat;
        }

        public void MutableAllocate(int width, int height, int depth, PixelInternalFormat pixelInternalFormat, IntPtr intPtr, PixelFormat pixelFormat, PixelType pixelType)
        {
            switch (Dimension)
            {
                case TextureDimension.One:
                    GL.TexImage1D(Target, 0, pixelInternalFormat, width, 0, pixelFormat, pixelType, intPtr);
                    Width = width;
                    break;

                case TextureDimension.Two:
                    if (Target == TextureTarget.TextureCubeMap)
                        for (int i = 0; i < 6; i++)
                            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, pixelInternalFormat, width, height, 0, pixelFormat, pixelType, intPtr);
                    else
                        GL.TexImage2D(Target, 0, pixelInternalFormat, width, height, 0, pixelFormat, pixelType, intPtr);
                    Width = width; Height = height;
                    break;

                case TextureDimension.Three:
                    GL.TexImage3D(Target, 0, pixelInternalFormat, width, height, depth, 0, pixelFormat, pixelType, intPtr);
                    Width = width; Height = height; Depth = depth;
                    break;

                default:
                    return;
            }
            PixelInternalFormat = pixelInternalFormat;
        }

        public void ImmutableAllocate(int width, int height, int depth, SizedInternalFormat sizedInternalFormat, int levels = 1)
        {
            switch (Dimension)
            {
                case TextureDimension.One:
                    GL.TexStorage1D((TextureTarget1d)Target, levels, sizedInternalFormat, width);
                    Width = width;
                    break;

                case TextureDimension.Two:
                    GL.TexStorage2D((TextureTarget2d)Target, levels, sizedInternalFormat, width, height);
                    Width = width; Height = height;
                    break;

                case TextureDimension.Three:
                    GL.TexStorage3D((TextureTarget3d)Target, levels, sizedInternalFormat, width, height, depth);
                    Width = width; Height = height; Depth = depth;
                    break;

                default:
                    return;
            }
            PixelInternalFormat = (PixelInternalFormat)sizedInternalFormat;
        }

        /// <summary>
        /// GL_ARB_bindless_texture must be available
        /// </summary>
        /// <returns></returns>
        public long GetTextureBindlessHandle()
        {
            long textureHandle = GL.Arb.GetTextureHandle(ID);
            GL.Arb.MakeTextureHandleResident(textureHandle);
            return textureHandle;
        }

        /// <summary>
        /// GL_ARB_bindless_texture must be available
        /// </summary>
        /// <returns></returns>
        public static bool UnmakeTextureBindless(long textureHandle)
        {
            if (GL.Arb.IsTextureHandleResident(textureHandle))
            {
                GL.Arb.MakeTextureHandleNonResident(textureHandle);
                return true;
            }
            return false;
        }

        /// <summary>
        /// GL_ARB_bindless_texture must be available
        /// </summary>
        /// <returns></returns>
        public long GetImageBindlessHandle(int level, bool layered, int layer, PixelFormat pixelFormat, TextureAccess textureAccess)
        {
            long imageHandle = GL.Arb.GetImageHandle(ID, level, layered, layer, pixelFormat);
            GL.Arb.MakeImageHandleResident(imageHandle, (All)textureAccess);
            return imageHandle;
        }

        /// <summary>
        /// GL_ARB_bindless_texture must be available
        /// </summary>
        /// <returns></returns>
        public static bool UnmakeImageBindless(long imageHandle)
        {
            if (GL.Arb.IsImageHandleResident(imageHandle))
            {
                GL.Arb.MakeImageHandleNonResident(imageHandle);
                return true;
            }
            return false;
        }

        public Bitmap GetTextureContent(int mipmapLevel = 0)
        {
            GetSizeMipmap(out int width, out int height, mipmapLevel);

            Bitmap bmp = new Bitmap(width, height);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.GetTextureImage(ID, mipmapLevel, PixelFormat.Bgra, PixelType.UnsignedByte, GetPixelSize(mipmapLevel) * width * height, bmpData.Scan0);
            GL.Finish();

            bmp.UnlockBits(bmpData);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return bmp;
        }

        public void GetSizeMipmap(out int width, out int height, int mipmapLevel = 0)
        {
            GL.GetTexLevelParameter(Target, mipmapLevel, GetTextureParameter.TextureWidth, out width);
            GL.GetTexLevelParameter(Target, mipmapLevel, GetTextureParameter.TextureHeight, out height);

            if (width * height != 0)
                throw new ArgumentException($"Can not get size from Texture {ID} at level {mipmapLevel}");
        }

        public int GetPixelTypeComponentSize(PixelTypeSize pixelTypeSize, int mipmapLevel = 0)
        {
            GL.GetTexLevelParameter(Target, mipmapLevel, (GetTextureParameter)pixelTypeSize, out int bitSize);
            return bitSize / 8;
        }

        public int GetPixelSize(int mipmapLevel = 0)
        {
            int r = GetPixelTypeComponentSize(PixelTypeSize.TextureRedSize, mipmapLevel);
            int g = GetPixelTypeComponentSize(PixelTypeSize.TextureGreenSize, mipmapLevel);
            int b = GetPixelTypeComponentSize(PixelTypeSize.TextureBlueSize, mipmapLevel);
            int a = GetPixelTypeComponentSize(PixelTypeSize.TextureAlphaSize, mipmapLevel);

            return r + g + b + a;
        }

        public void Dispose()
        {
            GL.DeleteTexture(ID);
        }
    }
}