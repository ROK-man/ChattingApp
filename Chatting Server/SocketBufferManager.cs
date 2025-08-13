using System.Net.Sockets;

namespace Chatting_Server
{
    internal class SocketBufferManager
    {
        int m_numBuffers;
        byte[]? m_bytes;
        Stack<int> m_freeIndexPool;
        int m_currentIndex;
        int m_bufferSize;

        public SocketBufferManager(int numBuffers, int bufferSize)
        {
            m_numBuffers = numBuffers;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
            m_currentIndex = 0;
        }

        public void Init()
        {
            m_bytes = new byte[m_numBuffers * m_bufferSize];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_bytes, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBuffers - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_bytes, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
