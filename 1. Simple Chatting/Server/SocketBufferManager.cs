using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class SocketBufferManager
    {
        int m_numBytes;
        byte[]? m_buffer;
        Stack<int> m_freeIndexPool;
        int m_currentIndex;
        int m_bufferSize;

        public SocketBufferManager(int numBytes, int bufferSize)
        {
            m_numBytes = numBytes;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
            m_currentIndex = 0;
        }

        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else if (m_currentIndex <= m_numBytes - m_bufferSize)
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            else
            {
                Console.WriteLine("Buffer assign Error!!");
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
