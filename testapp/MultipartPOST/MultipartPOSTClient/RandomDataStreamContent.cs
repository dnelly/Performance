// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MultipartPostClient
{
    public enum DataGenerationType
    {
        Binary =    1 << 0,
        Text =      1 << 1,
    }

    internal interface IDataGenerator
    {
        int Read(byte[] buffer, int offset, int count);
    }

    internal static class DataGeneratorFactory
    {
        private sealed class RandomBinaryDataGenerator : IDataGenerator
        {
            private byte _currentValue;
            public int Read(byte[] buffer, int offset, int count)
            {
                for (var iter = 0; iter < buffer.Length; ++iter)
                {
                    buffer[iter] = _currentValue;
                    _currentValue = (byte)((_currentValue + 1) % byte.MaxValue);
                }
                return count;
            }
        }

        private sealed class RandomUtf8TextDataGenerator : IDataGenerator
        {
            private readonly byte[] _textSeed;
            private int _currentByte;
            public RandomUtf8TextDataGenerator()
            {

                _textSeed =
                    Encoding.UTF8.GetBytes(
                        "I'm afraid I still don't understand, sir.\r\n Mr. Crusher, ready a collision course with the Borg ship.\r\n Congratulations - you just destroyed the Enterprise.\r\n They were just sucked into space.\r\n Smooth as an android's bottom, eh, Data?\r\n Our neural pathways have become accustomed to your sensory input patterns.\r\n That might've been one of the shortest assignments in the history of Starfleet.\r\n You bet I'm agitated!\r\n I may be surrounded by insanity, but I am not insane.\r\n Some days you get the bear, and some days the bear gets you.\r\n Maybe if we felt any human loss as keenly as we feel one of those close to us, human history would be far less bloody.\r\n This is not about revenge. This is about justice.\r\n Is it my imagination, or have tempers become a little frayed on the ship lately?\r\n Well, I'll say this for him - he's sure of himself.\r\n Damage report!\r\n I recommend you don't fire until you're within 40,000 kilometers.\r\n Well, that's certainly good to know.\r\n The game's not big enough unless it scares you a little.\r\n Besides, you look good in a dress.\r\n What's a knock-out like you doing in a computer-generated gin joint like this?\r\n And blowing into maximum warp speed, you appeared for an instant to be in two places at once.\r\n");
                _currentByte = 0;
            }
            public int Read(byte[] buffer, int offset, int count)
            {
                for (var iter = 0; iter < buffer.Length; ++iter)
                {
                    buffer[iter] = _textSeed[_currentByte];
                    _currentByte = (_currentByte + 1) % _textSeed.Length;
                }
                return count;
            }
        }

        public static IDataGenerator GetNewDataGenerator(DataGenerationType dataGenerationType)
        {
            switch (dataGenerationType)
            {
                case DataGenerationType.Binary:
                    return new RandomBinaryDataGenerator();
                case DataGenerationType.Text:
                    return new RandomUtf8TextDataGenerator();
                default:
                    throw new InvalidOperationException(nameof(dataGenerationType));
            }
        }
    }

    public class RandomDataStreamContent : HttpContent
    {
        private const int DefaultBufferSize = 4096;
        
        private readonly int _bufferSize;
        private readonly long _desiredLength;
        private readonly string _fileName;
        private readonly Action<long> _progressUpdater;
        private readonly IDataGenerator _generator;

        public RandomDataStreamContent(DataGenerationType dataGenerationType, long desiredLength, string fileName, Action<long> progressUpdater = null) :
            this(dataGenerationType, desiredLength, DefaultBufferSize, fileName, progressUpdater) { }

        public RandomDataStreamContent(DataGenerationType dataGenerationType, long desiredLength, int bufferSize, string fileName, Action<long> progressUpdater = null)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }
            
            _bufferSize = bufferSize;
            _fileName = fileName;
            _progressUpdater = progressUpdater;
            _desiredLength = desiredLength;
            _generator = DataGeneratorFactory.GetNewDataGenerator(dataGenerationType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Run(() =>
            {
                PrintLine("Serializing data");
                var buffer = new byte[_bufferSize];
                var uploaded = 0;

                while (uploaded < _desiredLength)
                {
                    var length = _generator.Read(buffer, 0, buffer.Length);
                    if (length <= 0) break;

                    uploaded += length;
                    if (uploaded > _desiredLength)
                    {
                        var delta = (int)(uploaded - _desiredLength);
                        uploaded -= delta;
                        length -= delta;
                    }

                    stream.Write(buffer, 0, length);
                    _progressUpdater?.Invoke(uploaded);
                }
                PrintLine($"Serialized { _desiredLength } bytes [{ _fileName }]");
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _desiredLength;
            return true;
        }

        private void PrintLine(string input, params object[] paramStrings)
        {
            Console.Write($"[{ DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }] ");
            Console.WriteLine(input, paramStrings);
        }
    }
}
