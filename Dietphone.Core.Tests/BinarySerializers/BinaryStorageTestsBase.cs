﻿using System.Linq;
using Dietphone.Models;
using System.IO;
using Ploeh.AutoFixture;
using Moq;
using System.Collections.Generic;

namespace Dietphone.BinarySerializers.Tests
{
    public abstract class BinaryStorageTestsBase
    {
        protected readonly Fixture fixture = new Fixture();

        protected T WriteAndRead<T>(BinaryStorage<T> storage, T itemToWrite, byte? overrideVersion = null)
            where T : Entity, new()
        {
            var stream = new NonDisposableMemoryStream();
            var streamProvider = new Mock<BinaryStreamProvider>();
            streamProvider.Setup(p => p.GetInputStream(It.IsAny<string>())).Returns(stream);
            streamProvider.Setup(p => p.GetOutputStream(It.IsAny<string>())).Returns(new OutputStreamStub(stream));
            storage.StreamProvider = streamProvider.Object;
            storage.Save(new List<T> { itemToWrite });
            stream.Position = 0;
            if (overrideVersion.HasValue)
            {
                stream.WriteByte(overrideVersion.Value);
                stream.Position = 0;
            }
            var readItem = storage.Load().Single();
            stream.Dispose();
            return readItem;
        }

        public class NonDisposableMemoryStream : MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
            }

            public new void Dispose()
            {
                base.Dispose(true);
            }
        }

        public class OutputStreamStub : OutputStream
        {
            public Stream Stream { get; private set; }

            public OutputStreamStub(Stream stream)
            {
                Stream = stream;
            }

            public void Commit(long size)
            {
            }
        }
    }
}
