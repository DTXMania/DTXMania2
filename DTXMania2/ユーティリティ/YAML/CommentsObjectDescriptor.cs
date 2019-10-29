using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace DTXMania2
{
    /// <summary>
    ///     コメント文を表すでスクリプタ。
    /// </summary>
    /// <seealso cref="https://dotnetfiddle.net/8M6iIE"/>
    public sealed class CommentsObjectDescriptor : IObjectDescriptor
    {
        private readonly IObjectDescriptor innerDescriptor;

        public CommentsObjectDescriptor( IObjectDescriptor innerDescriptor, string comment )
        {
            this.innerDescriptor = innerDescriptor;
            this.Comment = comment;
        }

        public string Comment { get; private set; }

        public object? Value { get { return innerDescriptor.Value; } }
        public Type Type { get { return innerDescriptor.Type; } }
        public Type StaticType { get { return innerDescriptor.StaticType; } }
        public ScalarStyle ScalarStyle { get { return innerDescriptor.ScalarStyle; } }
    }
}
