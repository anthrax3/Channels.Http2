﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Channels.Http2
{
    public partial struct HuffmanReader
    {
        public static unsafe string ReadString(ReadableBuffer buffer)
        {
            int maxChars = (buffer.Length << 3) / _minCodeLength;
            if(maxChars <= 1024)
            {
                char* c = stackalloc char[maxChars];
                return ReadString(buffer, c, maxChars);
            }
            else
            {
                char[] arr = new char[maxChars];
                fixed(char* c = arr)
                {
                    return ReadString(buffer, c, maxChars);
                }
            }
        }

        private static unsafe string ReadString(ReadableBuffer buffer, char* c, int maxLen)
        {
            var reader = new HuffmanReader(buffer);
            
            for(int i = 0; i < maxLen; i++)
            {
                int next = reader.ReadNext();
                if (next < 0) return i == 0 ? "" : new string(c, 0, i);

                c[i] = (char)next;
            }
            if(reader.ReadNext() < 0) return new string(c, 0, maxLen);

            throw new EndOfStreamException();
        }

        ReadableBuffer _buffer;
        int _bit, _bits;
        HuffmanNode _node;
        public HuffmanReader(ReadableBuffer buffer)
        {
            _buffer = buffer;
            _bits = _bit= 0;
            _node = _root;
        }
        public int ReadNext()
        {
            int bitsRead = 0;
            while(true)
            {
                if(_bit == 0)
                {
                    _bits = _buffer.Peek();
                    if(_bits < 0)
                    {
                        if (bitsRead < 8) return -1;
                        throw new EndOfStreamException(); // not allowed full byte of padding
                    }
                    _buffer = _buffer.Slice(1);
                    _bit = 0x80;
                }
                _node = ((_bits & _bit) == 0) ? _node.False : _node.True;
                _bit >>= 1;
                if(_node.IsLeaf)
                {
                    int val = _node.Value;
                    _node = _root;
                    return val;
                }
                bitsRead++;
            }
            

        }

        class HuffmanNode
        {
            public HuffmanNode True, False;
            public int Value = -1;

            public bool IsLeaf => True == null;
        }

        static readonly HuffmanNode _root;
        static HuffmanReader()
        {
            var root = new HuffmanNode();
            for(int i = 0; i < _codes.Length; i++)
            {
                var code = _codes[i];
                int bit = 1 << (_codeLengths[i] - 1);
                var node = root;
                while(bit != 0)
                {
                    if((code & bit) == 0)
                    {
                        node = node.False ?? (node.False = new HuffmanNode());
                    }
                    else
                    {
                        node = node.True?? (node.True = new HuffmanNode());
                    }
                    bit >>= 1;
                }
                node.Value = i;
            }
#if DEBUG
            // check they make sense
            var stack = new System.Collections.Generic.Stack<string>();
            Dive(root, stack);
#endif

            _root = root;
        }
#if DEBUG
        private static void Dive(HuffmanNode node, Stack<string> stack)
        {
            // either both or neither nodes must be set
            if ((node.False == null) != (node.True == null))
            {
                throw new InvalidOperationException($"The huffman tree for {string.Concat(stack)} is invalid; mismatched sub-tree (Value={node.Value})");
            }
            else if (node.False == null && node.Value < 0)
            {
                throw new InvalidOperationException($"The huffman tree for {string.Concat(stack)} is invalid; missing value");
            }
            if(node.True != null)
            {
                stack.Push("1");
                Dive(node.True, stack);
                stack.Pop();
            }
            if (node.False != null)
            {
                stack.Push("0");
                Dive(node.False, stack);
                stack.Pop();
            }
        }
#endif
    }

}