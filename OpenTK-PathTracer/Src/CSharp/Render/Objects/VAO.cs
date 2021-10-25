using System;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_PathTracer.Render.Objects
{
    class VAO : IDisposable
    {
        private static int lastBindedID = -1;

        public readonly int VertexSize;
        public readonly int ID;
        public VAO(BufferObject arrayBuffer, int vertexSize)
        {
            VertexSize = vertexSize;
            ID = GL.GenVertexArray();
            GL.BindVertexArray(ID);
            GL.BindVertexBuffer(0, arrayBuffer.ID, IntPtr.Zero, vertexSize);
        }

        public VAO(BufferObject arrayBuffer, BufferObject elementBuffer, int vertexSize)
        {
            VertexSize = vertexSize;
            
            ID = GL.GenVertexArray();
            GL.BindVertexArray(ID);
            GL.BindVertexBuffer(0, arrayBuffer.ID, IntPtr.Zero, vertexSize);
            elementBuffer.Bind(BufferTarget.ElementArrayBuffer);
        }

        public void SetAttribPointer(int index, int attribTypeElements, VertexAttribPointerType vertexAttribPointerType, int offset, bool normalize = false)
        {
            GL.VertexAttribPointer(index, attribTypeElements, vertexAttribPointerType, normalize, VertexSize, offset);
            GL.EnableVertexAttribArray(index);
        }

        public void Bind()
        {
            if (lastBindedID != ID)
            {
                GL.BindVertexArray(ID);
                lastBindedID = ID;
            }
        }

        public static void Bind(int iD)
        {
            if (lastBindedID != iD)
            {
                GL.BindVertexArray(iD);
                lastBindedID = iD;
            }
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(ID);
        }
    }
}
