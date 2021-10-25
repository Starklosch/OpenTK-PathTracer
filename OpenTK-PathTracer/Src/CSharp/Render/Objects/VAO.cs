using System;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_PathTracer.Render.Objects
{
    class VAO : IDisposable
    {
        private static int lastBindedID = -1;


        private readonly BufferObject vbo, ebo;
        public readonly int VertexSize;
        public readonly int ID;
        public VAO(BufferObject arrayBuffer, int vertexSize)
        {
            vbo = arrayBuffer;
            VertexSize = vertexSize;
            ID = GL.GenVertexArray();
            GL.BindVertexArray(ID);
        }

        public VAO(BufferObject arrayBuffer, BufferObject elementBuffer, int vertexSize)
        {
            vbo = arrayBuffer;
            ebo = elementBuffer;
            VertexSize = vertexSize;
            ID = GL.GenVertexArray();
            GL.BindVertexArray(ID);
        }

        public void SetAttribPointer(int index, int attribTypeElements, VertexAttribPointerType vertexAttribPointerType, int offset, bool normalize = false)
        {
            vbo.Bind(BufferTarget.ArrayBuffer);
            vbo.Bind(BufferTarget.ElementArrayBuffer);
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
