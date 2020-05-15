using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace FDK
{
    /// <summary>
    ///     コメント文があればYAMLに出力するためのVisitor。
    /// </summary>
    /// <seealso cref="https://dotnetfiddle.net/8M6iIE"/>
    public class CommentsObjectGraphVisitor : ChainedObjectGraphVisitor
    {
        public CommentsObjectGraphVisitor( IObjectGraphVisitor<IEmitter> nextVisitor )
            : base( nextVisitor )
        {
        }

        public override bool EnterMapping( IPropertyDescriptor key, IObjectDescriptor value, IEmitter context )
        {
            var commentsDescriptor = value as CommentsObjectDescriptor;
            if( commentsDescriptor != null && commentsDescriptor.Comment != null )
            {
                context.Emit( new Comment( commentsDescriptor.Comment, false ) );
            }

            return base.EnterMapping( key, value, context );
        }
    }
}
