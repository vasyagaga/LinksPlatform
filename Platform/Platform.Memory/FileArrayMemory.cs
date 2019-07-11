﻿using System.IO;
using Platform.Helpers.Disposables;
using Platform.Helpers.IO;
using Platform.Helpers.Unsafe;

namespace Platform.Memory
{
    public class FileArrayMemory<TElement> : DisposableBase, IArrayMemory<TElement> //-V3073
        where TElement : struct
    {
        public static readonly long ElementSize = UnsafeHelpers.SizeOf<TElement>();

        private readonly string _address;
        private readonly FileStream _file;

        public long Size => _file.Length;

        public FileArrayMemory(string address)
        {
            _address = address;
            _file = File.Open(address, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public TElement this[long index]
        {
            get
            {
                _file.Seek(ElementSize * index, SeekOrigin.Begin);
                return _file.ReadOrDefault<TElement>();
            }
            set
            {
                _file.Seek(ElementSize * index, SeekOrigin.Begin);
                _file.Write(value);
            }
        }

        #region Disposable

        protected override void DisposeCore(bool manual, bool wasDisposed) => Disposable.TryDispose(_file);

        protected override string ObjectName => $"File stored memory block '{_address}'.";

        #endregion
    }
}