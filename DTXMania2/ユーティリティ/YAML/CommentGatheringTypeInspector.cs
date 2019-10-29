using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace DTXMania2
{
    /// <summary>
    ///     [Description("コメント文")] で、YAML にコメントを出力するための TypeInspector。
    /// </summary>
    /// <remarks>
    ///     使用例：
    ///     var serializer = new YamlDotNet.Serialization.SerializerBuilder()
    ///         .WithTypeInspector( inner => new CommentGatheringTypeInspector( inner ) )
    ///         .WithEmissionPhaseObjectGraphVisitor( args => new CommentsObjectGraphVisitor( args.InnerVisitor ) )
    ///         .Build();
    /// </remarks>
    /// <seealso cref="https://dotnetfiddle.net/8M6iIE"/>
    public class CommentGatheringTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector innerTypeDescriptor;

        public CommentGatheringTypeInspector( ITypeInspector innerTypeDescriptor )
        {
            this.innerTypeDescriptor = innerTypeDescriptor ?? throw new ArgumentNullException( "innerTypeDescriptor" );
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties( Type type, object? container )
        {
            return innerTypeDescriptor
                .GetProperties( type, container )
                .Select( d => new CommentsPropertyDescriptor( d ) )
                ?? new List<CommentsPropertyDescriptor>();
        }

        private sealed class CommentsPropertyDescriptor : IPropertyDescriptor
        {
            private readonly IPropertyDescriptor baseDescriptor;

            public CommentsPropertyDescriptor( IPropertyDescriptor baseDescriptor )
            {
                this.baseDescriptor = baseDescriptor;
                Name = baseDescriptor.Name;
            }

            public string Name { get; set; }

            public Type Type { get { return baseDescriptor.Type; } }

            public Type? TypeOverride
            {
                get { return baseDescriptor.TypeOverride; }
                set { baseDescriptor.TypeOverride = value; }
            }

            public int Order { get; set; }

            public ScalarStyle ScalarStyle
            {
                get { return baseDescriptor.ScalarStyle; }
                set { baseDescriptor.ScalarStyle = value; }
            }

            public bool CanWrite { get { return baseDescriptor.CanWrite; } }

            public void Write( object target, object? value )
            {
                baseDescriptor.Write( target, value );
            }

            public T GetCustomAttribute<T>() where T : Attribute
            {
                return baseDescriptor.GetCustomAttribute<T>();
            }

            public IObjectDescriptor Read( object target )
            {
                var description = baseDescriptor.GetCustomAttribute<DescriptionAttribute>();
                return description != null
                    ? new CommentsObjectDescriptor( baseDescriptor.Read( target ), description.Description )
                    : baseDescriptor.Read( target );
            }
        }
    }
}
